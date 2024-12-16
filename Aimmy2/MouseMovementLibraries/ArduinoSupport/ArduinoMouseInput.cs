using System;
using System.IO.Ports;
using System.Linq;

namespace MouseMovementLibraries.ArduinoSupport
{
    internal class ArduinoInput
    {
        private SerialPort serialPort;

        public ArduinoInput()
        {
            string comPort = GetUsbSerialPort();
            if (comPort == null)
            {
                throw new Exception("Arduino not found on any COM port.");
            }

            serialPort = new SerialPort(comPort, 115200);
            serialPort.Open();
        }

        private string GetUsbSerialPort()
        {
            foreach (string port in SerialPort.GetPortNames())
            {
                string description = GetPortDescription(port);
                if (description.Contains("USB Serial Device"))
                {
                    return port;
                }
            }
            return null;
        }

        private string GetPortDescription(string port)
        {
            try
            {
                var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_SerialPort");
                foreach (var device in searcher.Get())
                {
                    if (device["DeviceID"].ToString() == port)
                    {
                        return device["Description"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while accessing port description: " + ex.Message);
            }
            return string.Empty;
        }

        public void SendMouseCommand(int x, int y, int click)
        {
            if (!serialPort.IsOpen)
            {
                throw new InvalidOperationException("Serial port is not open.");
            }

            string command = $"{x},{y},{click}\n";
            serialPort.Write(command);
        }

        public void Close()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
    }
}
