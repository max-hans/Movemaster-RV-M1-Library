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

namespace Demos
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var robot = new MovemasterRobotArm(comportName: "COM15"))
            {
                if (robot.SendCommandWithAnswer("WH", out string position))
                {
                    Console.WriteLine(position);
                }
                DeloreanCameraMove(robot);
                Console.WriteLine("done.");
            }
        }


        /// <summary>
        /// A short camera path to move an attached camera around a miniature delorean car model
        /// </summary>
        private static void DeloreanCameraMove(MovemasterRobotArm robot)
        {
            robot.SetToolLength(50);
            robot.SetSpeed(9);
            robot.MoveTo(.0, +420.0, +290, -30, 0); // away
            robot.MoveTo(.0, +420.0, +290, -30, 0); // away
            robot.MoveTo(-230, 240, 40, -91, 90); // back 
            robot.MoveTo(-150, 340, 30, -90, 45); // back side
            robot.MoveTo(-10, 370, 30, -90, 0); // into the door
            robot.MoveTo(-10, 370, 30, -90, 0); // into the door
            robot.MoveTo(140, 330, 20, -90, -45); // front side
            robot.MoveTo(200, 230, 20, -92, -90); // front side
            robot.MoveTo(60, 230, 70, -92, -90); // direct on front
            robot.MoveTo(70, 270, 75, -92, -90); // door up
            robot.MoveTo(-10, 340, 70, -92, 0); // door up
            robot.MoveTo(-10, 325, 150, -40, 0); // door up
            robot.MoveTo(-260, 190, 140, -40, 100); // away back
            robot.MoveTo(0, 450, 190, -50, 0); // door up
        }

     
        private static void Calibrate(MovemasterRobotArm robot)
        {
            robot.Reset();
            robot.SetGripPressure(startingGrippenForce: 15, retainedGrippingForce: 15, startGrippingForceRetentionTime: 3);
            robot.SetSpeed(49);
            robot.GripperClosed = true;
            robot.MoveTo(-260, 300, 10, -90, 0);
            robot.MoveDelta(0, 0, -10);
        }

    }
}
