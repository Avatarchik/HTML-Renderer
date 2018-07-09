using System;
using System.Windows.Forms;


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
            go_btn.Click += Go_btn_Click;
        }

        private async void Go_btn_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"go {url.Text}");
            await m_server.Go(url.Text);
            //htmlPanel1.Invalidate();
            htmlPanel1.Text = await m_server.GetHtmlAsync();
        }
    }
}
