using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Cluster
{
    internal class ClusterServer
    {
        private const int Running = 1;
        private const int NotRunning = 0;

        private readonly ILog log;

        private readonly ConcurrentDictionary<(Guid, string), CancellationTokenSource> runningTasks =
            new ConcurrentDictionary<(Guid, string), CancellationTokenSource>();

        private HttpListener httpListener;

        private int isRunning = NotRunning;


        private int RequestsCount;

        public ClusterServer(ServerOptions serverOptions, ILog log)
        {
            ServerOptions = serverOptions;
            this.log = log;
        }

        public ServerOptions ServerOptions { get; }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref isRunning, Running, NotRunning) == NotRunning)
            {
                httpListener = new HttpListener
                {
                    Prefixes =
                    {
                        $"http://+:{ServerOptions.Port}/{ServerOptions.MethodName}/"
                    }
                };

                Console.WriteLine($"Server is starting listening prefixes: {string.Join(";", httpListener.Prefixes)}");

                if (ServerOptions.Async)
                {
                    Console.WriteLine("Press ENTER to stop listening");
#pragma warning disable 4014
                    httpListener.StartProcessingRequestsAsync(CreateAsyncCallback(ServerOptions));
#pragma warning restore 4014
                }
                else
                {
                    httpListener.StartProcessingRequestsSync(CreateSyncCallback(ServerOptions));
                }
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref isRunning, NotRunning, Running) == Running)
            {
                Console.WriteLine($"Stopping {ServerOptions.Port}");
                httpListener.Stop();
            }
        }

        private Action<HttpListenerContext> CreateSyncCallback(ServerOptions parsedOptions)
        {
            return context =>
            {
                var currentRequestId = Interlocked.Increment(ref RequestsCount);
                log.InfoFormat("Thread #{0} received request #{1} at {2}",
                               Thread.CurrentThread.ManagedThreadId, currentRequestId, DateTime.Now.TimeOfDay);

                Thread.Sleep(parsedOptions.MethodDuration);

                var encryptedBytes = ClusterHelpers.GetBase64HashBytes(context.Request.QueryString["query"]);
                context.Response.OutputStream.Write(encryptedBytes, 0, encryptedBytes.Length);

                log.InfoFormat("Thread #{0} sent response #{1} at {2}",
                               Thread.CurrentThread.ManagedThreadId, currentRequestId,
                               DateTime.Now.TimeOfDay);
            };
        }

        private Func<HttpListenerContext, Task> CreateAsyncCallback(ServerOptions options)
        {
            return async context =>
            {
                var currentRequestNum = Interlocked.Increment(ref RequestsCount);
                Console.WriteLine("Thread #{0} received request #{1} at {2}",
                                  Thread.CurrentThread.ManagedThreadId, currentRequestNum, DateTime.Now.TimeOfDay);

                var query = context.Request.QueryString["query"];
                var token = CancellationToken.None;
                if (Guid.TryParse(context.Request.QueryString["clientId"], out var clientId))
                {
                    var key = (clientId, query);

                    if (runningTasks.TryRemove(key, out var existingCts))
                    {
                        Console.WriteLine("Thread #{0} received request cancel #{1} at {2}",
                                          Thread.CurrentThread.ManagedThreadId, currentRequestNum,
                                          DateTime.Now.TimeOfDay);
                        existingCts.Cancel();
                        return;
                    }

                    var cts = new CancellationTokenSource();
                    token = cts.Token;
                    runningTasks[key] = cts;
                }

                await Task.Delay(options.MethodDuration, token);

                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Thread #{0} request has been cancelled #{1} at {2}",
                                      Thread.CurrentThread.ManagedThreadId, currentRequestNum,
                                      DateTime.Now.TimeOfDay);
                    return;
                }

                var encryptedBytes = ClusterHelpers.GetBase64HashBytes(query);
                await context.Response.OutputStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length);

                Console.WriteLine("Thread #{0} sent response #{1} at {2}",
                                  Thread.CurrentThread.ManagedThreadId, currentRequestNum,
                                  DateTime.Now.TimeOfDay);
            };
        }
    }
}
