// Movemaster RV M1 Library
// https://github.com/Springwald/Movemaster-RV-M1-Library
//
// (C) 2021 Daniel Springwald, Bochum Germany
// Springwald Software  -   www.springwald.de
// daniel@springwald.de -  +49 234 298 788 46
// All rights reserved
// Licensed under MIT License

using System;
using System.Threading.Tasks;

namespace Movemaster_RV_M1_Library
{
    public class HorizontalExtender
    {
        private MovemasterRobotArm robot;
        private double rotationCorrectionP;
        private double rotationCorrectionR;
        private double extenderLength;
        private double middleZ;

        public HorizontalExtender(MovemasterRobotArm robot, double rotationCorrectionP, double rotationCorrectionR, double extenderLength)
        {
            this.robot = robot;
            this.rotationCorrectionP = rotationCorrectionP;
            this.rotationCorrectionR = rotationCorrectionR; // turns the extender staight forward
            this.extenderLength = extenderLength;
            var minZ = 240d;
            var maxZ = 360d;
            this.middleZ = minZ + (maxZ - minZ) / 2;
        }

        public async Task<bool> MoveTo(double x, double z, double y, double r, int interpolatePoints = 0)
        {
            z -= this.middleZ;

            if (Math.Abs(z) > 40)
            {
                if (z < 0)
                {
                    r = -180;
                    z += this.extenderLength;
                }
                else
                {
                    r = 0;
                    z -= this.extenderLength;
                }
            }
            else
            {
                if (x < 0)
                {
                    r = 90;
                    x += this.extenderLength;
                }
                else
                {
                    r = -90;
                    x -= this.extenderLength;
                }
            }
            z += this.middleZ;
            return await robot.MoveTo(x, z, y, this.rotationCorrectionP, r + this.rotationCorrectionR, interpolatePoints);
        }

    }
}
