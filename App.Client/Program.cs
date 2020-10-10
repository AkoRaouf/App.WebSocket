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
            InitLogging();
            Thread.Sleep(2000);
            var factory = new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket
                {
                    Options =
                    {
                        KeepAliveInterval = TimeSpan.FromSeconds(5000),
                        // Proxy = ...
                        // ClientCertificates = ...
                    }
                };
                //client.Options.SetRequestHeader("Origin", "xxx");
                return client;
            });

            var url = new Uri("ws://localhost:80/ws");

            using (IWebsocketClient client = new WebsocketClient(url, factory))
            {
                client.Name = "Bitmex";
                client.ReconnectTimeout = TimeSpan.FromSeconds(1);
                client.ErrorReconnectTimeout = TimeSpan.FromSeconds(1);
                client.ReconnectionHappened.Subscribe(type =>
                {
                    Log.Information($"Reconnection happened, type: {type}, url: {client.Url}");
                });
                client.DisconnectionHappened.Subscribe(info =>
                    Log.Warning($"Disconnection happened, type: {info.Type}"));

                client.MessageReceived.Subscribe(msg =>
                {
                    Log.Information($"Message received: {msg}");
                });

                Log.Information("Starting...");
                client.Start().Wait();
                Log.Information("Started.");

                Task.Run(() => StartSendingPing(client));
                //Task.Run(() => SwitchUrl(client));

                ExitEvent.WaitOne();
            }
        }
        
        private static async Task StartSendingPing(IWebsocketClient client)
        {
            while (true)
            {
                await Task.Delay(1000);

                if (!client.IsRunning)
                    continue;

                client.Send("ping");
            }
        }

        private static void InitLogging()
        {
            var executingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            var logPath = Path.Combine(executingDir, "logs", "verbose.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.ColoredConsole(LogEventLevel.Verbose,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message} {NewLine}{Exception}")
                .CreateLogger();
        }
    }
}
