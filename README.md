# Aimmy Arduino Edition
Aimmy 1.5.2... but with Arduino support!
Wish I could add this to Aimmy 2.0, but unfortunately, it's closed source and I'm not an official dev for Aimmy.

**DO NOT DM ME ON DISCORD FOR HELP I WILL BLOCK YOU**

Tags: Arduino AI Aim, AI chair, AI Aim Assist, AI undetected, Undetected bot, Fortnite Bot Undetected, Fortnite Arduino

# Features and Advantages:
- HID communication (No COM port required! Say goodbye to detection!)
- Easy to setup, just upload the script to your Arduino and get to work! (Unless you haven't spoofed your Arduino and disabled the COM port)
- Unlike Aimmy, fully undetected in R6, CoD, Apex, Fortnite, and some other games. If you use this in Valorant, it'll flag your account so you won't get banned for a month or 2 or until you reach Ascendant rank. CS2 FaceIt probably isn't safe either.

# Downsides:
- As of right now, it does not support USB Host Shields. (Should be easy to add though! If you succeed in adding it, please message me on Discord: Seconb). I can't add Host Shield support because I don't have one.
- Does not support Arduinos that don't have an ATmega32U4 chip, meaning only the Arduino Micro and Arduino Leonardo R3 will work.
- The Aimmy part itself is really easy to setup because you just extract it and run Netflix.vmp.exe, but if you don't know how to use an Arduino then have fun doing the tutorial below! I will not help you I swear to god if you DM me asking for help (and not related to the Host Shield thing I said above) I will block you so fast.

# Arduino Setup Tutorial
- Download Arduino IDE 1.8.19, accept everything in the setup. [https://downloads.arduino.cc/arduino-1.8.19-windows.exe](url)
- Spoof your Arduino's PID and VID. This makes it so that your Arduino looks like an exact copy of your mouse. Do what this guy does in his video: [https://www.youtube.com/watch?v=krjCJBfBgr4](url) (THIS IS SOMEONE'S SETUP FOR A RANDOM VALORANT CHEAT, IGNORE THAT PART. JUST DO THE PID AND VID CHANGING THING).
- Disable COM port: While you're still in the boards.txt to spoof your Arduino, look for the line that says "leonardo.build.extra_flags={build.usb_flags}" and add -DCDC_DISABLED to the end of it, so it becomes "leonardo.build.extra_flags={build.usb_flags} -DCDC_DISABLED" (without the quotes obviously)
- Go to the files you extracted from downloading
- Go to the download for the tool and extract it anywhere
- Go to the Arduino folder and run MouseInstructArduino.ino
- When the Arduino IDE opens, click the button in the top left that shows an arrow pointing to the right (the Upload button) then QUICKLY press the reset button on your Arduino, which should look something like this: [https://support.arduino.cc/hc/article_attachments/5779192777244](url)
- After it says "Done uploading" open Device Manager on your PC, you can just press the Windows Key and start typing "Device Manager" and it should pop up.
- Click the arrow next to "Ports (COM & LPT)
- If the only thing you see is "Communications Port (COM1)" you're good.
- Close Device Manager and open Control Panel
- Click "View Devices and Printers", if you see one of the Devices looks like a mouse and it's called HID-compliant device or like the same name as you put while spoofing your Arduino, you're good.
- If you see something other than COM1 or you see an Arduino in Control Panel instead of HID-compliant device or something like that then you messed up in disabling COM port or spoofing. Do some Googling and fix your mistake or ask someone who knows what they're doing to help you. (**DO NOT DM ME ON DISCORD FOR HELP I WILL BLOCK YOU**)

# Setup Tutorial
- Do the Arduino Setup Tutorial before you do this
- Download and extract Aimmy Arduino Edition if you haven't done that already, makes sure your anti-virus is off. Most of what you do with real Aimmy applies.
- Run Netflix.vmp.exe
- Yes I know it looks suspicious as hell when you open "Netflix" and see a bunch of command prompts open and close I swear on god I'm not ratting 😭, It's just what the C# code does. The Netflix thing is also just a security measure against anti-cheats. If you don't believe me, try decompiling the program with ilSpy and decompile the mousemovement.exe using pyinstxtractor or whatever it's called. It's open source anyway so who cares!
- If it says "Mouse device found" then that means it's probably working. Use it the same as you would use normal Aimmy.

**DO NOT DM ME ON DISCORD FOR HELP I WILL BLOCK YOU**

# Credits:
- [https://github.com/khanxbahria/MouseInstruct](url) - HUGE HELP THIS REPO IS REALLY AWESOME AND THE ONLY REASON WHY THIS EXISTS GO TO IT AND STAR IT!!!!
- ChatGPT Plus - Self explanatory, completed the parts I was too lazy to write
- Seconb (me) - I did all the work over a span of like 6 or 7 hours (the first prototype took like 4 hours and didn't work so I had to start from scratch, then the second version worked)
