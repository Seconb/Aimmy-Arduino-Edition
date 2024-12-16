using Aimmy2.Class;
using Class;
using System.Diagnostics;
using MouseMovementLibraries.ArduinoSupport;
using System.Drawing;
using System.Runtime.InteropServices;

namespace InputLogic
{
    internal class MouseManager
    {
        private static readonly double ScreenWidth = WinAPICaller.ScreenWidth;
        private static readonly double ScreenHeight = WinAPICaller.ScreenHeight;

        private static DateTime LastClickTime = DateTime.MinValue;
        private static int LastAntiRecoilClickTime = 0;

        private static double previousX = 0;
        private static double previousY = 0;
        public static double smoothingFactor = 0.5;
        public static bool IsEMASmoothingEnabled = false;
        public static double CurveStrength = 0.75;

        private static Random MouseRandom = new();

        public static ArduinoInput arduinoMouse = new();

        private static Point CubicBezier(Point start, Point end, Point control1, Point control2, double t)
        {
            double u = 1 - t;
            double tt = t * t;
            double uu = u * u;

            double x = uu * u * start.X + 3 * uu * t * control1.X + 3 * u * tt * control2.X + tt * t * end.X;
            double y = uu * u * start.Y + 3 * uu * t * control1.Y + 3 * u * tt * control2.Y + tt * t * end.Y;

            if (IsEMASmoothingEnabled)
            {
                x = EmaSmoothing(previousX, x, smoothingFactor);
                y = EmaSmoothing(previousY, y, smoothingFactor);
            }

            return new Point((int)x, (int)y);
        }

        private static double EmaSmoothing(double previousValue, double currentValue, double smoothingFactor) => (currentValue * smoothingFactor) + (previousValue * (1 - smoothingFactor));

        public static async Task DoTriggerClick()
        {
            int timeSinceLastClick = (int)(DateTime.UtcNow - LastClickTime).TotalMilliseconds;
            int triggerDelayMilliseconds = (int)(Dictionary.sliderSettings["Auto Trigger Delay"] * 1000);

            if (timeSinceLastClick < triggerDelayMilliseconds && LastClickTime != DateTime.MinValue)
            {
                return;
            }

            arduinoMouse.SendMouseCommand(0, 0, 1);

            LastClickTime = DateTime.UtcNow;
        }

        public static void DoAntiRecoil()
        {
            int timeSinceLastClick = Math.Abs(DateTime.UtcNow.Millisecond - LastAntiRecoilClickTime);

            if (timeSinceLastClick < Dictionary.AntiRecoilSettings["Fire Rate"])
            {
                return;
            }

            int xRecoil = (int)Dictionary.AntiRecoilSettings["X Recoil (Left/Right)"];
            int yRecoil = (int)Dictionary.AntiRecoilSettings["Y Recoil (Up/Down)"];

            arduinoMouse.SendMouseCommand(xRecoil, yRecoil, 0);

            LastAntiRecoilClickTime = DateTime.UtcNow.Millisecond;
        }

        public static void MoveCrosshair(int detectedX, int detectedY)
        {
            int halfScreenWidth = (int)ScreenWidth / 2;
            int halfScreenHeight = (int)ScreenHeight / 2;

            int targetX = detectedX - halfScreenWidth;
            int targetY = detectedY - halfScreenHeight;

            double aspectRatioCorrection = ScreenWidth / ScreenHeight;

            int MouseJitter = (int)Dictionary.sliderSettings["Mouse Jitter"];
            int jitterX = MouseRandom.Next(-MouseJitter, MouseJitter);
            int jitterY = MouseRandom.Next(-MouseJitter, MouseJitter);

            Point start = new(0, 0);
            Point end = new(targetX, targetY);
            Point midPoint = new((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            System.Windows.Vector direction = new System.Windows.Vector(end.X - start.X, end.Y - start.Y);
            direction.Normalize();

            System.Windows.Vector perpendicular = new System.Windows.Vector(-direction.Y, direction.X) * (CurveStrength * direction.Length);

            Point control1 = new Point((int)(start.X + (end.X - start.X) / 3 + perpendicular.X), (int)(start.Y + (end.Y - start.Y) / 3 + perpendicular.Y));
            Point control2 = new Point((int)(start.X + 2 * (end.X - start.X) / 3 + perpendicular.X), (int)(start.Y + 2 * (end.Y - start.Y) / 3 + perpendicular.Y));

            Point newPosition = CubicBezier(start, end, control1, control2, 1 - Dictionary.sliderSettings["Mouse Sensitivity (+/-)"]);

            targetX = Math.Clamp(targetX, -150, 150);
            targetY = Math.Clamp(targetY, -150, 150);

            targetY = (int)(targetY * aspectRatioCorrection);

            targetX += jitterX;
            targetY += jitterY;

            arduinoMouse.SendMouseCommand(newPosition.X, newPosition.Y, 0);

            if (Dictionary.toggleState["Auto Trigger"])
            {
                Task.Run(DoTriggerClick);
            }
        }
    }
}
