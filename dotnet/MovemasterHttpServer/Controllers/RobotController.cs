using Microsoft.AspNetCore.Mvc;
using Movemaster_RV_M1_Library;
using MovemasterHttpServer.Services;
using System;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace MovemasterHttpServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RobotController : ControllerBase
    {
        private static MovemasterRobotArm _robot;
        private readonly RobotSerialService _serialService;

        public RobotController(RobotSerialService serialService)
        {
            _serialService = serialService;
        }
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        // Initialize robot connection
        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] ConnectionRequest request)
        {
            try
            {
                if (_isInitialized)
                {
                    return Ok(new { success = true, message = "Robot already connected" });
                }

                lock (_lock)
                {
                    if (!_isInitialized)
                    {
                        string comPort = request?.ComPort ?? "COM3";
                        _robot = MovemasterRobotArm.CreateAsync(comPort).Result;

                        if (_robot != null)
                        {
                            _serialService.SubscribeToRobot(_robot);
                        }
                        _isInitialized = true;
                    }
                }

                return Ok(new { success = true, message = $"Connected to robot on {request?.ComPort ?? "COM3"}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Disconnect and dispose robot connection
        [HttpPost("disconnect")]
        public IActionResult Disconnect()
        {
            try
            {
                if (!_isInitialized)
                {
                    return Ok(new { success = true, message = "Robot was not connected" });
                }

                lock (_lock)
                {
                    if (_isInitialized)
                    {
                        _robot.Dispose();
                        _isInitialized = false;
                    }
                }

                return Ok(new { success = true, message = "Robot disconnected" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Get current position
        [HttpGet("position")]
        public async Task<IActionResult> GetPosition([FromQuery] bool forceUpdateByHardware = false)
        {
            try
            {
                CheckInitialized();

                var position = await _robot.GetActualPosition(forceUpdateByHardware);
                if (position != null)
                {
                    return Ok(new
                    {
                        success = true,
                        position = new
                        {
                            x = position.X,
                            y = position.Y,
                            z = position.Z,
                            p = position.P,
                            r = position.R
                        }
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to get position" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Force update of position from hardware
        [HttpPost("update-position")]
        public async Task<IActionResult> UpdatePositionByHardware()
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.UpdateActualPositionByHardware();
                if (success)
                {
                    var position = await _robot.GetActualPosition(false);
                    return Ok(new
                    {
                        success = true,
                        position = new
                        {
                            x = position.X,
                            y = position.Y,
                            z = position.Z,
                            p = position.P,
                            r = position.R
                        }
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to update position from hardware" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Reset the robot
        [HttpPost("reset")]
        public async Task<IActionResult> Reset()
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.Reset();
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Move to home position
        [HttpPost("home")]
        public async Task<IActionResult> MoveToHomePosition()
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.MoveToHomePosition();
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Move to absolute position
        [HttpPost("move")]
        public async Task<IActionResult> MoveTo([FromBody] MoveToRequest request)
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.MoveTo(
                    request.X,
                    request.Y,
                    request.Z,
                    request.Pitch,
                    request.Roll,
                    request.InterpolatePoints);

                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Move relative distance (delta)
        [HttpPost("move-delta")]
        public async Task<IActionResult> MoveDelta([FromBody] MoveDeltaRequest request)
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.MoveDelta(
                    request.DeltaX,
                    request.DeltaY,
                    request.DeltaZ,
                    request.DeltaP,
                    request.DeltaR,
                    request.InterpolatePoints);

                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Rotate axes
        [HttpPost("rotate")]
        public async Task<IActionResult> RotateAxis([FromBody] RotateAxisRequest request)
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.RotateAxis(
                    request.X,
                    request.Y,
                    request.Z,
                    request.P,
                    request.R);

                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Clean up R value
        [HttpPost("clean-r-value")]
        public IActionResult CleanUpRValue([FromBody] CleanRValueRequest request)
        {
            try
            {
                CheckInitialized();

                double cleanedValue = _robot.CleanUpRValue(request.X, request.Y, request.RTarget);
                return Ok(new { success = true, cleanedRValue = cleanedValue });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Set gripper state (open/close)
        [HttpPost("gripper")]
        public async Task<IActionResult> SetGripper([FromBody] GripperRequest request)
        {
            try
            {
                CheckInitialized();

                await _robot.SetGripperClosed(request.Closed);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Get gripper state
        [HttpGet("gripper")]
        public IActionResult GetGripper()
        {
            try
            {
                CheckInitialized();

                bool closed = _robot.GetGripperClosed();
                return Ok(new { success = true, closed });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Set tool length
        [HttpPost("tool-length")]
        public async Task<IActionResult> SetToolLength([FromBody] ToolLengthRequest request)
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.SetToolLength(request.LengthInMillimeter);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Set gripper pressure
        [HttpPost("gripper-pressure")]
        public async Task<IActionResult> SetGripperPressure([FromBody] GripperPressureRequest request)
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.SetGripPressure(
                    request.StartingGrippenForce,
                    request.RetainedGrippingForce,
                    request.StartGrippingForceRetentionTime);

                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Set movement speed
        [HttpPost("speed")]
        public async Task<IActionResult> SetSpeed([FromBody] SpeedRequest request)
        {
            try
            {
                CheckInitialized();

                bool success = await _robot.SetSpeed(request.Speed);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Enable/disable console output
        [HttpPost("console-output")]
        public IActionResult SetConsoleOutput([FromBody] ConsoleOutputRequest request)
        {
            try
            {
                CheckInitialized();

                _robot.WriteToConsole = request.Enabled;
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Send custom command with response
        [HttpPost("command")]
        public async Task<IActionResult> SendCommand([FromBody] CommandRequest request)
        {
            try
            {
                CheckInitialized();

                var response = await _robot.SendCommandWithAnswer(request.Command);
                return Ok(new
                {
                    success = response.Success,
                    response = response.ResponseString
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private void CheckInitialized()
        {
            if (!_isInitialized)
            {
                throw new Exception("Robot is not connected. Call /api/robot/connect first.");
            }
        }
    }

    // Request models
    public class ConnectionRequest
    {
        [JsonPropertyName("comPort")]
        public string ComPort { get; set; }
    }

    public class MoveToRequest
    {
        [JsonPropertyName("x")]
        public double X { get; set; }
        [JsonPropertyName("y")]
        public double Y { get; set; }
        [JsonPropertyName("z")]
        public double Z { get; set; }
        [JsonPropertyName("pitch")]
        public double Pitch { get; set; }
        [JsonPropertyName("roll")]
        public double Roll { get; set; }
        [JsonPropertyName("interpolatePoints")]
        public int InterpolatePoints { get; set; } = 0;
    }

    public class MoveDeltaRequest
    {
        [JsonPropertyName("deltaX")]
        public double DeltaX { get; set; }
        [JsonPropertyName("deltaY")]
        public double DeltaY { get; set; }
        [JsonPropertyName("deltaZ")]
        public double DeltaZ { get; set; }
        [JsonPropertyName("deltaP")]
        public double DeltaP { get; set; } = 0;
        [JsonPropertyName("deltaR")]
        public double DeltaR { get; set; } = 0;
        [JsonPropertyName("interpolatePoints")]
        public int InterpolatePoints { get; set; } = 0;
    }

    public class RotateAxisRequest
    {
        [JsonPropertyName("x")]
        public double X { get; set; }
        [JsonPropertyName("y")]
        public double Y { get; set; }
        [JsonPropertyName("z")]
        public double Z { get; set; }
        [JsonPropertyName("p")]
        public double P { get; set; }
        [JsonPropertyName("r")]
        public double R { get; set; }
    }

    public class CleanRValueRequest
    {
        [JsonPropertyName("x")]
        public double X { get; set; }
        [JsonPropertyName("y")]
        public double Y { get; set; }
        [JsonPropertyName("rTarget")]
        public double RTarget { get; set; }
    }

    public class GripperRequest
    {
        [JsonPropertyName("closed")]
        public bool Closed { get; set; }
    }

    public class ToolLengthRequest
    {
        [JsonPropertyName("lengthInMillimeter")]
        public int LengthInMillimeter { get; set; }
    }

    public class GripperPressureRequest
    {
        [JsonPropertyName("startingGrippenForce")]
        public int StartingGrippenForce { get; set; }
        [JsonPropertyName("retainedGrippingForce")]
        public int RetainedGrippingForce { get; set; }
        [JsonPropertyName("startGrippingForceRetentionTime")]
        public int StartGrippingForceRetentionTime { get; set; }
    }

    public class SpeedRequest
    {
        [JsonPropertyName("speed")]
        public int Speed { get; set; }
    }

    public class ConsoleOutputRequest
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }

    public class CommandRequest
    {
        [JsonPropertyName("command")]
        public string Command { get; set; }
    }
}