# GPIO sample

This is a simple application accessing the GPIOs using [System.Device.Gpio](https://github.com/dotnet/iot).
Changes to the default sample application (dotnetcode):

- added references to System.Device.Gpio and IoT.Device.Bindings to gpio.csproj, currently those references use a fixed preview release of the libraries, this could be moved to a stable release as soon as one it's going to be available for .NET Core 3.0.

- added -v /sys/class/gpio:/sys/class/gpio mount point command to docker run command line to expose the sysfs folders related to GPIO control inside the running container

- added --privileged to the commands used to start debug/release container, this is required to let the container access gpio-controlling filesystem entries from /sys/class/gpio.
