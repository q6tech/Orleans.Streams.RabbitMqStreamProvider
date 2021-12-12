using System;
using System.Diagnostics;
using Toxiproxy.Net;
using Toxiproxy.Net.Toxics;

namespace Orleans.Streams.RabbitMqStreamProvider.Tests
{
    internal static class ToxiProxyHelpers
    {
        public const string RmqPortEnvVar = "RMQ_PORT";
        public const string ProxyPortEnvVar = "RMQ_TOXI_PORT";
        private const string RmqProxyName = "RMQ";

        public static readonly int RmqPort = int.TryParse(Environment.GetEnvironmentVariable(RmqPortEnvVar), out var port) ? port : 5672;
        public static readonly int RmqProxyPort = int.TryParse(Environment.GetEnvironmentVariable(ProxyPortEnvVar), out var port) ? port : 5670;
        public static int ClientPort => CanRunProxy ? RmqProxyPort : RmqPort;

        public static bool CanRunProxy => true;

        public static Process StartProxy()
        {
            return null;
        }

        public static void AddLimitDataToRmqProxy(Connection connection, ToxicDirection direction, double toxicity, int timeout)
        {
            var proxy = connection.Client().FindProxyAsync(RmqProxyName).GetAwaiter().GetResult();
            proxy.AddAsync(new LimitDataToxic
            {
                Name = "Timeout",
                Toxicity = toxicity,
                Stream = direction,
                Attributes = new LimitDataToxic.ToxicAttributes
                {
                    Bytes = timeout
                }
            }).GetAwaiter().GetResult();
            proxy.UpdateAsync().GetAwaiter().GetResult();
        }

        public static void AddLatencyToRmqProxy(Connection connection, ToxicDirection direction, double toxicity, int latency, int jitter)
        {
            var proxy = connection.Client().FindProxyAsync(RmqProxyName).GetAwaiter().GetResult();
            proxy.AddAsync(new LatencyToxic
            {
                Name = "Latency",
                Toxicity = toxicity,
                Stream = direction,
                Attributes = new LatencyToxic.ToxicAttributes
                {
                    Latency = latency,
                    Jitter = jitter
                }
            }).GetAwaiter().GetResult();
            proxy.UpdateAsync().GetAwaiter().GetResult();
        }
    }
}