using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Core;
using TheArtOfDev.HtmlRenderer.WinForms.Adapters;


namespace HtmlRenderer.SimpleBrowser
{
    class HttpResourceServer : IResourceServer
    {
        string m_url;
        string Url
        {
            get { return m_url; }
            set
            {
                if (m_url == value) return;
                m_url = value;
                // url updated
                if (m_url.EndsWith("/"))
                {
                    m_baseUrl = m_url;
                }
                else
                {
                    var splited = m_url.Split('/');
                    m_baseUrl = string.Join("/", splited.Take(splited.Length-1)) + "/";
                }

                var uri = new Uri(m_url);
                var port = "";
                if (uri.Scheme == "http" && uri.Port != 80)
                {
                    port = ":" + uri.Port;
                }
                else if (uri.Scheme == "https" && uri.Port != 443)
                {
                    port = ":" + uri.Port;
                }
                m_baseUrlWithoutPath = $"{uri.Scheme}://{uri.Host}{port}";

                m_schema = "http:";
                if (uri.Scheme != "http")
                {
                    m_schema = uri.Scheme + ":";
                }
            }
        }
        string m_baseUrl;
        string m_baseUrlWithoutPath;
        string m_schema;

        public async Task Go(string href)
        {
            var url = GetUrl(href);
            var result = await m_session.GetAsync(url);
            var bytes = result.GetBodyBytes();
            Url = url;
            Html = Encoding.UTF8.GetString(bytes.ToArray());
        }

        string GetUrl(string href)
        {
            if (href.StartsWith("http:")
                || href.StartsWith("https:"))
            {
                // external
                return href;
            }
            else if (href.StartsWith("//"))
            {
                // absolute path
                return m_schema + href;
            }
            else if (href.StartsWith("/"))
            {
                // absolute path
                return m_baseUrlWithoutPath + href;
            }
            else
            {
                // relative path
                return m_baseUrl + href;
            }
        }

        HttpSession m_session;

        public HttpResourceServer()
        {
            m_session = new HttpSession();
        }

        public void Dispose()
        {
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

        class HttpTask
        {
            public string Url;
            public Task<HttpResult> Task;
            public Byte[] Bytes;
            public Exception Error;

            HttpTask() { }

            public static HttpTask Create(string url, Task<HttpResult> task, Action callback)
            {
                Console.WriteLine($"[HttpTask.Create]{url}");
                var httpTask = new HttpTask
                {
                    Url = url
                };
                httpTask.Task = task.ContinueWith(x =>
                {
                    try
                    {
                        if (x.IsCompleted)
                        {
                            httpTask.Bytes = x.Result.GetBodyBytes();
                            callback();
                            return x.Result;
                        }
                    }
                    catch(Exception ex)
                    {
                        httpTask.Error = ex;
                        Console.WriteLine($"{httpTask.Url} {ex}");
                    }
                    return default(HttpResult);
                });
                return httpTask;
            }

            CssData m_cssData;
            public CssData GetCssData(RAdapter adapter)
            {
                if (Bytes == null) return null;

                if (m_cssData == null)
                {
                    var css = Encoding.UTF8.GetString(Bytes);
                    m_cssData = CssData.Parse(adapter, css);
                }
                return m_cssData;
            }

            RImage m_image;
            public RImage GetImage(RAdapter adapter)
            {
                if (Bytes == null) return null;

                if (m_image == null)
                {
                    using (var ms = new MemoryStream(Bytes))
                    {
                        m_image = adapter.ImageFromStream(ms);
                    }
                }
                return m_image;
            }
        }

        Dictionary<string, HttpTask> m_resultMap=new Dictionary<string, HttpTask>();
        void PushTask(string href, HttpTask body)
        {
            lock (((ICollection)m_resultMap).SyncRoot){
                m_resultMap[href] = body;
            }
        }
        HttpTask GetTask(string href)
        {
            lock (((ICollection)m_resultMap).SyncRoot) {
                HttpTask httpTask;
                if (!m_resultMap.TryGetValue(href, out httpTask))
                {
                    return null;
                }
                return httpTask;
            }
        }

        public CssData GetCssData(/*RAdapter adapter,*/ string href, Dictionary<string, string> attributes)
        {
            HttpTask task;
            if (m_resultMap.TryGetValue(href, out task))
            {
                // found
                var adapter = WinFormsAdapter.Instance;
                return task.GetCssData(adapter);
            }

            // not found. request
            var url = GetUrl(href);
            var newTask = HttpTask.Create(url, m_session.GetAsync(url), RaiseUpdated);
            PushTask(href, newTask);
            return null;
        }

        public RImage GetImage(/*RAdapter adapter,*/ string href, Dictionary<string, string> attributes)
        {
            HttpTask task;
            if (m_resultMap.TryGetValue(href, out task))
            {
                // found
                var adapter = WinFormsAdapter.Instance;
                return task.GetImage(adapter);
            }

            // not found. request
            var url = GetUrl(href);
            var newTask = HttpTask.Create(url, m_session.GetAsync(url), RaiseUpdated);
            PushTask(href, newTask);
            return null;
        }
    }
}
