using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.Adapters;


namespace TheArtOfDev.HtmlRenderer.Core
{
    public interface IResourceServer: IDisposable
    {
        void SetHtml(string html);
        Task<String> GetHtmlAsync();

        void SetCssData(CssData cssData);
        Task<CssData> GetCssDataAsync();

        Task<CssData> GetCssDataAsync(string location, Dictionary<string, string> attributes);
        Task<RImage> GetImageAsync(string location, Dictionary<string, string> attributes);
    }

    public static class ResourceServerFactory
    {
        public delegate IResourceServer Factory();

        static Factory s_func;

        public static void Iniialize(Factory func)
        {
            s_func = func;
        }

        public static IResourceServer Create()
        {
            return s_func();}
    }
}
