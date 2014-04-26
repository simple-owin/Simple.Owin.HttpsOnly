using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Simple.Owin;

namespace TestApp
{
    using AppFunc = Func<IDictionary<string,object>,Task>;

    public class OwinAppSetup
    {
        private static readonly byte[] Html = Encoding.Default.GetBytes("<h1>Secure!</h1>");
        public static void Setup(Action<Func<AppFunc, AppFunc>> use)
        {
            use(HttpsOnly.Create(44300, 301, new []{"/js/*", "/favicon.ico"}));
            use(n => e =>
            {
                e["owin.ResponseStatusCode"] = 200;
                ((IDictionary<string, string[]>)e["owin.ResponseHeaders"])["Content-Type"] = new[] { "text/html" };
                return ((Stream)e["owin.ResponseBody"]).WriteAsync(Html, 0, Html.Length);
            });
        }
    }
}