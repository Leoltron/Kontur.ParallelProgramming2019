using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    public class RoundRobinClusterClient : ClusterClientBase
    {
        private readonly Random rand = new Random();

        public RoundRobinClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var traverseOrder = GetIndexShuffle();
            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / traverseOrder.Count);

            foreach (var replicaIndex in traverseOrder)
            {
                var result =
                    await ProcessRequestAsync(CreateRequest($"{ReplicaAddresses[replicaIndex]}?query={query}"))
                       .LimitByTimeout(timeout);

                if (!result.IsTimeout)
                    return result.Value;

                CancelRequest(query, ReplicaAddresses[replicaIndex]);
            }

            throw new TimeoutException();
        }


        protected List<int> GetIndexShuffle()
        {
            var indices = Enumerable.Range(0, ReplicaAddresses.Length).ToList();
            indices.Shuffle(rand);
            return indices;
        }
    }
}
