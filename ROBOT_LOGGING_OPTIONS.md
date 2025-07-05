# Robot Logging with Environment Variables

This document explains how to control logging in the MovemasterRobotArm using environment variables.

## Implementation

The `MovemasterRobotArm` class has a `WriteToConsole` property that controls debug output to the console. This is set via environment variable when creating the robot instance.

## Environment Variable

**Environment Variable:** `ROBOT_ENABLE_LOGGING`

**Usage:**

```bash
# Enable robot logging
export ROBOT_ENABLE_LOGGING=true

# Disable robot logging
export ROBOT_ENABLE_LOGGING=false
```

**Implementation:** The RobotController reads this environment variable in the `Connect` method and passes it to `MovemasterRobotArm.CreateAsync()`.

## How It Works

1. **Environment Variable Check:** The `Connect` method reads `ROBOT_ENABLE_LOGGING`
2. **Robot Creation:** The value is passed to `MovemasterRobotArm.CreateAsync(comPort, enableLogging)`
3. **Logging Control:** The robot's `WriteToConsole` property is set based on the environment variable

## Usage Examples

### Development Environment

```bash
# Enable logging for development
export ROBOT_ENABLE_LOGGING=true
dotnet run
```

### Production Environment

```bash
# Disable logging for production
export ROBOT_ENABLE_LOGGING=false
dotnet run
```

### Docker Environment

```dockerfile
# In Dockerfile
ENV ROBOT_ENABLE_LOGGING=false
```

### Kubernetes Environment

```yaml
# In deployment.yaml
env:
  - name: ROBOT_ENABLE_LOGGING
    value: "false"
```

### Windows Environment

```cmd
# Enable logging
set ROBOT_ENABLE_LOGGING=true
dotnet run

# Disable logging
set ROBOT_ENABLE_LOGGING=false
dotnet run
```

### PowerShell Environment

```powershell
# Enable logging
$env:ROBOT_ENABLE_LOGGING="true"
dotnet run

# Disable logging
$env:ROBOT_ENABLE_LOGGING="false"
dotnet run
```

## Testing

You can test the logging by setting the environment variable and running the application:

```bash
# Test with logging enabled
ROBOT_ENABLE_LOGGING=true dotnet run

# Test with logging disabled
ROBOT_ENABLE_LOGGING=false dotnet run
```

## What Gets Logged

When `ROBOT_ENABLE_LOGGING=true`, the following information is logged to the console:

- Robot movement commands and coordinates
- Serial communication commands
- Error responses from the robot
- Position updates

Example output:

```
##SENDING NO ANSWER: 'MP 100.0, 200.0, 50.0, 0.0, 0.0'
100.0 | 200.0 | 50.0 | 0.0 | 0.0
##SENDING WITH ANSWER: 'WH'
```

## Benefits

- **Simple:** Just set an environment variable
- **Flexible:** Easy to change per environment
- **Clean:** No API endpoints or configuration files needed
- **Standard:** Follows common environment variable patterns
