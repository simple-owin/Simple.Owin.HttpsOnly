namespace TestApp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Simple.Owin;

    public class OwinAppSetup
    {
        private static readonly byte[] Html = Encoding.Default.GetBytes("<h1>Secure!</h1>");
        public static void Setup(Action<Func<IDictionary<string, object>, Func<IDictionary<string, object>, Task>, Task>> use)
        {
            use(HttpsOnly.Create(44300, 301, new []{"/js/*", "/favicon.ico"}));
            use((e, n) =>
            {
                e["owin.ResponseStatusCode"] = 200;
                ((IDictionary<string, string[]>)e["owin.ResponseHeaders"])["Content-Type"] = new[] { "text/html" };
                return ((Stream)e["owin.ResponseBody"]).WriteAsync(Html, 0, Html.Length);
            });
        }
    }
}