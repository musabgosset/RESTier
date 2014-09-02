using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Microsoft.Data.Domain.Tests
{
    internal static class ODataTestHelpers
    {
        /// <summary>
        /// Creates a temporary http server and invoker to perform an OData test.
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="httpMethod"></param>
        /// <param name="requestContent"></param>
        /// <param name="expectedStatusCode"></param>
        /// <param name="registerOData">
        ///     An action that registers the OData endpoint with the given temporary http server and client.
        ///     For example, `(config, server) => WebApiConfig.Register(config);`
        /// </param>
        /// <param name="headers"></param>
        /// <param name="postProcessContentHandler"></param>
        /// <param name="baselineFileName"></param>
        /// <returns></returns>
        public static async Task TestRequest(
            string requestUri,
            HttpMethod httpMethod,
            HttpContent requestContent,
            HttpStatusCode expectedStatusCode,
            Action<HttpConfiguration, HttpServer> registerOData,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            Func<string, string> postProcessContentHandler = null,
            [CallerMemberName] string baselineFileName = "")
        {
            using (HttpConfiguration config = new HttpConfiguration())
            {
                using (HttpServer server = new HttpServer(config))
                using (HttpMessageInvoker client = new HttpMessageInvoker(server))
                {
                    registerOData(config, server);
                    HttpRequestMessage request = new HttpRequestMessage(httpMethod, requestUri);
                    try
                    {
                        request.Content = requestContent;
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                request.Headers.Add(header.Key, header.Value);
                            }
                        }

                        using (HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None))
                        {
                            await CheckResponse(response, expectedStatusCode, baselineFileName, postProcessContentHandler);
                        }
                    }
                    finally
                    {
                        request.DisposeRequestResources();
                        request.Dispose();
                    }
                }
            }
        }

        public static async Task CheckResponse(
            HttpResponseMessage response,
            HttpStatusCode expectedStatusCode,
            string baselineFileName,
            Func<string, string> postProcessContentHandler = null)
        {
            string content = await BaselineHelpers.GetFormattedContent(response);

            Assert.AreEqual(expectedStatusCode, response.StatusCode, "Response.StatusCode\r\nContent:\r\n{0}", content);

            if (content != null)
            {
                if (postProcessContentHandler != null)
                {
                    content = postProcessContentHandler(content);
                }

                BaselineHelpers.VerifyBaseline(baselineFileName, content);
            }
            else
            {
                BaselineHelpers.VerifyBaselineDoesNotExist(baselineFileName);
            }
        }
    }
}