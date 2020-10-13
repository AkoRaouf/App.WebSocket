using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace App.Client
{
    public class Client : IDisposable
    {
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        private readonly Uri serverUrl = new Uri("ws://localhost:80/ws");
        private readonly IWebsocketClient webSocketClient;
        public Client()
        {
            InitLogging();
            WaitUntilServerStarts();
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
            webSocketClient = new WebsocketClient(serverUrl, factory)
            {
                Name = "First Client"
            };
            webSocketClient.DisconnectionHappened.Subscribe(info =>
                Log.Warning($"Disconnection happened, type: {info.Type}"));

            Log.Information("Starting...");
            webSocketClient.Start().Wait();
            Log.Information("Started.");
            //var res = SendAsync("ping").Result;

            //Log.Information($"Message received: {res}");

            //ExitEvent.WaitOne();
        }

        private static void WaitUntilServerStarts()
        {
            Thread.Sleep(2000);
        }

        public async Task<string> SendAsync(string message)
        {
            await Task.Delay(1000);

            return await Task.Run(() =>
            {
                string result = string.Empty;
                webSocketClient.Send(message);
                webSocketClient.MessageReceived.Subscribe(msg =>
                {
                    result = msg.TextData;
                });
                return result;
            });
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

        public void Dispose()
        {
            webSocketClient.Dispose();
        }
    }
}
