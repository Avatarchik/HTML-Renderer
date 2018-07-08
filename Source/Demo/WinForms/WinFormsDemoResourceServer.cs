using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Core;
using TheArtOfDev.HtmlRenderer.Demo.Common;
using TheArtOfDev.HtmlRenderer.WinForms.Adapters;


namespace TheArtOfDev.HtmlRenderer.Demo.WinForms
{
    public class WinFormsDemoResourceServer : IResourceServer
    {
        /// <summary>
        /// Cache for resource images
        /// </summary>
        private readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        string m_html;
        public void SetHtml(string html)
        {
            m_html=html;
        }

        public async Task<string> GetHtmlAsync()
        {
            return m_html;
        }

        CssData m_cssData;
        public void SetCssData(CssData cssData)
        {
            m_cssData=cssData;
        }

        public async Task<CssData> GetCssDataAsync()
        {
            return m_cssData;
        }

        public WinFormsDemoResourceServer(string html="", CssData cssData=null)
        {
            m_html = html;
            m_cssData = cssData;
        }

        public void Dispose()
        {
        }

        public async Task<CssData> GetCssDataAsync(string src, Dictionary<string, string> attributes)
        {
            return CssData.Parse(WinFormsAdapter.Instance, DemoUtils.GetStylesheet(src));
        }

        /// <summary>
        /// Get image by resource key.
        /// </summary>
        public Image TryLoadResourceImage(string src)
        {
            Image image;
            if (!_imageCache.TryGetValue(src, out image))
            {
                var imageStream = DemoUtils.GetImageStream(src);
                if (imageStream != null)
                {
                    image = Image.FromStream(imageStream);
                    _imageCache[src] = image;
                }
            }
            return image;
        }

        public virtual async Task<RImage> GetImageAsync(string location, Dictionary<string, string> attributes)
        {
            var imgObj = TryLoadResourceImage(location);
            if (attributes != null)
            {
                if (attributes.ContainsKey("byevent"))
                {
                    int delay;
                    if (Int32.TryParse(attributes["byevent"], out delay))
                    {
                        await Task.Run(() =>
                        {
                            Thread.Sleep(delay);
                        });

                        //e.Callback("https://fbcdn-sphotos-a-a.akamaihd.net/hphotos-ak-snc7/c0.44.403.403/p403x403/318890_10151195988833836_1081776452_n.jpg");
                        //return;
                        throw new NotImplementedException();
                    }
                    else
                    {
                        //e.Callback("http://sphotos-a.xx.fbcdn.net/hphotos-ash4/c22.0.403.403/p403x403/263440_10152243591765596_773620816_n.jpg");
                        throw new NotImplementedException();
                    }
                }
                else if (attributes.ContainsKey("byrect"))
                {
                    var split = attributes["byrect"].Split(',');
                    var rect = new Rectangle(Int32.Parse(split[0]), Int32.Parse(split[1]), Int32.Parse(split[2]), Int32.Parse(split[3]));

                    if (imgObj != null)
                    {
                        return WinFormsAdapter.Instance.ConvertImage(imgObj);
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //return TryLoadResourceImage("htmlicon"), rect.X, rect.Y, rect.Width, rect.Height);
                    }
                }
            }

            return WinFormsAdapter.Instance.ConvertImage(imgObj);
        }
    }

    /*
    class WinFormsPdfResourceServer : WinfFormsResourceServer
    {
        /// <summary>
        /// Get image by resource key.
        /// </summary>
        public XImage TryLoadResourceXImage(string src)
        {
            var img = TryLoadResourceImage(src);
            XImage xImg;

            if (img == null)
                return null;

            using (var ms = new MemoryStream())
            {
                img.Save(ms, img.RawFormat);
                xImg = img != null ? XImage.FromStream(ms) : null;
            }

            return xImg;
        }

        public override async Task<RImage> GetImageAsync(string location, Dictionary<string, string> attributes)
        {
            var img = TryLoadResourceImage(location);
            XImage xImg = null;

            if (img != null)
            {
                using (var ms = new MemoryStream())
                {
                    img.Save(ms, img.RawFormat);
                    xImg = img != null ? XImage.FromStream(ms) : null;
                }
            }

            if (!e.Handled && e.Attributes != null)
            {
                if (e.Attributes.ContainsKey("byevent"))
                {
                    int delay;
                    if (Int32.TryParse(e.Attributes["byevent"], out delay))
                    {
                        e.Handled = true;
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            Thread.Sleep(delay);
                            e.Callback("https://fbcdn-sphotos-a-a.akamaihd.net/hphotos-ak-snc7/c0.44.403.403/p403x403/318890_10151195988833836_1081776452_n.jpg");
                        });
                        return;
                    }
                    else
                    {
                        e.Callback("http://sphotos-a.xx.fbcdn.net/hphotos-ash4/c22.0.403.403/p403x403/263440_10152243591765596_773620816_n.jpg");
                        return;
                    }
                }
                else if (e.Attributes.ContainsKey("byrect"))
                {
                    var split = e.Attributes["byrect"].Split(',');
                    var rect = new Rectangle(Int32.Parse(split[0]), Int32.Parse(split[1]), Int32.Parse(split[2]), Int32.Parse(split[3]));
                    e.Callback(imgObj ?? TryLoadResourceImage("htmlicon"), rect.X, rect.Y, rect.Width, rect.Height);
                    return;
                }
            }

            return xImg;
        }
    }
    */
}
