using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MovemasterHttpServer.Hubs
{
    public class RobotHub : Hub
    {
        // This method can be called by clients
        public async Task SendMessage(string message)
        {
            // This broadcasts to all connected clients
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        // Special method that runs when a client connects
        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        // Special method that runs when a client disconnects
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}