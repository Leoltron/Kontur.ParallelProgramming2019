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
            var addresses = GetNewIterationReplicaAddresses();
            var traverseOrder = GetIndexShuffle(addresses.Length);
            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / traverseOrder.Count);

            foreach (var replicaIndex in traverseOrder)
            {
                var result =
                    await ProcessRequestAsync(CreateRequest($"{addresses[replicaIndex]}?query={query}"))
                       .LimitByTimeout(timeout);

                if (!result.IsTimeout)
                    return result.Value;

                ReplicaGreyList.Add(addresses[replicaIndex], 2);
                CancelRequest(query, addresses[replicaIndex]);
            }

            throw new TimeoutException();
        }


        protected List<int> GetIndexShuffle(int length)
        {
            var indices = Enumerable.Range(0, length).ToList();
            indices.Shuffle(rand);
            return indices;
        }
    }
}
