using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable CommentTypo

namespace ClusterClient.Clients
{
    /// <summary>
    ///     То же что и SmartRoundRobin, но за один "круг" опрашивает не один сервер, а <see cref="PacketSize" />
    /// </summary>
    public class PacketSmartRoundRobinClusterClient : SmartRoundRobinClusterClient
    {
        public PacketSmartRoundRobinClusterClient(string[] replicaAddresses, int packetSize = 4) : base(
            replicaAddresses)
        {
            PacketSize = packetSize;
        }

        private int PacketSize { get; }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var traverseOrder = GetIndexShuffle();
            var packetsCount = Math.Ceiling((double) traverseOrder.Count / PacketSize);
            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / packetsCount);

            var tasksWithUris = new List<(Task<string> task, string uri)>();
            var tasks = new List<Task<string>>();

            foreach (var replicaIndices in traverseOrder.Split(PacketSize))
            {
                var newTasks = replicaIndices
                              .Select(i => ReplicaAddresses[i])
                              .Select(uri => (ProcessRequestAsync(CreateRequest(uri, query)), uri))
                              .ToList();

                tasks.AddRange(newTasks.Select(taskWithUri => taskWithUri.Item1));
                tasksWithUris.AddRange(newTasks);

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
