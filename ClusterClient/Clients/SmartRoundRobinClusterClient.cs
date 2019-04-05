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
            var addresses = GetNewIterationReplicaAddresses();
            var traverseOrder = GetIndexShuffle(addresses.Length);
            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / traverseOrder.Count);

            var tasksWithUris = new List<(Task<string> task, string uri)>();
            var tasks = new List<Task<string>>();

            foreach (var replicaIndex in traverseOrder)
            {
                var requestTask = ProcessRequestAsync(CreateRequest(addresses[replicaIndex], query));

                tasks.Add(requestTask);
                tasksWithUris.Add((task: requestTask, addresses[replicaIndex]));

                var result = await Task.WhenAny(tasks).LimitByTimeout(timeout);
                if (result.IsTimeout)
                    continue;

                CancelUnfinishedRequests(query, tasksWithUris);
                foreach (var (task, uri) in tasksWithUris)
                    if (!task.IsCompleted)
                        ReplicaGreyList.Add(uri, 1);
                return result.Value.Result;
            }

            throw new TimeoutException();
        }
    }
}
