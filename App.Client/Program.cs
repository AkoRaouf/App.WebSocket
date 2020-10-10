using Serilog;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace App.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket
                {
                    Options =
                    {
                        KeepAliveInterval = TimeSpan.FromSeconds(5),
                        // Proxy = ...
                        // ClientCertificates = ...
                    }
                };
                //client.Options.SetRequestHeader("Origin", "xxx");
                return client;
            });

            var url = new Uri("wss://localhost:5000");

            using (IWebsocketClient client = new WebsocketClient(url, factory))
            {
                client.Name = "Bitmex";
                client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ErrorReconnectTimeout = TimeSpan.FromSeconds(30);
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

                //ExitEvent.WaitOne();
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
    }
}
