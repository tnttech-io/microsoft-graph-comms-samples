using Microsoft.AspNetCore.SignalR;

namespace EchoBot.Hubs
{
    public class SpeechHub : Hub
    {
        public async Task BroadcastTranscript(string text)
        {
            await Clients.All.SendAsync("ReceiveTranscript", text);
        }
    }
}
