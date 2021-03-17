// Movemaster RV M1 Library
// https://github.com/Springwald/Movemaster-RV-M1-Library
//
// (C) 2021 Daniel Springwald, Bochum Germany
// Springwald Software  -   www.springwald.de
// daniel@springwald.de -  +49 234 298 788 46
// All rights reserved
// Licensed under MIT License

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace Movemaster_RV_M1_Library
{
    /// <summary>
    /// Controls the robot arm via the serial port
    /// </summary>
    public class MovemasterRobotArm : IDisposable
    {
        /// <summary>
        /// The response of sending a command with result 
        /// </summary>
        public class SendCommandAnswer
        {
            public string ResponseString { get; set; }
            public bool Success { get; set; }
        }

        /// <summary>
        /// Com port line feed character
        /// </summary>
        private const char LF = (char)10;

        /// <summary>
        /// Is the angle of the R-axis (hand/tool rotation) to be specified relative or absolute?
        /// </summary>
        public enum RModes
        {
            Absolute,
            Relative
        }

        private readonly SerialPort comport = new SerialPort();
        private readonly StringBuilder responseStringBuffer = new StringBuilder();
        private readonly Queue<string> responses = new Queue<string>();
        private bool isDisposed;
        private bool? toolIsClosed;

        /// <summary>
        /// The current position of the robot tool
        /// </summary>
        public Position ActualPosition { get; private set; } = new Position { };

        /// <summary>
        /// Is the angle of the R-axis (hand/tool rotation) to be specified relative or absolute?
        /// </summary>
        public RModes RMode { get; set; } = RModes.Absolute;

        /// <summary>
        /// Write debug information to console?
        /// </summary>
        public bool WriteToConsole { get; set; } = false;

        /// <summary>
        /// Gets if the tool gripper is closed
        /// </summary>
        public async Task SetGripperClosed(bool value)
        {
            if (this.toolIsClosed.HasValue == false || this.toolIsClosed != value)
            {
                if (value)
                {
                    await this.SendCommandNoAnswer("GC");
                }
                else
                {
                    await this.SendCommandNoAnswer("GO");
                }
                this.toolIsClosed = value;
            }
        }

        /// <summary>
        /// Gets if the tool gripper is closed
        /// </summary>
        public bool GetGripperClosed() => this.toolIsClosed ?? false;

        /// <summary>
        /// Initializes the robot arm via the specified COM port
        /// </summary>
        public static async Task<MovemasterRobotArm> CreateAsync(string comportName)
        {
            var instance = new MovemasterRobotArm();
            if (!instance.OpenComPort(comportName, out string errorMsg)) throw new Exception($"can not open robot com port '{comportName}': {errorMsg}");
            if (!await instance.UpdateActualPositionByHardware()) throw new Exception("can not read initial position from hardware");
            return instance;
        }

        /// <summary>
        /// Defines the tool length of the robot arm
        /// </summary>
        public async Task<bool> SetToolLength(int lengthInMillimeter) => await this.SendCommandNoAnswer($"TL {lengthInMillimeter}");

        /// <summary>
        /// Defines the pressure of the robot arm gripper
        /// </summary>
        public async Task<bool> SetGripPressure(int startingGrippenForce, int retainedGrippingForce, int startGrippingForceRetentionTime)
        {
            if (startingGrippenForce < 0 || startingGrippenForce > 15) return false;
            if (retainedGrippingForce < 0 || retainedGrippingForce > 15) return false;
            if (startGrippingForceRetentionTime < 0 || startGrippingForceRetentionTime > 99) return false;
            return await SendCommandNoAnswer($"GP {startingGrippenForce}, {retainedGrippingForce}, {startGrippingForceRetentionTime}");
        }

        /// <summary>
        /// Sets the move speed of the robot arm
        /// </summary>
        /// <param name="speed">0=slowest, 9=fastest</param>
        public async Task<bool> SetSpeed(int speed)
        {
            if (speed > 9) return false;
            if (speed < 0) return false;
            return await this.SendCommandNoAnswer($"SP {speed}");
        }

        /// <summary>
        /// Moves all axes to zero position
        /// </summary>
        public async Task<bool> MoveToHomePosition() => await this.SendCommandNoAnswer("OG");

        /// <summary>
        /// Resets the control box
        /// </summary>
        public async Task<bool> Reset() => await this.SendCommandNoAnswer("RS");

        /// <summary>
        /// Moves the robot arm to the given absolute position/axis values using *interpolatePoints* linear calculated path points
        /// </summary>
        public async Task<bool> MoveTo(double x, double z, double y, int interpolatePoints = 0) => await MoveTo(x, z, y, this.ActualPosition.P, this.ActualPosition.R, interpolatePoints);

        /// <summary>
        /// Moves the robot arm to the given absolute position/axis values using the shorted path, not a linear path.
        /// </summary>
        public async Task<bool> MoveTo(double x, double z, double y, double p, double r, int interpolatePoints = 0)
        {
            if (this.WriteToConsole) Console.WriteLine($"{x:0.0} | {z:0.0} | {y:0.0} | {p:0.0} | {r:0.0}");

            var rTarget = r;
            switch (this.RMode)
            {
                case RModes.Absolute:
                    rTarget += Math.Atan2(x, z) * 180 / Math.PI;
                    break;
                case RModes.Relative:
                    break;
                default: throw new ArgumentOutOfRangeException($"{nameof(this.RMode)}:{this.RMode.ToString()}");
            }

            var success = false;
            if (interpolatePoints == 0)
            {
                success = await SendCommandNoAnswer($"MP {PS(x)}, {PS(z)}, {PS(y)}, {PS(p)}, {PS(rTarget)}");
            }
            else
            {
                if (await SendCommandNoAnswer($"PC 1")) // clear numbered position 1
                {
                    if (await SendCommandNoAnswer($"PD 1, {PS(x)}, {PS(z)}, {PS(y)}, {PS(p)}, {PS(rTarget)}")) // need to set a temporary numbered position 1
                    {
                        if (await SendCommandNoAnswer($"MS 1, {interpolatePoints}, {(this.toolIsClosed ?? false ? "C" : "O")}")) // move to temporary position 1
                            success = true;
                    }
                }
            }

            if (success)
            {
                this.ActualPosition.X = x;
                this.ActualPosition.Y = y;
                this.ActualPosition.Z = z;
                this.ActualPosition.P = p;
                this.ActualPosition.R = r;
            }

            return success;
        }

        public void Dispose()
        {
            if (this.isDisposed) return;
            this.isDisposed = true;
            this.comport.DataReceived -= new SerialDataReceivedEventHandler(this.Comport_DataReceived);
            if (comport.IsOpen) this.comport.Close();
            this.comport.Dispose();
        }

        /// <summary>
        /// Sends a command and waits for the robot response
        /// </summary>
        public async Task<SendCommandAnswer> SendCommandWithAnswer(string command)
        {
            this.comport.WriteLine(command);
            await Task.Delay(100);

            string responseString = null;
            while (responseString == null)
            {
                await Task.Delay(1);
                responseString = this.ReadResponse().Trim(new char[] { '\r', '\n', ' ' });
            }
            var success = await this.CheckRobotErrorCode();
            if (success == false && this.WriteToConsole) Console.WriteLine($"##ERROR for '{command}'");
            return new SendCommandAnswer
            {
                ResponseString = responseString,
                Success = success
            };
        }

        /// <summary>
        /// Reads the actual position of the robot hardware and writes it into the property *ActualPosition*
        /// </summary>
        public async Task<bool> UpdateActualPositionByHardware()
        {
            if (this.RMode != RModes.Absolute) throw new NotImplementedException("Update position from hardware can only be used with absolute mode!");
            var response = await this.SendCommandWithAnswer("WH");
            if (response.Success)
            {
                var valueStrings = response.ResponseString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (valueStrings.Length == 5) return true;
                // TO DO
            }
            return false;
        }

        /// <summary>
        /// Moves the robot arm to the given relative x-y-z-axis values
        /// </summary>
        /// <param name="interpolatePoints">when != 0: use linear calculated path points</param>
        public async Task<bool> MoveDelta(double x, double z, double y, int interpolatePoints = 0) => await MoveDelta(x, z, y, 0, 0, interpolatePoints);

        /// <summary>
        /// Moves the robot arm to the given relative position/axis values using the shorted path, not a linear path.
        /// </summary>
        /// <param name="interpolatePoints">when != 0: use linear calculated path points</param>
        public async Task<bool> MoveDelta(double x, double z, double y, double p, double r, int interpolatePoints = 0) => await this.MoveTo(this.ActualPosition.X + x, this.ActualPosition.Z + z, this.ActualPosition.Y + y, this.ActualPosition.P + p, this.ActualPosition.R + r, interpolatePoints);

        /// <summary>
        /// Position value to string without localization problems like "0,5" für "0.5"
        /// </summary>
        private string PS(double position) => $"{position:0.0}".Replace(",", ".");

        private async Task<bool> SendCommandNoAnswer(string command)
        {
            this.comport.WriteLine(command);
            await Task.Delay(100);
            var success = await this.CheckRobotErrorCode();
            if (success == false && this.WriteToConsole) Console.WriteLine($"##ERROR for '{command}'");
            return success;
        }

        private string ReadResponse()
        {
            if (responses.Count == 0) return null;
            return responses.Dequeue();
        }

        private void Comport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var data = this.comport.ReadExisting();
            foreach (char c in data)
            {
                if (c == LF)
                {
                    responseStringBuffer.Append(c);
                    this.responses.Enqueue(responseStringBuffer.ToString());
                    responseStringBuffer.Clear();
                }
                else
                {
                    responseStringBuffer.Append(c);
                }
            }
        }

        private bool OpenComPort(string comPortName, out string errorMsg)
        {
            if (comport.IsOpen) comport.Close();

            comport.BaudRate = 9600;
            comport.Handshake = Handshake.RequestToSendXOnXOff;
            comport.DataBits = 7;
            comport.StopBits = StopBits.Two;
            comport.Parity = Parity.Even;
            comport.PortName = comPortName;
            comport.Handshake = Handshake.RequestToSendXOnXOff;
            comport.DtrEnable = true;
            comport.RtsEnable = true;
            comport.DataReceived += new SerialDataReceivedEventHandler(Comport_DataReceived);

            try
            {
                comport.Open();
                errorMsg = null;
                return true;
            }
            catch (Exception e)
            {
                errorMsg = e.Message;
                return false;
            }
        }

        private async Task<bool> CheckRobotErrorCode()
        {
            this.comport.WriteLine("ER");
            string result = null;
            while (result == null)
            {
                await Task.Delay(1);
                result = this.ReadResponse();
            }
            result = result.Trim(new char[] { '\r', '\n', ' ' });
            switch (result)
            {
                case "0": return true;
                default:
                    await Task.Delay(10); // beep durations
                    this.comport.WriteLine("RS");
                    await Task.Delay(100);
                    return false;
            }
        }
    }
}
