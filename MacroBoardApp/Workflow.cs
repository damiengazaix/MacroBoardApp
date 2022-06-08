using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MacroBoardApp
{
    internal class Workflow
    {
        public string Name { get; set; }
        public ImageSource ImgSource { get; set; }
        public double widthPhone { get; set; }
        public Workflow(string Name, ImageSource ImgSource, double widthPhone)
        {
            this.Name = Name;
            this.ImgSource = ImgSource;
            this.widthPhone = widthPhone;
        }
    }
}