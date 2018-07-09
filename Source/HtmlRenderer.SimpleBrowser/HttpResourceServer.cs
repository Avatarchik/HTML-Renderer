using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Core;


namespace HtmlRenderer.SimpleBrowser
{
    class HttpResourceServer : IResourceServer
    {
        string m_url;
        HttpSession m_session;

        public HttpResourceServer()
        {
            m_session = new HttpSession();
        }

        public async Task Go(string url)
        {
            var result = await m_session.Get(url);
            var bytes = result.GetBodyBytes();
            m_html = Encoding.UTF8.GetString(bytes.ToArray());
        }

        #region Interface
        public void Dispose()
        {
        }

        string m_html;
        public void SetHtml(string html)
        {
            m_html = html;
        }
        public async Task<string> GetHtmlAsync()
        {
            return m_html;
        }

        CssData m_cssData;
        public void SetCssData(CssData cssData)
        {
            m_cssData = cssData;
        }
        public async Task<CssData> GetCssDataAsync()
        {
            return m_cssData;
        }

        public Task<CssData> GetCssDataAsync(string location, Dictionary<string, string> attributes)
        {
            throw new NotImplementedException();
        }

        public Task<RImage> GetImageAsync(string location, Dictionary<string, string> attributes)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
