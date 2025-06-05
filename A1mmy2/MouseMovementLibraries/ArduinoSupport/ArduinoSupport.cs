using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;

namespace MouseMovementLibraries.ArduinoSupport
{
    internal class ArduinoInput
    {
        private SerialPort serialPort;

        public ArduinoInput(string comPortNumber)
        {
            string portNumber = Regex.Match(comPortNumber ?? "", @"\d+").Value;

            if (string.IsNullOrEmpty(portNumber))
                throw new ArgumentException("Invalid COM port input.");

            string comPort = $"COM{portNumber}";
            serialPort = new SerialPort(comPort, 115200);

            try
            {
                serialPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open {comPort}: {ex.Message}", "COM Port Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public void SendMouseCommand(int x, int y, int click)
        {
            if (!serialPort.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            string command = $"{x},{y},{click}\n";
            serialPort.Write(command);
        }

        public void Close()
        {
            if (serialPort.IsOpen)
                serialPort.Close();
        }
    }
}