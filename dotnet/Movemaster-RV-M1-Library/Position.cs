// Movemaster RV M1 Library
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
        /// Z-Axis (forwards-backwards)
        /// </summary>
        public double Z { get; internal set; }

        /// <summary>
        /// Y-Axis (up-down)
        /// </summary>
        public double Y { get; internal set; }

        /// <summary>
        /// Elbow angle
        /// </summary>
        public double P { get; internal set; }
        
        /// <summary>
        /// Hand / Tool Rotation
        /// </summary>
        public double R { get; internal set; }
    }
}
