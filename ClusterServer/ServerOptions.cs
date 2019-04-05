using System;
using Fclp;

namespace Cluster
{
    internal class ServerOptions
    {
        private static readonly FluentCommandLineParser<ServerOptions> argParser;

        static ServerOptions()
        {
            argParser = new FluentCommandLineParser<ServerOptions>();
            argParser.Setup(a => a.Port)
                     .As(CaseType.CaseInsensitive, "p", "port")
                     .Required();

            argParser.Setup(a => a.MethodName)
                     .As(CaseType.CaseInsensitive, "n", "name")
                     .Required();

            argParser.Setup(a => a.MethodDuration)
                     .As(CaseType.CaseInsensitive, "d", "duration")
                     .WithDescription("Server will return his response in <duration> ms")
                     .Required();

            argParser.Setup(a => a.Async)
                     .As(CaseType.CaseInsensitive, "a", "async")
                     .SetDefault(false);

            argParser.SetupHelp("?", "h", "help")
                     .Callback(text => Console.WriteLine(text));
        }

        public int Port { get; set; }
        public string MethodName { get; set; }
        public int MethodDuration { get; set; }
        public bool Async { get; set; }

        public static bool TryGetArguments(string[] args, out ServerOptions parsedOptions)
        {
            var parsingResult = argParser.Parse(args);

            if (parsingResult.HasErrors)
            {
                argParser.HelpOption.ShowHelp(argParser.Options);
                parsedOptions = null;
                return false;
            }

            parsedOptions = argParser.Object;
            return !parsingResult.HasErrors;
        }
    }
}
