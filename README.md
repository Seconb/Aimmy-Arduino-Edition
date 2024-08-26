# Aimmy Arduino Edition!

Aimmy but with Arduino support!

**DO NOT ASK FOR HELP ON DISCORD. DO NOT DM ME ON DISCORD AT ALL**

## Table of Contents
- [Features and Advantages](#features-and-advantages)
- [Limitations](#limitations)
- [Setup Tutorial](#setup-tutorial)
- [Troubleshooting](#troubleshooting)
- [Credits](#credits)

## Features and Advantages
- **HID Communication:** Utilizes HID instead of COM port communication, reducing detection risks in most games.
- **Easy Setup:** Straightforward script upload process to your Arduino. Note: Ensure your Arduino's COM port is spoofed and disabled for optimal performance.
- **Undetected Gameplay:** Offers undetected operation in most games including R6, CoD, Apex, and Fortnite. Detected in Valorant and CS2 FaceIt.

## Limitations
- **USB Host Shields:** Does not support USB Host Shields. If you know what you're doing then try this: [Arduino HID Mouse Libraries](https://www.unknowncheats.me/forum/valorant/642071-arduino-hid-mouse-free-libraries.html)
- **Chip Compatibility:** Specifically designed for Arduinos with an ATmega32U4 chip, such as the Leonardo R3. Other Arduinos might work by installing HoodLoader2 but the autospoofer won't work with those.

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

## Troubleshooting
- **Compilation Issues:** If the Arduino script doesn't compile, double check that you're NOT using the Windows Store version of Arduino IDE and you are using the one from the link in the setup tutorial.
- **"Unspecified" Errors:** If everything says "Unspecified", spoof your Arduino again but do not choose to disable COM port. Then, upload `MouseInstructArduino` again. It should work now but it's more likely to get detected in some games in the future. This is unfortunately the only known fix.
- **Missing Directories:** If you get `No Such Directory "HID-Settings.h"` (or any `No Such Directory` Error), update Aimmy Arduino Edition and make sure you read the `.txt` file in `MouseInstructArduino`.
- **Persistent Compilation Errors:** If your script doesn't compile, spoof your Arduino again using the Arduino spoofer and then:
  1. Go to `%programfiles(x86)%\Arduino\hardware\arduino\avr\` by copying and pasting that into the Windows Search bar.
  2. Copy the `boards.txt` from there to `%localappdata%\Arduino15\packages\arduino\hardware\avr\1.8.6` by copying and pasting it into the Windows Search bar.
  3. Right-click the `boards.txt` files, go to **Properties**, and check "Read-only". Save that.
  4. If you need to spoof your Arduino again, uncheck "Read-only" on both `boards.txt` files before doing so.
  
  **Important:** Ensure that both `boards.txt` files are the same and both are spoofed. Make sure you picked the right mouse in the spoofer.
- **Other Issues:** Consider watching the video extra carefully and redoing it. If all else fails, there's no support on Discord. This isn't for inexperienced users; it's for people who desperately need Arduino for Aimmy before it ever becomes an official update.
- **Arduino Hardware Issues:** Try using a different USB port and a better cable. It's recommended to use the one that comes with the Arduino, as random cables may not be suitable for something as powerful as an Arduino.
- **COM Ports Not Disabled After Spoofing:** 
  1. Ensure Arduino IDE is installed correctly.
  2. If it is installed correctly, download this: [USBCore.cpp](https://github.com/Seconb/Aimmy-Arduino-Edition/releases/download/v1/USBCore.cpp)
  3. Replace `USBCore.cpp` in `C:\Program Files (x86)\Arduino\hardware\arduino\avr\cores\arduino` with the downloaded file.
  4. Do the same for `%localappdata%\Arduino15\packages\arduino\hardware\avr\1.8.6\cores\arduino` if you have that folder (not everyone has it).

## Credits
- [MouseInstruct Repository](https://github.com/khanxbahria/MouseInstruct) for their amazing HID library. Made mouse movement via Arduino easy.
- Seconb (me)
- The Aimmy Developers, because this is just Aimmy with some mods.

