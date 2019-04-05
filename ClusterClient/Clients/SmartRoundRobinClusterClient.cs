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
            var traverseOrder = GetIndexShuffle();
            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / traverseOrder.Count);

            var tasksWithUris = new List<(Task<string> task, string uri)>();
            var tasks = new List<Task<string>>();

            foreach (var replicaIndex in traverseOrder)
            {
                var task = ProcessRequestAsync(CreateRequest(ReplicaAddresses[replicaIndex], query));

                tasks.Add(task);
                tasksWithUris.Add((task, ReplicaAddresses[replicaIndex]));

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
