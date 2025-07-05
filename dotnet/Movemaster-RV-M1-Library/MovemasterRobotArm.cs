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
using System.Net.Security;
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
            public required string ResponseString { get; set; }
            public required bool Success { get; set; }
        }

        /// <summary>
        /// Com port line feed character
        /// </summary>
        private const char LF = (char)10;

        private readonly SerialPort comport = new SerialPort();
        private readonly StringBuilder responseStringBuffer = new StringBuilder();
        private readonly Queue<string> responses = new Queue<string>();

        public delegate void SerialDataReceivedHandler(string data);
        public event SerialDataReceivedHandler OnSerialDataReceived;


        private bool isDisposed;
        private bool? toolIsClosed;
        private Position? _actualPosition = null;

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
        /// use MovemasterRobotArm.CreateAsync(string comportName) instead
        /// </summary>
        private MovemasterRobotArm()
        {
        }


        /// <summary>
        /// Initializes the robot arm via the specified COM port
        /// </summary>
        public static async Task<MovemasterRobotArm> CreateAsync(string comportName)
        {
            var instance = new MovemasterRobotArm();
            if (!instance.OpenComPort(comportName, out string? errorMsg)) throw new Exception($"can not open robot com port '{comportName}': {errorMsg}");
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
        /// Gets the actual position of the robot arm tool
        /// </summary>
        /// <param name="forceUpdateByHardware">
        /// if true: the actual position is read from the hardware (slow)
        /// if false: the last known position successfull send to the robot is returned (fast). If you move the robot arm by hand, the position is not updated.
        /// </param>
        /// <returns>the actual position</returns>
        public async Task<Position?> GetActualPosition(bool forceUpdateByHardware)
        {
            if (_actualPosition == null) throw new Exception("Actual position not set");
            if (forceUpdateByHardware == true && await this.UpdateActualPositionByHardware() == false)
                return null;

            return _actualPosition.Clone();
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
        public async Task<bool> MoveTo(Position position) => await MoveTo(position.X, position.Y, position.Z, position.P, position.R);

        /// <summary>
        /// Moves the robot arm to the given absolute position/axis values using *interpolatePoints* linear calculated path points
        /// </summary>
        public async Task<bool> MoveTo(double x, double y, double z, int interpolatePoints = 0)
        {
            if (_actualPosition == null) throw new Exception("Actual position not set");
            return await MoveTo(x, y, z, _actualPosition.P, _actualPosition.R, interpolatePoints);
        }

        /// <summary>
        /// Moves the robot arm in a path defined by a list of positions
        /// </summary>
        /// <param name="positions">
        /// List of positions to move to
        /// </param>
        public async Task<bool> MovePath(List<Position> positions){

            if (positions.Count == 0) throw new Exception("Number of positions is 0");
            if (positions.Count > 629) throw new Exception("Number of positions exceed maximum (629)");

            int i = 0;
            foreach (var position in positions){
                await SendCommandNoAnswer($"PD {i}, {PS(position.X)}, {PS(position.Y)}, {PS(position.Z)}, {PS(position.P)}, {PS(position.R)}");
                i++;
                await Task.Delay(100);
                /* if (await MoveTo(position.X, position.Y, position.Z, position.P, position.R, interpolatePoints)){
                } */
            }

            int numberOfPositions = positions.Count;

            await SendCommandNoAnswer($"MC 0, {numberOfPositions}");



            return true;
            /* if ( // need to set a temporary numbered position 1 */
        }

        /// <summary>
        /// Moves the robot arm to the given absolute position/axis values using the shorted path, not a linear path.
        /// </summary>
        public async Task<bool> MoveTo(double x, double y, double z, double p, double r, int interpolatePoints = 0)
        {
            if (this.WriteToConsole) Console.WriteLine($"{x:0.0} | {y:0.0} | {z:0.0} | {p:0.0} | {r:0.0}");

            var success = false;
            if (interpolatePoints == 0)
            {
                success = await SendCommandNoAnswer($"MP {PS(x)}, {PS(y)}, {PS(z)}, {PS(p)}, {PS(r)}");
            }
            else
            {
                if (await SendCommandNoAnswer($"PC 1")) // clear numbered position 1
                {
                    if (await SendCommandNoAnswer($"PD 1, {PS(x)}, {PS(y)}, {PS(z)}, {PS(p)}, {PS(r)}")) // need to set a temporary numbered position 1
                    {
                        if (await SendCommandNoAnswer($"MS 1, {interpolatePoints}, {(this.toolIsClosed ?? false ? "C" : "O")}")) // move to temporary position 1
                            success = true;
                    }
                }
            }

            if (success)
            {
                if (_actualPosition == null)
                    _actualPosition = new Position(x, y, z, p, r);
                else
                {
                    _actualPosition.X = x;
                    _actualPosition.Y = y;
                    _actualPosition.Z = z;
                    _actualPosition.P = p;
                    _actualPosition.R = r;
                }
            }

            return success;
        }

        /// <summary>
        /// Rotates the axes relative to the actual position
        /// </summary>
        public async Task<bool> RotateAxis(double x, double y, double z, double p, double r)
        {
            if (this.WriteToConsole) Console.WriteLine($"Rotate Axes {x:0.0} | {y:0.0} | {z:0.0} | {p:0.0} | {r:0.0}");
            if (await SendCommandNoAnswer($"MJ {PS(x)}, {PS(y)}, {PS(z)}, {PS(p)}, {PS(r)}"))
                if (await this.UpdateActualPositionByHardware())
                    return true;

            return false;
        }

        public double CleanUpRValue(double x, double y, double rTarget)
        {
            rTarget += Math.Atan2(x, y) * 180 / Math.PI;
            //while (rTarget > 360) rTarget -= 360;
            //while (rTarget < 0) rTarget += 360;
            return rTarget;
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

            string? responseString = null;
            while (responseString == null)
            {
                await Task.Delay(1);
                responseString = this.ReadResponse()?.Trim(new char[] { '\r', '\n', ' ' });
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
            var response = await this.SendCommandWithAnswer("WH");
            if (response.Success)
            {
                // Console.WriteLine(response.ResponseString);
                var valueStrings = response.ResponseString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (valueStrings.Length == 5)
                {
                    if (_actualPosition == null) _actualPosition = new Position();
                    _actualPosition.X = ParseResultValueToDouble(valueStrings[0]);
                    _actualPosition.Y = ParseResultValueToDouble(valueStrings[1]);
                    _actualPosition.Z = ParseResultValueToDouble(valueStrings[2]);
                    _actualPosition.P = ParseResultValueToDouble(valueStrings[3]);
                    _actualPosition.R = ParseResultValueToDouble(valueStrings[4]);
                    return true;
                }
                else
                {
                    Console.WriteLine($"##ERROR for 'WH': '{response.ResponseString}', valueStrings.Length=" + valueStrings.Length);
                }
            }
            return false;
        }


        private double ParseResultValueToDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return double.NaN;
            if (value.StartsWith(".")) value = "0" + value;
            value = value.Replace(".", ",");
            if (double.TryParse(value, out double result)) return result;
            Console.WriteLine($"##ERROR parsing value to double: '{value}'");
            return double.NaN;
        }


        /// <summary>
        /// Moves the robot arm to the given relative x-y-z-axis values
        /// </summary>
        /// <param name="interpolatePoints">when != 0: use linear calculated path points</param>
        public async Task<bool> MoveDelta(double x, double y, double z, int interpolatePoints = 0) => await MoveDelta(x, y, z, 0, 0, interpolatePoints);

        /// <summary>
        /// Moves the robot arm to the given relative position/axis values using the shorted path, not a linear path.
        /// </summary>
        /// <param name="interpolatePoints">when != 0: use linear calculated path points</param>
        public async Task<bool> MoveDelta(double x, double y, double z, double p, double r, int interpolatePoints = 0)
        {
            if (_actualPosition == null) throw new Exception("Actual position not set");
            return await this.MoveTo(_actualPosition.X + x, _actualPosition.Y + y, _actualPosition.Z + z, _actualPosition.P + p, _actualPosition.R + r, interpolatePoints);
        }
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

        private string? ReadResponse()
        {
            if (responses.Count == 0) return null;
            return responses.Dequeue();
        }

        private void Comport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var data = this.comport.ReadExisting();

            OnSerialDataReceived?.Invoke(data);


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

        private bool OpenComPort(string comPortName, out string? errorMsg)
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
            string? result = null;
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
