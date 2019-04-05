using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    public class RoundRobinClusterClient : ClusterClientBase
    {
        private readonly Random rand = new Random();

        private readonly Dictionary<string, TimeSpan> ReplicasResponseTime;

        public RoundRobinClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            ReplicasResponseTime = replicaAddresses.ToDictionary(addr => addr, addr => TimeSpan.Zero);
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var addresses = GetAddressesSortedByResponseTime();

            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / addresses.Count);

            foreach (var address in addresses)
            {
                var result = await ProcessRequestAsync(address, query).LimitByTimeout(timeout);

                if (!result.IsTimeout)
                    return result.Value;

                CancelRequest(query, address);
            }

            throw new TimeoutException();
        }

        protected List<string> GetAddressesSortedByResponseTime()
        {
            return ReplicasResponseTime.GetEntries().OrderBy(entry => entry.Value).Select(e => e.Key).ToList();
        }

        protected async Task<string> ProcessRequestAsync(string address, string query)
        {
            var sw = Stopwatch.StartNew();
            var result = await ProcessRequestAsync(CreateRequest(address, query));
            ReplicasResponseTime[address] = sw.Elapsed;
            return result;
        }

        protected List<int> GetIndexShuffle(int length)
        {
            var indices = Enumerable.Range(0, length).ToList();
            indices.Shuffle(rand);
            return indices;
        }
    }
}
