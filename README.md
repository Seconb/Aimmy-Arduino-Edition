# Aimmy Arduino Edition!

Aimmy but with Arduino support!

**WARNING: THIS CHEAT IS DETECTED ON FORTNITE AND YOU WILL GET DELAY BANNED!!**

**DO NOT ASK FOR HELP ON DISCORD. DO NOT DM ME ON DISCORD AT ALL**

## Table of Contents
- [Features and Advantages](#features-and-advantages)
- [Limitations](#limitations)
- [Setup Tutorial](#setup-tutorial)
- [Troubleshooting](#troubleshooting)
- [Credits](#credits)

## Features
- **HID Communication:** Utilizes HID instead of COM port communication, reducing detection risks in most games.
- **Easy Setup:** Straightforward script upload process to your Arduino. Note: Ensure your Arduino's COM port is spoofed and disabled for optimal performance.
- **Undetected Gameplay:** Undetected in every game except Fortnite, Valorant, and CS2. Arduino Leonardo required.
- ** USB Host Shield Support: ** Added support for USB Host Shields, though this support probably won't work on Logitech and Razer mice. Many mice will have scroll wheel and side button issues.
- ** Improvements over normal Aimmy: ** This uses Whoswhip's fork of Aimmy, which features the following:
```diff
+ General Optimization
+ DirectX Screen Capturing (Taylor's Aimmy Cuda)
+ Third Person Support (Fov Settings)
+ Bezier Curve Strength
+ Debug Mode (AI Loop Speed)

- Prediction Removed
- Data Collection Removed
- GDI Screen Capturing Removed
```

## Setup Tutorial
1. [Video Tutorial](https://streamable.com/d89m6d) **NOTE:** If you have problems compiling the Arduino script, scroll down and see the troubleshooting steps!
2. Download and install [Arduino IDE 1.8.19](https://downloads.arduino.cc/arduino-1.8.19-windows.exe)
3. Download [Aimmy Arduino Edition Download](https://github.com/Seconb/Aimmy-Arduino-Edition/releases/tag/v4) and extract it
4. Run `arduinospoofer.exe` **AS ADMIN**
5. Once that finishes, open `MouseInstructArduino.ino`
6. Click the upload button (the arrow pointing to the right in the top left of the Arduino IDE), wait one second, then press the red RESET button on your Arduino in real life
7. If it says **Done Uploading**, then continue
8. Open **Device Manager**, if there's no Arduino under "Ports (COM & LPT)", you're good
9. Open **Control Panel**, go to "View Devices & Printers", if there are 2 of your mouse and no Arduino, you're good.
10. Run `Discord THEMIDA.exe` as admin (protected with Themida, open `Discord.exe` for the non-protected version)

**DO NOT ASK ME FOR HELP ON DISCORD**
**WARNING: THIS CHEAT IS DETECTED ON FORTNITE AND YOU WILL GET DELAY BANNED!!**

## Troubleshooting
- Will update in the future. Haven't had any testers yet.

## Credits
- Seconb (me)
- The Aimmy Developers, because this is just Aimmy with some mods.
- Whoswhip, for their fork of Aimmy which includes bezier curve, DirectX screen capture, and other such features.

## How to Use Bezier Curve
In the Aim Config Settings you will find the bezier curve strength option, increasing this will give the aim path a stronger curve, whilst decreasing it will give the aim path a straighter line.
![aimmy curve 2](https://github.com/user-attachments/assets/a292c337-1f80-4fa7-b3d8-117e0f8dcb43)

