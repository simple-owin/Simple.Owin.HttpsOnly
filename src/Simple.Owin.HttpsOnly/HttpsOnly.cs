namespace Simple.Owin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a middleware function to redirect all incoming HTTP requests to HTTPS
    /// </summary>
    public class HttpsOnly
    {
        private const string OwinRequestScheme = "owin.RequestScheme";
        private const string OwinRequestHeaders = "owin.RequestHeaders";
        private const string OwinRequestPathBase = "owin.RequestPathBase";
        private const string OwinRequestPath = "owin.RequestPath";
        private const string OwinRequestQueryString = "owin.RequestQueryString";
        private const string OwinResponseHeaders = "owin.ResponseHeaders";
        private const string OwinResponseStatusCode = "owin.ResponseStatusCode";

        private static readonly int[] ValidRedirectCodes = {301, 303, 307};

        private readonly int _redirectCode;
        private readonly int _port;
        private readonly bool _checkAbsoluteExclusions;
        private readonly HashSet<string> _absoluteExclusions;
        private readonly bool _checkPartialExclusions;
        private readonly HashSet<string> _partialExclusions;

        private HttpsOnly(int port, int redirectCode, string[] exclusions)
        {
            if (!ValidRedirectCodes.Contains(redirectCode))
            {
                throw new ArgumentOutOfRangeException("redirectCode", "Redirect code must be either 301, 303 or 307.");
            }
            _port = port;
            _redirectCode = redirectCode;

            if (exclusions != null)
            {
                var partialExclusions = exclusions.Where(e => e.EndsWith("/*")).Select(e => e.TrimEnd('*')).ToArray();
                if (partialExclusions.Length > 0)
                {
                    _partialExclusions = new HashSet<string>(partialExclusions);
                    _checkPartialExclusions = true;
                }

                var absoluteExclusions = exclusions.Where(e => !e.EndsWith("/*")).ToArray();
                if (absoluteExclusions.Length > 0)
                {
                    _absoluteExclusions = new HashSet<string>(absoluteExclusions);
                    _checkAbsoluteExclusions = true;
                }
            }
        }

        /// <summary>
        /// Creates the AppFunc delegate to use in the OWIN setup.
        /// </summary>
        /// <param name="port">The HTTPS port to redirect to.</param>
        /// <param name="redirectCode">The redirect code to use. May be 301 (default), 303 or 307.</param>
        /// <param name="exclusions">A list of paths to exclude from the redirect. Wildcards may be applied but only at the first folder level, e.g. <c>/Scripts/*</c>.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">redirectCode;Redirect code must be either 301, 303 or 307.</exception>
        public static Func<IDictionary<string, object>, Func<IDictionary<string, object>, Task>, Task> Create(int port = 443,
            int redirectCode = 301, string[] exclusions = null)
        {
            if (!ValidRedirectCodes.Contains(redirectCode))
            {
                throw new ArgumentOutOfRangeException("redirectCode", "Redirect code must be either 301, 303 or 307.");
            }
            return new HttpsOnly(port, redirectCode, exclusions).AppFunc;
        }

        private Task AppFunc(IDictionary<string, object> env, Func<IDictionary<string, object>, Task> next)
        {
            var scheme = (string) env[OwinRequestScheme];
            if (!scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                return next(env);
            }
            if (_checkAbsoluteExclusions && _absoluteExclusions.Contains((string) env[OwinRequestPath]))
            {
                return next(env);
            }
            if (_checkPartialExclusions)
            {
                var path = (string) env[OwinRequestPath];
                var start = path.Substring(0, path.IndexOf('/', 1) + 1);
                if (start != string.Empty && _partialExclusions.Contains(start))
                {
                    return next(env);
                }
            }
            var headers = (IDictionary<string, string[]>) env[OwinRequestHeaders];
            var host = headers["Host"].First();

            var colon = host.IndexOf(':');
            if (colon > 0)
            {
                host = host.Substring(0, colon);
            }
            if (_port != 443)
            {
                host += ":" + _port;
            }
            var uri = string.Concat("https://", host,
                (string) env[OwinRequestPathBase], (string) env[OwinRequestPath]);

            if (!string.IsNullOrWhiteSpace((string)env[OwinRequestQueryString]))
                uri += "?" + (string)env[OwinRequestQueryString];

            ((IDictionary<string, string[]>) env[OwinResponseHeaders])["Location"] = new []{uri};
            env[OwinResponseStatusCode] = _redirectCode;
            return TaskHelper.Completed;
        }
    }
}
