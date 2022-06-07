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

        public Workflow(string Name, ImageSource ImgSource)
        {
            this.Name = Name;
            this.ImgSource = ImgSource;
        }
    }
}