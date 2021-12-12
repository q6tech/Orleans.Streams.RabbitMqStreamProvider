using System;
using System.Diagnostics;

namespace Orleans.Streams.RabbitMqStreamProvider.Tests
{
    public static class ProcessExtension
    {
        public static void Terminate(this Process process)
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(3000))
            {
                process.Kill();
                if (!process.WaitForExit(3000))
                {
                    throw new Exception($"Cannot terminate the process {process.ProcessName}!");
                }
            }
        }
    }
}