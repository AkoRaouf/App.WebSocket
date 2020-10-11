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
    public class Client
    {
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        private static readonly IWebsocketClient client;
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

            var serverUrl = new Uri("ws://localhost:80/ws");

            using (IWebsocketClient webSocketClient = new WebsocketClient(serverUrl, factory))
            {
                webSocketClient.Name = "Bitmex";
                webSocketClient.ReconnectionHappened.Subscribe(type =>
                {
                    Log.Information($"Reconnection happened, type: {type}, url: {webSocketClient.Url}");
                });
                webSocketClient.DisconnectionHappened.Subscribe(info =>
                    Log.Warning($"Disconnection happened, type: {info.Type}"));

                //client.MessageReceived.Subscribe(msg =>
                //{
                //    Log.Information($"Message received: {msg}");
                //});

                Log.Information("Starting...");
                webSocketClient.Start().Wait();
                Log.Information("Started.");

                //Task.Run(() => StartSendingPing(client));
                //Task.Run(() => StartSendingPing1(client));
                //Task.Run(() => SwitchUrl(client));
                var res = SendAsync(webSocketClient).Result;

                Log.Information($"Message received: {res}");

                ExitEvent.WaitOne();
            }
        }

        private static void WaitUntilServerStarts()
        {
            Thread.Sleep(2000);
        }

        private async Task<string> SendAsync(IWebsocketClient client)
        {
            await Task.Delay(1000);

            return await Task.Run(() =>
            {
                string result = string.Empty;
                client.Send("ping");
                client.MessageReceived.Subscribe(msg =>
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
    }
}
