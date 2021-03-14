using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Movemaster_RV_M1_Library
{
    public class MovemasterRobotArm : IDisposable
    {
        private const char LF = (char)10;

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

        public Position ActualPosition { get; private set; } = new Position { };
        public RModes RMode { get; set; } = RModes.Absolute;

        public bool WriteToConsole { get; set; } = false;

        public bool GripperClosed
        {
            get
            {
                return this.toolIsClosed ?? false;
            }
            set
            {
                if (this.toolIsClosed.HasValue == false || this.toolIsClosed != value)
                {
                    if (value)
                    {
                        this.SendCommandNoAnswer("GC");
                    }
                    else
                    {
                        this.SendCommandNoAnswer("GO");
                    }
                    this.toolIsClosed = value;
                }
            }
        }

        public MovemasterRobotArm(string comportName)
        {
            if (!this.OpenComPort(comportName, out string errorMsg)) throw new Exception($"can not open robot com port '{comportName}': {errorMsg}");
            if (!this.UpdateActualPositionByHardware()) throw new Exception("can not read initial position from hardware");
        }

        public bool SetToolLength(int lengthInMillimeter)=> this.SendCommandNoAnswer($"TL {lengthInMillimeter}");

        public bool SetGripPressure(int startingGrippenForce, int retainedGrippingForce, int startGrippingForceRetentionTime)
        {
            if (startingGrippenForce < 0 || startingGrippenForce > 15) return false;
            if (retainedGrippingForce < 0 || retainedGrippingForce > 15) return false;
            if (startGrippingForceRetentionTime < 0 || startGrippingForceRetentionTime > 99) return false;
            return SendCommandNoAnswer($"GP {startingGrippenForce}, {retainedGrippingForce}, {startGrippingForceRetentionTime}");
        }

        public bool SetSpeed(int speed)
        {
            if (speed > 9) return false;
            if (speed < 0) return false;
            return this.SendCommandNoAnswer($"SP {speed}");
        }

        public bool MoveToHomePosition() => this.SendCommandNoAnswer("OG");

        public bool Reset() => this.SendCommandNoAnswer("RS");

        public void ShutDown()
        {
            if (comport.IsOpen) comport.Close();
        }

        public bool MoveTo(double x, double z, double y, int interpolatePoints = 0) => MoveTo(x, z, y, this.ActualPosition.P, this.ActualPosition.R, interpolatePoints);
        public bool MoveTo(double x, double z, double y, double p, double r, int interpolatePoints = 0)
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
                success = SendCommandNoAnswer($"MP {PS(x)}, {PS(z)}, {PS(y)}, {PS(p)}, {PS(rTarget)}");
            }
            else
            {
                if (SendCommandNoAnswer($"PC 1"))
                {
                    if (SendCommandNoAnswer($"PD 1, {PS(x)}, {PS(z)}, {PS(y)}, {PS(p)}, {PS(rTarget)}"))
                    {
                        if (SendCommandNoAnswer($"MS 1, {interpolatePoints}, {(this.toolIsClosed ?? false ? "C" : "O")}"))
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
            this.ShutDown();
        }

        public bool SendCommandWithAnswer(string command, out string response)
        {
            this.comport.WriteLine(command);
            Thread.Sleep(100);

            response = null;
            while (response == null)
            {
                Thread.Sleep(1);
                response = this.ReadResponse();
            }
            response = response.Trim(new char[] { '\r', '\n', ' ' });
            var success = this.CheckRobotErrorCode();
            if (success == false && this.WriteToConsole) Console.WriteLine($"##ERROR for '{command}'");
            return success;
        }

        public bool UpdateActualPositionByHardware()
        {
            if (this.RMode != RModes.Absolute) throw new NotImplementedException("Update position from hardware can only be used with absolute mode!");
            if (this.SendCommandWithAnswer("WH", out string response))
            {
                var valueStrings = response.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (valueStrings.Length == 5) return true;
            }
            return false;
        }

        public bool MoveDelta(double x, double z, double y, int interpolatePoints = 0) => MoveDelta(x, z, y, 0, 0, interpolatePoints);
        public bool MoveDelta(double x, double z, double y, double p, double r, int interpolatePoints = 0) => this.MoveTo(this.ActualPosition.X + x, this.ActualPosition.Z + z, this.ActualPosition.Y + y, this.ActualPosition.P + p, this.ActualPosition.R + r, interpolatePoints);

        /// <summary>
        /// Position value to string without localization problems like "0,5" für "0.5"
        /// </summary>
        private string PS(double position) => $"{position:0.0}".Replace(",", ".");

        private bool SendCommandNoAnswer(string command)
        {
            this.comport.WriteLine(command);
            Thread.Sleep(100);
            var success = this.CheckRobotErrorCode();
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

        private bool CheckRobotErrorCode()
        {
            this.comport.WriteLine("ER");
            string result = null;
            while (result == null)
            {
                Thread.Sleep(1);
                result = this.ReadResponse();
            }
            result = result.Trim(new char[] { '\r', '\n', ' ' });
            switch (result)
            {
                case "0": return true;
                default:
                    Thread.Sleep(10); // beep duration
                    this.comport.WriteLine("RS");
                    Thread.Sleep(100);
                    return false;
            }
        }
    }
}
