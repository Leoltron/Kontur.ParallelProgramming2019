using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class BalancingClusterClient : ClusterClientBase
    {
        public BalancingClusterClient(string[] replicaAddresses)
            : base(replicaAddresses)
        {
        }

        public async override Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var responseTasks = new Dictionary<WebRequest, Task<string>>();
            var globalTimer = Stopwatch.StartNew();

            foreach (var webRequest in ReplicaAddresses.Select(uri => CreateRequest(uri + "?query=" + query)))
            {
                Log.InfoFormat($"Processing {webRequest.RequestUri} {DateTime.Now.TimeOfDay}");

                var interRequestsDelay = new TimeSpan((timeout - globalTimer.Elapsed).Ticks / (ReplicaAddresses.Length - responseTasks.Count));
                if (interRequestsDelay < TimeSpan.Zero)
                    throw new TimeoutException();

                responseTasks[webRequest] = ProcessRequestAsync(webRequest);

                await Task.WhenAny(responseTasks.Values.Concat(new[] { Task.Delay(interRequestsDelay) }));

                var responseTask = responseTasks.Values.FirstOrDefault(t => t.IsCompleted);

                if (responseTask == null)
                    continue;

                foreach (var webRequestToAbort in responseTasks.Where(t => !t.Value.IsCompleted).Select(t => t.Key))
                {
                    webRequestToAbort.Abort();
                    Log.InfoFormat("WebRequest to {0} was aborted", webRequestToAbort.RequestUri);
                }

                return responseTask.Result;
            }

            throw new TimeoutException();
        }

        protected override ILog Log
        {
            get { return LogManager.GetLogger(typeof(BalancingClusterClient)); }
        }
    }
}