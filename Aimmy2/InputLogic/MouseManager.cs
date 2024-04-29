using Aimmy2.Class;
using Class;

using MouseMovementLibraries.ArduinoSupport;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace InputLogic
{
    internal class MouseManager
    {
        private static readonly double ScreenWidth = WinAPICaller.ScreenWidth;
        private static readonly double ScreenHeight = WinAPICaller.ScreenHeight;

        //private int TimeSinceLastClick = 0;
        private static DateTime LastClickTime = DateTime.MinValue;

        private static int LastAntiRecoilClickTime = 0;

        public static SocketArduinoMouse arduinoController = new();

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private static Random MouseRandom = new();

        private static Point CubicBezier(Point start, Point end, Point control1, Point control2, double t)
        {
            double u = 1 - t;
            double tt = t * t;
            double uu = u * u;

            double x = uu * u * start.X + 3 * uu * t * control1.X + 3 * u * tt * control2.X + tt * t * end.X;
            double y = uu * u * start.Y + 3 * uu * t * control1.Y + 3 * u * tt * control2.Y + tt * t * end.Y;

            return new Point((int)x, (int)y);
        }

        public static async Task DoTriggerClick()
        {
            int timeSinceLastClick = (int)(DateTime.UtcNow - LastClickTime).TotalMilliseconds;
            int triggerDelayMilliseconds = (int)(Dictionary.sliderSettings["Auto Trigger Delay"] * 1000);
            const int clickDelayMilliseconds = 20;

            if (timeSinceLastClick < triggerDelayMilliseconds && LastClickTime != DateTime.MinValue)
            {
                return;
            }

            string mouseMovementMethod = Dictionary.dropdownState["Mouse Movement Method"];
            Action mouseDownAction;
            Action mouseUpAction;

            switch (mouseMovementMethod)
            {
                case "Arduino":
                    mouseDownAction = () => arduinoController.SendMouseClick(1);
                    mouseUpAction = () => arduinoController.SendMouseClick(0);
                    break;

                default:
                    mouseDownAction = () => arduinoController.SendMouseClick(1);
                    mouseUpAction = () => arduinoController.SendMouseClick(0);
                    break;
            }

            mouseDownAction.Invoke();
            await Task.Delay(clickDelayMilliseconds);
            mouseUpAction.Invoke();

            LastClickTime = DateTime.UtcNow;
        }

        public static void DoAntiRecoil()
        {
            int timeSinceLastClick = Math.Abs(DateTime.UtcNow.Millisecond - LastAntiRecoilClickTime);

            //Debug.WriteLine(timeSinceLastClick);
            if (timeSinceLastClick < Dictionary.AntiRecoilSettings["Fire Rate"])
            {
                return;
            }

            int xRecoil = (int)Dictionary.AntiRecoilSettings["X Recoil (Left/Right)"];
            int yRecoil = (int)Dictionary.AntiRecoilSettings["Y Recoil (Up/Down)"];

            switch (Dictionary.dropdownState["Mouse Movement Method"])
            {

                case "Arduino":
                    arduinoController.SendMouseCoordinates(xRecoil, yRecoil);
                    break;

                default:
                    arduinoController.SendMouseCoordinates(xRecoil, yRecoil);
                    break;
            }

            LastAntiRecoilClickTime = DateTime.UtcNow.Millisecond;
        }

        public static void MoveCrosshair(int detectedX, int detectedY)
        {
            #region Variables

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
            Point control1 = new(start.X + (end.X - start.X) / 3, start.Y + (end.Y - start.Y) / 3);
            Point control2 = new(start.X + 2 * (end.X - start.X) / 3, start.Y + 2 * (end.Y - start.Y) / 3);
            Point newPosition = CubicBezier(start, end, control1, control2, 1 - Dictionary.sliderSettings["Mouse Sensitivity (+/-)"]);

            #endregion Variables

            #region Variable Changes

            targetX = Math.Clamp(targetX, -150, 150);
            targetY = Math.Clamp(targetY, -150, 150);

            targetY = (int)(targetY * aspectRatioCorrection);

            targetX += jitterX;
            targetY += jitterY;

            #endregion Variable Changes

            switch (Dictionary.dropdownState["Mouse Movement Method"])
            {

                case "Arduino":
                    arduinoController.SendMouseCoordinates(newPosition.X, newPosition.Y);
                    break;

                default:
                    arduinoController.SendMouseCoordinates(newPosition.X, newPosition.Y);
                    break;
            }

            if (Dictionary.toggleState["Auto Trigger"])
                Task.Run(DoTriggerClick);
        }
    }
}
