# Aimmy Arduino Edition!

Aimmy but with Arduino support!

** Detected on many games, honestly it's just Aimmy's fault lol, it's flagged pretty bad. **

**DO NOT ASK FOR HELP ON DISCORD. DO NOT DM ME ON DISCORD AT ALL**

## Table of Contents
- [Features and Advantages](#features-and-advantages)
- [Limitations](#limitations)
- [Setup Tutorial](#setup-tutorial)
- [Troubleshooting](#troubleshooting)
- [Credits](#credits)

## Features
- **Easy Setup:** Setup in just a few clicks.
- **Undetected Gameplay:** Undetected in every game except Fortnite, Valorant, and CS2. Arduino Leonardo required. (Note: As of 04/01/2025 there are actually a lot more games where it's detected but the issue is with Aimmy itself being flagged, not Arduino. Use with caution and ask in the community found games channel for help in the Aimmy Discord)
- **USB Host Shield Support:** Added support for USB Host Shields, though this support probably won't work on Logitech and Razer mice. Many mice will have scroll wheel and side button issues.
- **Improvements over normal Aimmy: **This uses Whoswhip's fork of Aimmy, which features the following:
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
0. Video: [Setup Tutorial on Streamable](https://streamable.com/c77jhu) https://streamable.com/c77jhu (Text tutorial below)
1. Download Arduino IDE 1.8.19 ("legacy" download on arduino.cc)
2. Drag the libraries folder into Documents\Arduino\libraries (replace the old libraries folder with the one here, be careful not to corrupt your old libraries)
3. Run the spoofer, spoof according to the correct mouse.
4. Upload the script in the serial folder. (NO USB HOST SHIELD SUPPORT, PLUG MOUSE INTO PC OR ADD USB HOST SHIELD SUPPORT TO SCRIPT MANUALLY)
5. Open Device Manager, go to Ports (COM & LPT), then look for the Arduino COM Port ("USB Serial Device"). Remember what number it is.
6. Open Aimmy Arduino Edition (Spotify.exe or SpotifyTh3m1daProtected.exe, protected is better), then in settings slide the COM port to the right one. Then, click save.
7. Use like normal Aimmy.
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

