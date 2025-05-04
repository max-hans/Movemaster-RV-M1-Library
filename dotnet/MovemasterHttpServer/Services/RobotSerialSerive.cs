using Microsoft.AspNetCore.SignalR;
using MovemasterHttpServer.Hubs;
using Movemaster_RV_M1_Library;

namespace MovemasterHttpServer.Services
{
    public class RobotSerialService
    {
        private readonly IHubContext<RobotHub> _hubContext;

        public RobotSerialService(IHubContext<RobotHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void SubscribeToRobot(MovemasterRobotArm robot)
        {
            // Subscribe to the robot's serial data event
            robot.OnSerialDataReceived += async (data) =>
            {
                // Broadcast the data to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceiveSerialData", data);
            };
        }
    }
}