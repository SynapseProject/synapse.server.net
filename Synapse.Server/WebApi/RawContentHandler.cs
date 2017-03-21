using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Synapse.Services
{
    public class RawContentHandler : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if( request.Content != null )
                request.Properties["body"] = await request.Content.ReadAsStringAsync();

            return await base.SendAsync( request, cancellationToken );
        }
    }
}