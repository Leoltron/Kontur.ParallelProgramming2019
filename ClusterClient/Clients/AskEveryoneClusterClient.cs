using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    public class AskEveryoneClusterClient : ClusterClientBase
    {
        public AskEveryoneClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            Log.InfoFormat("Processing query {0}", query);
            var result = await ProcessRequestNoTimeoutAsync(query).LimitByTimeout(timeout);
            return result.ValueOrThrow;
        }

        private async Task<string> ProcessRequestNoTimeoutAsync(string query)
        {
            var tasksWithUris = GetNewIterationReplicaAddresses()
                               .Select(uri => (ProcessRequestAsync(CreateRequest(uri, query)), uri))
                               .ToList();
            var firstFinishedTask = await Task.WhenAny(tasksWithUris.Select(taskAndUri => taskAndUri.Item1));
            CancelUnfinishedRequests(query, tasksWithUris);

            return firstFinishedTask.Result;
        }
    }
}
