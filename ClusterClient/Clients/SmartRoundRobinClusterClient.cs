using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClusterClient.Clients
{
    public class SmartRoundRobinClusterClient : RoundRobinClusterClient
    {
        public SmartRoundRobinClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var addresses = GetAddressesSortedByResponseTime();
            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / addresses.Count);

            var tasksWithUris = new List<(Task<string> task, string uri)>();
            var tasks = new List<Task<string>>();

            foreach (var address in addresses)
            {
                var requestTask = ProcessRequestAsync(address, query);

                tasks.Add(requestTask);
                tasksWithUris.Add((requestTask, address));

                var result = await Task.WhenAny(tasks).LimitByTimeout(timeout);
                if (result.IsTimeout)
                    continue;

                CancelUnfinishedRequests(query, tasksWithUris);
                return result.Value.Result;
            }

            throw new TimeoutException();
        }
    }
}
