using System;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.Core.Entities;

namespace HtmlRenderer.SimpleBrowser
{
    public partial class Form1 : Form
    {
        HttpResourceServer m_server;

        public Form1()
        {
            InitializeComponent();

            m_server = new HttpResourceServer();
            htmlPanel1.ResourceServer = m_server;
            htmlPanel1.LinkClicked += HtmlPanel1_LinkClicked;
            go_btn.Click += Go_btn_Click;
        }

        private async void HtmlPanel1_LinkClicked(object sender, HtmlLinkClickedEventArgs e)
        {
            await m_server.Go(e.Link);
            //htmlPanel1.Text = m_server.Html;
        }

        private async void Go_btn_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"go {url.Text}");
            await m_server.Go(url.Text);
            htmlPanel1.Text = m_server.Html;
        }
    }
}
