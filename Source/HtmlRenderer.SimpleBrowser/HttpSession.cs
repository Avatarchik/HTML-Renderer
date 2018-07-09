using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using System.Runtime.InteropServices.WindowsRuntime;
#else
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
#endif


namespace HtmlRenderer.SimpleBrowser
{
    static class HttpConst
    {
        public const string UserAgent = "HtmlRenderer";
    }

#if NETFX_CORE
    public class HttpResult
    {
    #region Response
        public int StatusCode { get; private set; }

        List<KeyValuePair<string, string>> m_headers = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> Headers { get { return m_headers; } }
        public string Cookie
        {
            get
            {
                foreach (var kv in Headers)
                {
                    if (kv.Key.ToLower() == "set-cookie")
                    {
                        return kv.Value;
                    }
                }
                throw new KeyNotFoundException("set-cookie");
            }
        }

        List<Byte> m_body = new List<byte>();
        public List<Byte> Body { get { return m_body; } }
        public string GetBodyString()
        {
            return Encoding.UTF8.GetString(Body.ToArray());
        }
    #endregion

        public HttpResult(int statusCode)
        {
            StatusCode = statusCode;
        }

        public void AddHeader(string key, string value)
        {
            m_headers.Add(new KeyValuePair<string, string>(key, value));
        }

        public void AddBody(IEnumerable<Byte> bytes)
        {
            m_body.AddRange(bytes);
        }

        public IEnumerable<Byte> GetBodyBytes(Encoding encoding = null)
        {
            return m_body;
        }
    }

    public static class Http
    {
        public static Task<HttpResult> PostForm(HttpSession session, string url,
            Dictionary<string, string> form)
        {
            var content = new HttpFormUrlEncodedContent(form);
            return Post(session, url, content);
        }

        public async static Task<HttpResult> Post(HttpSession session, string url,
            IHttpContent content)
        {
            var uri = new Uri(url);

            using (var response = await session.Client.PostAsync(uri, content))
            {
                var result = new HttpResult((int)response.StatusCode);
                foreach (var h in response.Headers)
                {
                    result.AddHeader(h.Key, h.Value);
                }

                // get body
                var buffer = await response.Content.ReadAsBufferAsync();
                result.AddBody(buffer.ToArray());

                return result;
            }
        }

        public static async Task<HttpResult> Get(HttpSession session, string url)
        {
            var uri = new Uri(url);
            using (var response = await session.Client.GetAsync(uri))
            {
                var result = new HttpResult((int)response.StatusCode);
                foreach (var h in response.Headers)
                {
                    result.AddHeader(h.Key, h.Value);
                }

                // get body
                var buffer = await response.Content.ReadAsBufferAsync();
                result.AddBody(buffer.ToArray());

                return result;
            }
        }
    }

    public class HttpSession
    {
        HttpBaseProtocolFilter m_filter;
        public HttpBaseProtocolFilter Filter
        {
            get { return m_filter; }
        }

        // Windows.Web.Httpの方。
        // System.Net.Httpではない。
        HttpClient m_client;
        public HttpClient Client
        {
            get { return m_client; }
        }

        public HttpSession()
        {
            m_filter = new HttpBaseProtocolFilter();
            m_filter.AllowAutoRedirect = false;
            m_filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            m_filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            m_filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            m_filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;

            m_client = new HttpClient(m_filter);
        }

        public void AddCookie(string key, string value, string domain)
        {
            var cookieManager = Filter.CookieManager;
            var cookie = new HttpCookie(key, domain, "/")
            {
                Value = value
            };
            cookieManager.SetCookie(cookie);
        }

        public Task<HttpResult> Get(string url)
        {
            return Http.Get(this, url);
        }
    }

#else
    public static class Cert
    {
        static bool OnRemoteCertificateValidationCallback(
          System.Object sender,
          X509Certificate certificate,
          X509Chain chain,
          SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static bool s_initialized;

        public static void Initialize()
        {
            if (s_initialized) return;
            s_initialized = true;

            ServicePointManager.ServerCertificateValidationCallback =
            new RemoteCertificateValidationCallback(OnRemoteCertificateValidationCallback);
        }
    }

    public class HttpResult
    {
        public HttpWebRequest Request;
        public HttpWebResponse Response;

        public string Cookie
        {
            get
            {
                return Response.Headers["Set-Cookie"];
            }
        }

        public IEnumerable<Byte> GetBodyBytes()
        {
            var buffer = new Byte[1024];
            using (var data = Response.GetResponseStream())
            {
                while(true)
                {
                    var readSize = data.Read(buffer, 0, buffer.Length);
                    if (readSize == 0)
                    {
                        break;
                    }
                    for(int i=0; i<readSize; ++i)
                    {
                        yield return buffer[i];
                    }
                }
            }
        }

        public HttpResult(HttpWebRequest request, HttpWebResponse response)
        {
            Request = request;
            Response = response;
        }
    }

    public static class Http
    {
        public static Task<HttpResult> Get(HttpSession session, string url)
        {
            var tcs = new TaskCompletionSource<HttpResult>();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var request = (HttpWebRequest)System.Net.WebRequest.Create(url);
                    request.CookieContainer = session.CookieContainer;
                    request.UserAgent = HttpConst.UserAgent;
                    //((HttpWebRequest)request).UserAgent = ".NET Framework Example Client";
                    var response = (HttpWebResponse)request.GetResponse();
                    tcs.SetResult(new HttpResult(request, response));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public static Task<HttpResult> PostForm(HttpSession session, string url,
            Dictionary<string, string> form)
        {
            var postData = string.Join("&", form.Select(kv => kv.Key + "=" + Uri.EscapeDataString(kv.Value)).ToArray());
            var postDataBytes = Encoding.ASCII.GetBytes(postData);
            return Post(session, url, "application/x-www-form-urlencoded", postDataBytes);
        }

        public static Task<HttpResult> Post(HttpSession session, string url,
            string contentType, Byte[] bytes)
        {
            var tcs = new TaskCompletionSource<HttpResult>();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var request = (HttpWebRequest)System.Net.WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = contentType;
                    request.ContentLength = bytes.Length;
                    request.AllowAutoRedirect = false;
                    request.CookieContainer = session.CookieContainer;
                    request.UserAgent = HttpConst.UserAgent;

                    using (var reqStream = request.GetRequestStream())
                    {
                        reqStream.Write(bytes, 0, bytes.Length);
                        reqStream.Close();
                    }

                    var response = (HttpWebResponse)request.GetResponse();
                    tcs.SetResult(new HttpResult(request, response));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }

    public class HttpSession
    {
        CookieContainer m_cc = new CookieContainer();
        public CookieContainer CookieContainer
        {
            get { return m_cc; }
        }

        public HttpSession()
        {
            Cert.Initialize();
        }

        public void AddCookie(string key, string value, string domain)
        {
            var cookie = new Cookie(key, value) { Domain = domain };
            m_cc.Add(cookie);
        }

        public Task<HttpResult> GetAsync(string url)
        {
            return Http.Get(this, url);
        }
    }
#endif
}
