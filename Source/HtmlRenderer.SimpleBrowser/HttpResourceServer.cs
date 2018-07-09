﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                m_url =value;
                // url updated
                if (m_url.EndsWith("/"))
                {
                    m_baseUrl = m_url;
                }
                else
                {
                    m_baseUrl = Path.GetDirectoryName(m_url);
                }

                var uri = new Uri(m_url);
                var port = "";
                if(uri.Scheme=="http" && uri.Port != 80)
                {
                    port = ":" + uri.Port;
                }
                else  if(uri.Scheme=="https" && uri.Port !=443)
                {
                    port = ":" + uri.Port;
                }
                m_baseUrlWithoutPath = $"{uri.Scheme}://{uri.Host}{port}";
            }
        }
        string m_baseUrl;
        string BaseUrl
        {
            get { return m_baseUrl; }
        }
        string m_baseUrlWithoutPath;
        string BaseUrlWithoutPath
        {
            get { return m_baseUrlWithoutPath; }
        }
        public async Task Go(string url)
        {
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
            else if(href.StartsWith("/"))
            {
                // absolute path
                return BaseUrlWithoutPath + href;
            }
            else
            {
                // relative path
                return BaseUrl + href;
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
            public Task<HttpResult> Task;
            public Byte[] Bytes;

            HttpTask() { }

            public static HttpTask Create(Task<HttpResult> task, Action callback)
            {
                var httpTask = new HttpTask();
                httpTask.Task = task.ContinueWith(x =>
                {
                    httpTask.Bytes = x.Result.GetBodyBytes().ToArray();
                    callback();
                    return x.Result;
                });
                return httpTask;
            }

            CssData m_cssData;
            public CssData GetCssData(RAdapter adapter)
            {
                if (Bytes == null) return null;

                if (m_cssData == null)
                {
                    var css = Encoding.UTF8.GetString(Task.Result.GetBodyBytes().ToArray());
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
            var newTask = HttpTask.Create(m_session.GetAsync(url), RaiseUpdated);
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
            var newTask = HttpTask.Create(m_session.GetAsync(url), RaiseUpdated);
            PushTask(href, newTask);
            return null;
        }
    }
}
