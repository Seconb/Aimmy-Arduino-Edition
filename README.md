## Aimmy Arduino Edition!
Aimmy but with Arduino support!

Star this repo to support me, but to support Aimmy's development donate to Babyhamsta via the sponsor link on the official Aimmy GitHub repo!

**DO NOT ASK FOR HELP ON DISCORD. ONLY DM ME IF YOU WANNA ADD HOST SHIELD SUPPORT.**

Tags: Arduino AI Aim, AI chair, AI Aim Assist, AI undetected, Undetected bot, Fortnite Bot Undetected, Fortnite Arduino

## Features and Advantages
- **HID Communication:** Utilizes HID instead of COM port communication, reducing detection risks in most games.
- **Easy Setup:** Straightforward script upload process to your Arduino. Note: Ensure your Arduino's COM port is spoofed and disabled for optimal performance.
- **Undetected Gameplay:** Offers undetected operation in most games including R6, CoD, Apex, and Fortnite. Detected in Valorant and CS2 FaceIt.

## Limitations

- **USB Host Shields:** Does not support USB Host Shields. Community contributions for this feature are welcome. Plug your mouse into your PC instead of the shield if you are using one.
- **Chip Compatibility:** Specifically designed for Arduinos with an ATmega32U4 chip, such as the Leonardo R3. Other Arduinos might work by installing HoodLoader2 but the autospoofer won't work with those.

## Setup Tutorial
- [Video Tutorial](https://streamable.com/oknd08) NOTE: If you have problems compiling the Arduino script, scroll down and see the troubleshooting steps!
- Download and install [Arduino IDE 1.8.19](https://downloads.arduino.cc/arduino-1.8.19-windows.exe)
- Download [Aimmy Arduino Edition Download](https://github.com/Seconb/Aimmy-Arduino-Edition/releases/tag/v2) and extract it
- Run arduinospoofer.exe **AS ADMIN**
- Once that finishes, open MouseInstructArduino.ino
- Next, click the upload button (the arrow pointing to the right in the top left of the Arduino IDE), wait one second, then press the red RESET button on your Arduino in real life
- If it says "Done Uploading.", you're set to continue, otherwise try again or fix whatever error it gave you.
- Run Discord_protected.exe as admin (You can also run Discord.exe, but it's less safe against anticheats because it isn't packed with Themida)

**DO NOT ASK ME FOR HELP ON DISCORD**

## Troubleshooting
- If your script doesn't compile, spoof your Arduino again using the Arduino spoofer and then go to %programfiles(x86)%\Arduino\hardware\arduino\avr\ by copy and pasting that into the Windows Search bar. Then, copy the boards.txt from there to %localappdata%\Arduino15\packages\arduino\hardware\avr\1.8.6 by copy and pasting it into the Windows Search bar. Next, right click the boards.txt files, go to properties, and check "Read-only". Save that. If you need to spoof your Arduino again, uncheck "Read-only" on both boards.txt files before doing so. It's really important that both boards.txt files are the same so confirm that they are and both are spoofed.
- If you have any other issue consider watching the video extra carefully and redoing it. If all else fails idk what to tell you because I don't want to help you on Discord. This isn't for some inexperienced users this is for people who desperately need Arduino for Aimmy before it ever becomes an official update.

## Credits:

- [MouseInstruct Repository](https://github.com/khanxbahria/MouseInstruct) for their amazing HID library. Made mouse movement via Arduino easy.
- Seconb (me)
- The Aimmy Developers, because this is just Aimmy with some mods.
