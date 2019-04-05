using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClusterClient.Clients;
using ConsoleTables;
using Fclp;
using log4net;
using log4net.Config;

namespace ClusterClient
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            if (!TryGetReplicaAddresses(args, out var replicaAddresses))
                return;

            try
            {
                var clients = new ClusterClientBase[]
                {
                    new RandomClusterClient(replicaAddresses),
                    new AskEveryoneClusterClient(replicaAddresses),
                    new RoundRobinClusterClient(replicaAddresses),
                    new SmartRoundRobinClusterClient(replicaAddresses),
                    new PacketSmartRoundRobinClusterClient(replicaAddresses)
                };
                var queries = new[]
                {
                    "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
                    "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod",
                    "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"
                };


                var table = new ConsoleTable("Client", "Elapsed");

                foreach (var client in clients)
                {
                    Console.WriteLine("Testing {0} started", client.GetType());
                    var clientStopWatch = Stopwatch.StartNew();
                    TestClient(client, queries);
                    Console.WriteLine("Testing {0} finished.", client.GetType());
                    table.AddRow(client.GetType().Name, clientStopWatch.Elapsed);
                }

                Console.WriteLine("Results:");
                table.Write();
            }
            catch (Exception e)
            {
                Log.Fatal(e);
            }
        }

        private static void TestClient(ClusterClientBase client, IEnumerable<string> queries)
        {
            var queryTasks = queries.Select(q => ProcessQuery(client, q)).ToArray();
            Task.WaitAll(queryTasks);
        }

        private static async Task ProcessQuery(ClusterClientBase client, string query)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                await client.ProcessRequestAsync(query, TimeSpan.FromSeconds(6));

                Console.WriteLine("Processed query \"{0}\" in {1} ms", query, timer.ElapsedMilliseconds);
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Query \"{0}\" timeout ({1} ms)", query, timer.ElapsedMilliseconds);
            }
        }

        private static bool TryGetReplicaAddresses(string[] args, out string[] replicaAddresses)
        {
            var argumentsParser = new FluentCommandLineParser();
            string[] result = { };

            argumentsParser.Setup<string>(CaseType.CaseInsensitive, "f", "file")
                           .WithDescription("Path to the file with replica addresses")
                           .Callback(fileName => result = File.ReadAllLines(fileName))
                           .Required();

            argumentsParser.SetupHelp("?", "h", "help")
                           .Callback(text => Console.WriteLine(text));

            var parsingResult = argumentsParser.Parse(args);

            if (parsingResult.HasErrors)
            {
                argumentsParser.HelpOption.ShowHelp(argumentsParser.Options);
                replicaAddresses = null;
                return false;
            }

            replicaAddresses = result;
            return !parsingResult.HasErrors;
        }
    }
}
