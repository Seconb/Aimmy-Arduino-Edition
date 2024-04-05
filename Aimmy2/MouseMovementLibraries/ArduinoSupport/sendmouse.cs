using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net.Sockets;
using System.Text;


namespace MouseMovementLibraries.ArduinoSupport
{
    public static class StartArduino
    {
        public static void StartArduinoMouse()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = Path.Combine(currentDirectory, "mousemovement.exe");

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            Process process = Process.Start(start);
        }
    }

    public class SocketArduinoMouse
    {
        public SocketArduinoMouse() { }

        public void SendMouseCoordinates(int x, int y)
        {
            string ipAddress = "127.0.0.1";
            int port = 9999;

            using (var client = new TcpClient())
            {
                client.Connect(ipAddress, port);

                if (x != 0 || y != 0)
                {
                    string message = $"{x},{y}";
                    byte[] buffer = Encoding.ASCII.GetBytes(message);
                    client.GetStream().Write(buffer, 0, buffer.Length);
                }
            }
        }

        public void SendMouseClick(int click)
        {
            string ipAddress = "127.0.0.1";
            int port = 9999;

            using (var client = new TcpClient())
            {
                client.Connect(ipAddress, port);

                if (click.Equals(0) || click.Equals(1))
                {
                    string message = $"{click}";
                    byte[] buffer = Encoding.ASCII.GetBytes(message);
                    client.GetStream().Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}