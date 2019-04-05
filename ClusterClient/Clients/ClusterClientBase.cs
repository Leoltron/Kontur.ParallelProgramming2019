using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Fclp.Internals.Extensions;
using log4net;

namespace ClusterClient.Clients
{
    public abstract class ClusterClientBase
    {
        protected ClusterClientBase(string[] replicaAddresses)
        {
            if (replicaAddresses.IsNullOrEmpty())
                throw new ArgumentException("No addresses has been given", nameof(replicaAddresses));
            ReplicaAddresses = replicaAddresses;
            Log = LogManager.GetLogger(GetType());
        }

        protected Guid clientId { get; } = Guid.NewGuid();

        protected string[] ReplicaAddresses { get; }
        protected ILog Log { get; }

        public abstract Task<string> ProcessRequestAsync(string query, TimeSpan timeout);

        protected static HttpWebRequest CreateRequest(string uriStr)
        {
            var request = WebRequest.CreateHttp(Uri.EscapeUriString(uriStr));
            request.Proxy = null;
            request.KeepAlive = true;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = 100500;
            return request;
        }

        protected HttpWebRequest CreateRequest(string uri, string query)
        {
            return CreateRequest($"{uri}?query={query}&clientId={clientId}");
        }

        protected async Task<string> ProcessRequestAsync(WebRequest request)
        {
            var timer = Stopwatch.StartNew();
            using (var response = await request.GetResponseAsync())
            {
                var result = await new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEndAsync();
                Log.InfoFormat("Response from {0} received in {1} ms", request.RequestUri, timer.ElapsedMilliseconds);
                return result;
            }
        }

        protected void CancelUnfinishedRequests(string query,
                                                IEnumerable<(Task<string> task, string uri)> tasksWithUris)
        {
            tasksWithUris
               .Where(taskAndUri => !taskAndUri.task.IsCompleted)
               .ForEach(taskAndUri => CancelRequest(query, taskAndUri.uri));
        }

        protected void CancelRequest(string query, string uri)
        {
            CreateRequest($"{uri}?query={query}&clientId={clientId}");
        }
    }
}
