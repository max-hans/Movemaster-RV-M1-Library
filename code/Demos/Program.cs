// Movemaster RV M1 Library
// https://github.com/Springwald/Movemaster-RV-M1-Library
//
// (C) 2021 Daniel Springwald, Bochum Germany
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
        static void Main(string[] args)
        {
            Demo().Wait();
        }

        private static async Task Demo()
        {
            using (var robot = await MovemasterRobotArm.CreateAsync(comportName: "COM15"))
            {
                var cmdResponse = await robot.SendCommandWithAnswer("WH");
                if (cmdResponse.Success)
                {
                    Console.WriteLine(cmdResponse.ResponseString);
                }
                //await DeloreanCameraMove(robot);
                await ToolPointRotation(robot);
                Console.WriteLine("done.");
            }
        }


        /// <summary>
        /// A short camera path to move an attached camera around a miniature delorean car model
        /// </summary>
        private static async Task DeloreanCameraMove(MovemasterRobotArm robot)
        {
            await robot.SetToolLength(50);
            await robot.SetSpeed(3);
            await robot.MoveTo(.0, +420.0, +290, -30, 0); // away
            await robot.MoveTo(.0, +420.0, +290, -30, 0); // away
            await robot.MoveTo(-230, 240, 40, -91, 90); // back 
            await robot.MoveTo(-150, 340, 30, -90, 45); // back side
            await robot.MoveTo(-10, 370, 30, -90, 0); // into the door
            await robot.MoveTo(-10, 370, 30, -90, 0); // into the door
            await robot.MoveTo(140, 330, 20, -90, -45); // front side
            await robot.MoveTo(200, 230, 20, -92, -90); // front side
            await robot.MoveTo(60, 230, 70, -92, -90); // direct on front
            await robot.MoveTo(70, 270, 75, -92, -90); // door up
            await robot.MoveTo(-10, 340, 70, -92, 0); // door up
            await robot.MoveTo(-10, 325, 150, -40, 0); // door up
            await robot.MoveTo(-260, 190, 140, -40, 100); // away back
            await robot.MoveTo(0, 450, 190, -50, 0); // door up
        }

        /// <summary>
        /// rotate tool around fixed position
        /// </summary>
        private static async Task ToolPointRotation(MovemasterRobotArm robot)
        {
            await robot.SetToolLength(102);
            await robot.SetSpeed(3);
            var toolX = 0;
            var toolY = 600;
            var toolZ = 500;
            // await robot.MoveTo(toolX, toolZ, toolY, -0, -0, 10);
            await robot.MoveTo(toolX, toolZ, toolY, -45, -0, 10);
            await robot.MoveTo(toolX, toolZ, toolY, 45, -0, 10);
        }


        private static async Task Test(MovemasterRobotArm robot)
        {
            await robot.Reset();
            await robot.SetGripPressure(startingGrippenForce: 15, retainedGrippingForce: 15, startGrippingForceRetentionTime: 3);
            await robot.SetSpeed(4);
            await robot.SetGripperClosed(true);
            await robot.MoveTo(-260, 300, 10, -90, 0);
            await robot.MoveDelta(0, 0, -10);
        }

    }
}
