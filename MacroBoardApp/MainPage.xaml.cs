using System;
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
using System.Net.NetworkInformation;

namespace MacroBoardApp
{
    public partial class MainPage : ContentPage
    {
        NetworkStream stream;
        string localAddr;
        public TcpClient clientSender;
        public TcpClient client;
        ObservableCollection<Workflow> WfList { get; set; }
        public Observable<string> btnName { get; set; } = new Observable<string>("Connect");
        public double widthPhone;

        public Observable<bool> btnConnectIsEnable { get; set; } = new Observable<bool>(true);
        public Observable<bool> btnDisconnectIsEnable { get; set; } = new Observable<bool>(false);

        private bool isConnected { get; set; } = false;

        public MainPage()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NjUxNTAyQDMyMzAyZTMxMmUzMGJsbW9vVzMxaUtEdnVRbWNOYVdpc2VtYUdOREIyQjZyUCt6VWRCK0hGbDg9");

            InitializeComponent();
            BindingContext = this;

            WfList = new ObservableCollection<Workflow>();
            WfList_XAML.ItemsSource = WfList;
            widthPhone = DeviceDisplay.MainDisplayInfo.Width / 6.25;

            if(Preferences.ContainsKey("IP"))
                IpBar.Text = Preferences.Get("IP", "default");
            
        }

        private void Btn_Connect_Clicked(object sender, EventArgs e)
        {
            btnConnectIsEnable.Value = false;
            btnDisconnectIsEnable.Value = false;
            btnName.Value = "Waiting...";
            WfList.Clear();

            Thread thread = new Thread(() => Reload_Workflows());
            thread.Start();

            Thread.Sleep(1000);

            if (!isConnected)
            {
                
                if (client != null)
                {
                    if (stream != null)
                    {
                        byte[] serverResponse = new byte[50];

                        stream.Write(Encoding.ASCII.GetBytes("Quit"), 0, 4);
                        stream.Read(serverResponse, 0, serverResponse.Length);
                        Console.WriteLine(Encoding.ASCII.GetString(serverResponse));

                        stream.Close();
                    }

                    if (client.GetStream() != null)
                        client.GetStream().Close();

                    client.Close();
                }

                client = null;
                stream = null;

                thread.Abort();

                btnConnectIsEnable.Value = true;
                btnName.Value = "Connect";
                
            }
        }

        private void Btn_Disconnect_Clicked(object sender, EventArgs e)
        {
            WfList.Clear();
            btnName.Value = "Connect";

            if (stream == null) return;

            btnDisconnectIsEnable.Value = false;

            byte[] serverResponse = new byte[50];

            stream.Write(Encoding.ASCII.GetBytes("Quit"), 0, 4);
            stream.Read(serverResponse, 0, serverResponse.Length);
            Console.WriteLine(Encoding.ASCII.GetString(serverResponse));

            if (clientSender.GetStream() != null) clientSender.GetStream().Close();
            if (client.GetStream() != null) client.GetStream().Close();
            client.Close();
            clientSender.Close();

            client = null;
            clientSender = null;
        }

        private void Reload_Workflows()
        {
            if(client == null)
            {
                localAddr = IpBar.Text;

                Preferences.Set("IP", IpBar.Text);
                Console.WriteLine(Preferences.Get("IP", "default"));

                try
                {
                    isConnected = false;
                    client = new TcpClient(localAddr, 13000);
                    stream = client.GetStream();
                    isConnected = true;
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se.Message);
                    btnConnectIsEnable.Value = true;
                    btnDisconnectIsEnable.Value = false;
                    btnName.Value = "Connect";
                    return;
                }
            }

            isConnected = true;

            try
            {
                byte[] serverResponse = new byte[50];

                stream.Write(Encoding.ASCII.GetBytes("Continue"), 0, 8);
                stream.Read(serverResponse, 0, serverResponse.Length);
                Console.WriteLine(Encoding.ASCII.GetString(serverResponse));

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

                    
                }

                Console.WriteLine("Everything Received");

                if (clientSender == null)
                    clientSender = new TcpClient(localAddr, 14000);

                Trace.WriteLine("Connected");

                btnName.Value = "Refresh";

                btnConnectIsEnable.Value = true;
                btnDisconnectIsEnable.Value = true;
            }
            catch (Exception)
            {
                WfList.Clear();
                btnConnectIsEnable.Value = true;
                btnDisconnectIsEnable.Value = false;
                btnName.Value = "Connect";
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


        public class Observable<T> : INotifyPropertyChanged
        {
            public Observable(T initialValue)
            {
                this.Value = initialValue;
            }

            private T _value;
            public T Value
            {
                get { return _value; }
                set { _value = value; NotifyPropertyChanged("Value"); }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            internal void NotifyPropertyChanged(String propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }

    }
}