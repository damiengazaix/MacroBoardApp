﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Xamarin.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Xamarin.Essentials;

namespace MacroBoardApp
{
    public partial class MainPage : ContentPage
    {
        NetworkStream stream;
        IPAddress localAddr;
        public TcpClient clientSender;
        ObservableCollection<Workflow> WfList { get; set; }
        public bool waitImgVisibility { get; set; } = false;
        public double widthPhone;

        public MainPage()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NjUxNTAyQDMyMzAyZTMxMmUzMGJsbW9vVzMxaUtEdnVRbWNOYVdpc2VtYUdOREIyQjZyUCt6VWRCK0hGbDg9");


            InitializeComponent();
            BindingContext = this;

            WfList = new ObservableCollection<Workflow>();
            WfList_XAML.ItemsSource = WfList;
            widthPhone = DeviceDisplay.MainDisplayInfo.Width / 6.25;

        }


        private void Btn_Connect_Clicked(object sender, EventArgs e)
        {
            Console.WriteLine(widthPhone);
            WfList.Clear();
            waitImgVisibility = true;

            ThreadStart starter = Reload_Workflows;
            starter += () =>
            {
                waitImgVisibility = false;
            };

            Thread thread = new Thread(starter) { IsBackground = true };
            thread.Start();




        }

        private void Reload_Workflows()
        {
            localAddr = IPAddress.Parse("147.94.234.152");
            int port = 13000;
            TcpClient client = new TcpClient(localAddr.ToString(), port);
            stream = client.GetStream();
            Trace.WriteLine("Connected");
            try
            {
                List<byte> imgData = new List<byte>();
                byte[] imgLength = new byte[20];
                byte[] nameLength = new byte[20];
                byte[] nameData = new byte[50];

                byte[] wfNumber = new byte[1];
                stream.Read(wfNumber, 0, 1);

                for (int i = Int16.Parse(Encoding.ASCII.GetString(wfNumber)); i > 0; i--)
                {
                    imgData = new List<byte>();
                    imgLength = new byte[20];
                    nameLength = new byte[20];

                    stream.Read(imgLength, 0, imgLength.Length);

                    byte[] confirmMsg = Encoding.ASCII.GetBytes("Image Length Receive");

                    stream.Write(confirmMsg, 0, confirmMsg.Length);

                    int bytes;
                    int imgMaxLen = Int32.Parse(Encoding.ASCII.GetString(imgLength));

                    for (int count = 0; count < imgMaxLen; count++)
                    {
                        bytes = stream.ReadByte();
                        imgData.Add((byte)bytes);
                    }

                    confirmMsg = Encoding.ASCII.GetBytes("Image Receive");
                    stream.Write(confirmMsg, 0, confirmMsg.Length);

                    stream.Read(nameLength, 0, nameLength.Length);

                    int nameMaxLen = Int32.Parse(Encoding.ASCII.GetString(nameLength));
                    nameData = new byte[nameMaxLen];
                    stream.Read(nameData, 0, nameMaxLen);

                    confirmMsg = Encoding.ASCII.GetBytes("Everything Received");
                    stream.Write(confirmMsg, 0, confirmMsg.Length);

                    WfList.Add(new Workflow(Encoding.ASCII.GetString(nameData, 0, nameData.Length),
                        byteArrayToImage(imgData.ToArray()), widthPhone));

                    Console.WriteLine("Everything Received");
                }

                // Close everything.
                stream.Close();
                client.Close();
                int portsender = 14000;
                clientSender = new TcpClient(localAddr.ToString(), portsender);
                Trace.WriteLine("Connected");
            }
            catch (ArgumentNullException ee)
            {
                Console.WriteLine("ArgumentNullException: {0}", ee);
            }
            catch (SocketException ee)
            {
                Console.WriteLine("SocketException: {0}", ee);
            }
            catch (NullReferenceException ee)
            {
                Console.WriteLine("NullReferenceException: {0}", ee);
            }

        }

        public void BtnOnclick(object sender, EventArgs args)
        {
            if (clientSender == null)
                return;

            NetworkStream streamSender = clientSender.GetStream();

            string nameBtn = ((Label)((Grid)((ImageButton)sender).Parent).Children[1]).Text.Trim();
            Console.WriteLine("name : " + nameBtn);

            byte[] nameToSend = Encoding.ASCII.GetBytes(nameBtn.Length.ToString());
            streamSender.Write(nameToSend, 0, nameToSend.Length);
            Console.WriteLine("size : " + Encoding.ASCII.GetString(nameToSend));

            byte[] okReception = new byte[8];
            streamSender.Read(okReception, 0, okReception.Length);


            nameToSend = Encoding.ASCII.GetBytes(nameBtn);
            streamSender.Write(nameToSend, 0, nameToSend.Length);







        }





        private ImageSource byteArrayToImage(byte[] byteArrayIn)
        {
            ImageSource returnImage = ImageSource.FromStream(() => new MemoryStream(byteArrayIn));
            return returnImage;
        }

    }
}