using System;
using System.Threading;
using System.Device.Gpio;

namespace gpio
{
    class Program
    {
        static void Main(string[] args)
        {
            int pin = 34; // SODIMM 135 on Colibri iMX6

            GpioController controller = new GpioController();

            controller.OpenPin(pin, PinMode.Output);

            for (; ; )
            {
                controller.Write(pin, PinValue.High);
                Thread.Sleep(500);
                controller.Write(pin, PinValue.Low);
                Thread.Sleep(500);
            }
        }
    }
}
