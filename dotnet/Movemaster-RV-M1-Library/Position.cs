﻿// Movemaster RV M1 Library
// https://github.com/Springwald/Movemaster-RV-M1-Library
//
// (C) 2022 Daniel Springwald, Bochum Germany
// Springwald Software  -   www.springwald.de
// daniel@springwald.de -  +49 234 298 788 46
// All rights reserved
// Licensed under MIT License

namespace Movemaster_RV_M1_Library
{
    public class Position
    {
        /// <summary>
        /// X-Axis (left-right)
        /// </summary>
        public double X { get; internal set; }

        /// <summary>
        /// Y-Axis (up-down)
        /// </summary>
        public double Y { get; internal set; }

        /// <summary>
        /// Z-Axis (forwards-backwards)
        /// </summary>
        public double Z { get; internal set; }

        /// <summary>
        /// Elbow angle
        /// </summary>
        public double P { get; internal set; }
        
        /// <summary>
        /// Hand / Tool Rotation
        /// </summary>
        public double R { get; internal set; }

        /// <summary>
        /// All axis values as string
        /// </summary>  
        public string PositionAsString=> $"X:{X.ToString("0.00")} Y:{Y.ToString("0.00")} Z:{Z.ToString("0.00")} P:{P.ToString("0.00")} R:{R.ToString("0.00")}";
        

        public Position()
        {
        }

        public Position(double x, double y, double z, double p, double r)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.P = p;
            this.R = r;
        }

        public Position Clone()
        {
            // deep clone the object
            return new Position()
            {
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                P = this.P,
                R = this.R
            };
        }
    }
}
