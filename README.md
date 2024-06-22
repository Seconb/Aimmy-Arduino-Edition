## Aimmy Arduino Edition!
Aimmy but with Arduino support!

Star this repo to support me, but to support Aimmy's development donate to Babyhamsta via the sponsor link on the official Aimmy GitHub repo!

**DO NOT ASK FOR HELP ON DISCORD. DO NOT DM ME ON DISCORD AT ALL**

Tags: Arduino AI Aim, AI chair, AI Aim Assist, AI undetected, Undetected bot, Fortnite Bot Undetected, Fortnite Arduino

## Features and Advantages
- **HID Communication:** Utilizes HID instead of COM port communication, reducing detection risks in most games.
- **Easy Setup:** Straightforward script upload process to your Arduino. Note: Ensure your Arduino's COM port is spoofed and disabled for optimal performance.
- **Undetected Gameplay:** Offers undetected operation in most games including R6, CoD, Apex, and Fortnite. Detected in Valorant and CS2 FaceIt. (You will get banned on Fortnite if you don't spoof correctly. Keep that in mind.)

## Limitations

- **USB Host Shields:** Does not support USB Host Shields. If you know what you're doing then try this: https://www.unknowncheats.me/forum/valorant/642071-arduino-hid-mouse-free-libraries.html
- **Chip Compatibility:** Specifically designed for Arduinos with an ATmega32U4 chip, such as the Leonardo R3. Other Arduinos might work by installing HoodLoader2 but the autospoofer won't work with those.

## Setup Tutorial
- [Video Tutorial](https://streamable.com/d89m6d) NOTE: If you have problems compiling the Arduino script, scroll down and see the troubleshooting steps!
- Download and install [Arduino IDE 1.8.19](https://downloads.arduino.cc/arduino-1.8.19-windows.exe)
- Download [Aimmy Arduino Edition Download](https://github.com/Seconb/Aimmy-Arduino-Edition/releases/tag/v2) and extract it
- Run arduinospoofer.exe **AS ADMIN**
- Once that finishes, open MouseInstructArduino.ino
- Next, click the upload button (the arrow pointing to the right in the top left of the Arduino IDE), wait one second, then press the red RESET button on your Arduino in real life
- If it says "Done Uploading.", you're set to continue, otherwise try again or fix whatever error it gave you.
- Run Discord_protected.exe as admin (You can also run Discord.exe, but it's less safe against anticheats because it isn't packed with Themida)
**- ALSO I HIGHLY RECOMMEND YOU MANUALLY VMPROTECT/THEMIDA Discord.exe!!! Your file will have a different md5 signature and will therefore be harder to detect. I only have 1 detection report from this on Fortnite so far but better to be safe than sorry!
- ALSO SOLID BAN CHANCE ON BE/EAC IF YOU DON'T DISABLE COM PORT**

**DO NOT ASK ME FOR HELP ON DISCORD**

## Troubleshooting
- If the Arduino script doesn't compile I would double check that you're NOT using the Windows Store version of Arduino IDE and you are using the one from the link in the setup tutorial.
- If you get No Such Directory "HID-Settings.h" (or any No Such Directory Error) then press CTRL+Shift+I in the Arduino IDE, search for HID-Project, and install it. Then, try again. If that doesn't work, try replacing the MouseInstructArduino folder with the one from here (replace nothing else, only MouseInstructArduino): https://github.com/Seconb/Aimmy-Arduino-Edition/releases/download/v1/Aimmy.Arduino.v2.1.rar
- If your script doesn't compile, spoof your Arduino again using the Arduino spoofer and then go to %programfiles(x86)%\Arduino\hardware\arduino\avr\ by copy and pasting that into the Windows Search bar. Then, copy the boards.txt from there to %localappdata%\Arduino15\packages\arduino\hardware\avr\1.8.6 by copy and pasting it into the Windows Search bar. Next, right click the boards.txt files, go to properties, and check "Read-only". Save that. If you need to spoof your Arduino again, uncheck "Read-only" on both boards.txt files before doing so. It's really important that both boards.txt files are the same so confirm that they are and both are spoofed. You really have to make sure you picked the right mouse in the spoofer.
- If you have any other issue consider watching the video extra carefully and redoing it. If all else fails idk what to tell you because I don't want to help you on Discord. This isn't for some inexperienced users this is for people who desperately need Arduino for Aimmy before it ever becomes an official update.
- If your Arduino has issues working in general, try using a different USB port and a better cable. I recommend the one that comes with the Arduino. A lot of times, using some random cable you have somewhere is bad because they aren't made for something as powerful as an Arduino
- If COM ports aren't disabled after spoofing, make sure you have Arduino IDE installed correctly. If it is installed correctly, download this: https://github.com/Seconb/Aimmy-Arduino-Edition/releases/download/v1/USBCore.cpp  then go to C:\Program Files (x86)\Arduino\hardware\arduino\avr\cores\arduino and replace USBCore.cpp with the one you downloaded. Do the same for C:\Users\karab\AppData\Local\Arduino15\packages\arduino\hardware\avr\1.8.6\cores\arduino if you have that folder (not everyone has it).


## Credits:

- [MouseInstruct Repository](https://github.com/khanxbahria/MouseInstruct) for their amazing HID library. Made mouse movement via Arduino easy.
- Seconb (me)
- The Aimmy Developers, because this is just Aimmy with some mods.
