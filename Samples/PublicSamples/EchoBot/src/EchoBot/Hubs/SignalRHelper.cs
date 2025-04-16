using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace EchoBot.Hubs
{
    public static class SignalRHelper
    {
        private static IServiceHubContext _hubContext;

        public static async Task InitializeAsync(string signalRConnectionString)
        {
            var serviceManager = new ServiceManagerBuilder()
                .WithOptions(o => o.ConnectionString = signalRConnectionString)
                .BuildServiceManager();

            // Fix: Add a CancellationToken parameter to match the method signature
            _hubContext = await serviceManager.CreateHubContextAsync("TranscriptHub", CancellationToken.None);
        }

        public static IServiceHubContext HubContext => _hubContext;

        public static async Task DisposeAsync() => await _hubContext.DisposeAsync();
    }
}
