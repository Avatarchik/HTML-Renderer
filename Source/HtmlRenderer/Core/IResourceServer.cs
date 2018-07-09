using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.Adapters;


namespace TheArtOfDev.HtmlRenderer.Core
{
    public class ResourceServerUpdatedEventArgs : EventArgs
    {
    }


    public interface IResourceServer : IDisposable
    {
        String Html { get; set; }
        CssData CssData { get; set; }

        event EventHandler<ResourceServerUpdatedEventArgs> Updated;

        Task Go(string href);
        CssData GetCssData(/*RAdapter adapter,*/ string href, Dictionary<string, string> attributes);
        RImage GetImage(/*RAdapter adapter, */ string href, Dictionary<string, string> attributes);
    }


    public class DefaultResourceServer : IResourceServer
    {
        string m_html;
        public string Html
        {
            get { return m_html; }
            set
            {
                if (m_html == value) return;
                m_html = value;
                RaiseUpdated();
            }
        }

        CssData m_cssData;
        public CssData CssData
        {
            get { return m_cssData; }
            set
            {
                if (m_cssData == value) return;
                m_cssData = value;
                RaiseUpdated();
            }
        }

        public event EventHandler<ResourceServerUpdatedEventArgs> Updated;
        void RaiseUpdated()
        {
            var handler = Updated;
            if (handler != null)
            {
                handler(this, new ResourceServerUpdatedEventArgs());
            }
        }

        public void Dispose()
        {
        }

        public CssData GetCssData(/*RAdapter adapter,*/ string href, Dictionary<string, string> attributes)
        {
            throw new NotImplementedException();
        }

        public RImage GetImage(/*RAdapter adapter,*/ string href, Dictionary<string, string> attributes)
        {
            throw new NotImplementedException();
        }

        public Task Go(string href)
        {
            throw new NotImplementedException();
        }
    }

    public static class ResourceServerFactory
    {
        public delegate IResourceServer Factory();

        static Factory s_func = () => new DefaultResourceServer();

        public static void Iniialize(Factory func)
        {
            s_func = func;
        }

        public static IResourceServer Create()
        {
            return s_func();
        }
    }
}
