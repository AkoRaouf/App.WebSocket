using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace App.Client
{
    class Program
    {
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            var clientConsumer = new ClientConsumer();
            clientConsumer.Do();
            ExitEvent.WaitOne();
        }

    }

    public class ClientConsumer
    {
        public async void Do()
        {
            var client = new Client();
            await Task.Run(async () =>
            {
                while (true)
                {
                    Console.Write("Please Enter the valid command:");
                    var arg = Console.ReadLine();
                    var response = await client.SendAsync(arg);
                    Log.Information($"The response is {response}");
                }
            });
        }
    }
}
