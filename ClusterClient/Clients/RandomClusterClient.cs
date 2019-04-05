using System;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    public class RandomClusterClient : ClusterClientBase
    {
        private readonly Random random = new Random();

        public RandomClusterClient(string[] replicaAddresses)
            : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var addresses = GetNewIterationReplicaAddresses();
            var uri = addresses[random.Next(addresses.Length)];

            var webRequest = CreateRequest($"{uri}?query={query}");

            Log.InfoFormat("Processing {0}", webRequest.RequestUri);

            var resultTask = ProcessRequestAsync(webRequest);
            await Task.WhenAny(resultTask, Task.Delay(timeout));

            if (resultTask.IsCompleted)
                return resultTask.Result;

            ReplicaGreyList.Add(uri, 4);
            throw new TimeoutException();
        }
    }
}
