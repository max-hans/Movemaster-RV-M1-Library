// Movemaster RV M1 Library
// https://github.com/Springwald/Movemaster-RV-M1-Library
//
// (C) 2022 Daniel Springwald, Bochum Germany
// Springwald Software  -   www.springwald.de
// daniel@springwald.de -  +49 234 298 788 46
// All rights reserved
// Licensed under MIT License

using Movemaster_RV_M1_Library;
using System;
using System.Threading.Tasks;

namespace Demos
{
    class Program
    {
        private const string comportName = "COM3";

        static void Main(string[] args)
        {
            //Demo().Wait();

            var resinMoldSwinging = new DemoResinMoldSwinging(comportName);
            resinMoldSwinging.Run().Wait();
        }

        private static async Task Demo(string comportName)
        {
            using (var robot = await MovemasterRobotArm.CreateAsync(comportName: comportName))
            {
                var cmdResponse = await robot.SendCommandWithAnswer("WH"); // Read actual position from robot
                if (cmdResponse.Success)
                {
                    Console.WriteLine(cmdResponse.ResponseString); // write actual position to screen
                }

                await robot.Reset();
                await robot.SetGripPressure(startingGrippenForce: 15, retainedGrippingForce: 15, startGrippingForceRetentionTime: 3);
                await robot.SetSpeed(4);
                await robot.SetGripperClosed(true);
                await robot.MoveTo(-260, 300, 10, -90, 0);
                await robot.MoveDelta(0, 0, -10);

                Console.WriteLine("done.");
            }
        }

    }
}
