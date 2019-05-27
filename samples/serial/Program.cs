using System;
using System.Threading;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Text;

namespace serial
{
    class Program
    {
        static void DoEcho(SerialPort port, string portname)
        {
            port.Write("Hello, this is a serial echo thread running on " + portname + ".\r\n");
            port.Write("Everything you type will be echoed back.\r\n");
            port.Write("Type X to terminate.\r\n");

            // just discard data already in buffer (need that for EVB where RS485 echoes back sent data)
            Thread.Sleep(1000);
            port.DiscardInBuffer();

            try
            {
                byte[] b = new byte[1];

                for (; ; )
                {
                    // we use read and not ReadByte to let the system wait for next char
                    // without having to use a busy loop
                    int ret = port.Read(b, 0, 1);

                    Console.WriteLine(portname + "-" + b[0].ToString());

                    if (b[0] == (byte)'X')
                    {
                        port.Write("Echo thread terminated on port " + portname + ".\r\n");
                        Console.WriteLine("Echo thread terminated on port " + portname + ".\r\n");
                        return;
                    }

                    port.Write(b, 0, 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message + " opening port " + portname);
            }
        }

        static void Main(string[] args)
        {
            string[] ports = SerialPort.GetPortNames();
            List<Task> tasks = new List<Task>();

            foreach (string portname in ports)
            {
                try
                {
                    SerialPort port = new SerialPort(portname);

                    port.BaudRate = 115200;
                    port.Parity = System.IO.Ports.Parity.None;

                    port.Open();

                    tasks.Add(Task.Factory.StartNew(() => DoEcho(port, portname)));

                    Console.WriteLine("Echo thread started on port " + portname);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error " + ex.Message + " opening port " + portname);
                }
            }

            Task.WaitAny(tasks.ToArray());
            Console.WriteLine("Execution terminated by user.");
        }
    }
}
