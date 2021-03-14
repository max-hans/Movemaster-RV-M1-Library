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

        private static void DeloreanCameraMove(MovemasterRobotArm robot)
        {
            const bool waiting = false;

            robot.SetToolLength(50);
            robot.SetSpeed(9);
            SetCameraFocus(16);
            robot.MoveTo(.0, +420.0, +290, -30, 0); // away
            if (waiting) Console.ReadLine();
            SetCameraFocus(16);
            robot.MoveTo(.0, +420.0, +290, -30, 0); // away
            if (waiting) Console.ReadLine();
            SetCameraFocus(89);
            robot.MoveTo(-230, 240, 40, -91, 90); // back 
            if (waiting) Console.ReadLine();
            SetCameraFocus(111);
            robot.MoveTo(-150, 340, 30, -90, 45); // back side
            if (waiting) Console.ReadLine();
            SetCameraFocus(91);
            robot.MoveTo(-10, 370, 30, -90, 0); // into the door
            if (waiting) Console.ReadLine();
            SetCameraFocus(91);
            robot.MoveTo(-10, 370, 30, -90, 0); // into the door
            if (waiting) Console.ReadLine();
            SetCameraFocus(132);
            robot.MoveTo(140, 330, 20, -90, -45); // front side
            if (waiting) Console.ReadLine();
            SetCameraFocus(101);
            robot.MoveTo(200, 230, 20, -92, -90); // front side
            if (waiting) Console.ReadLine();
            SetCameraFocus(127);
            robot.MoveTo(60, 230, 70, -92, -90); // direct on front
            if (waiting) Console.ReadLine();
            SetCameraFocus(125);
            robot.MoveTo(70, 270, 75, -92, -90); // door up
            if (waiting) Console.ReadLine();
            SetCameraFocus(172);
            robot.MoveTo(-10, 340, 70, -92, 0); // door up
            if (waiting) Console.ReadLine();
            SetCameraFocus(84);
            robot.MoveTo(-10, 325, 150, -40, 0); // door up
            if (waiting) Console.ReadLine();
            SetCameraFocus(33);
            robot.MoveTo(-260, 190, 140, -40, 100); // away back
            if (waiting) Console.ReadLine();
            SetCameraFocus(28);
            robot.MoveTo(0, 450, 190, -50, 0); // door up
        }

        static void SetCameraFocus(int focus)
        {
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
