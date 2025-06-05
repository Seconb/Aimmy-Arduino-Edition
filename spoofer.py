import hid
import os
import stat
import time
import os
import re
 
print("Seconb's Basic Arduino Spoofer")
print("This isn't perfect, and won't make your Arduino 1:1 to a real mouse, but will work on MOST games. Don't use on main PC for full safety.")
print("Report any issues to Seconb on UnknownCheats")
print("RUN THIS AS ADMIN OR IT WILL NOT WORK")
 
time.sleep(3)
 
n = 1
for device in hid.enumerate():
    if device['usage_page'] == 0x01 and device['usage'] == 0x02:
        print(f"{n}. Name: {device['product_string']}")
        print(f"{n}. Manufacturer: {device['manufacturer_string']}")
        print(f"{n}. VID: 0x{device['vendor_id']:04X}")
        print(f"{n}. PID: 0x{device['product_id']:04X}")
        n += 1
 
print("Enter the mouse info to spoof your Arduino to:")
new_vid = input("New VID (e.g. 0x1234): ").lower()
new_pid = input("New PID (e.g. 0x5678): ").lower()
new_name = input("New Device Name: ")
new_manufacturer = input("New Manufacturer: ")
disable_com = "n"
 
spoof_power = input("Do you want to spoof the power usage? (Y or N): ").strip().lower()
if spoof_power == "y":
    custom_power_limit = input("Enter the custom power limit (100 recommended): ").strip()
    try:
        custom_power_limit = int(custom_power_limit)
    except ValueError:
        custom_power_limit = 100
else:
    custom_power_limit = None
 
boards_path = r"C:\Program Files (x86)\Arduino\hardware\arduino\avr\boards.txt"
if not os.path.exists(boards_path):
    raise FileNotFoundError(f"boards.txt not found at: {boards_path}")
 
os.chmod(boards_path, stat.S_IWRITE)
 
with open(boards_path, "r", encoding="utf-8") as file:
    lines = file.readlines()
 
new_lines = []
usb_mfr_added = False
for i, line in enumerate(lines):
    if line.startswith("leonardo.name="):
        new_lines.append(f"leonardo.name={new_name}\n")
    elif line.startswith("leonardo.vid.") or line.startswith("leonardo.build.vid="):
        new_lines.append(line.split("=")[0] + f"={new_vid}\n")
    elif line.startswith("leonardo.pid.") or line.startswith("leonardo.build.pid="):
        new_lines.append(line.split("=")[0] + f"={new_pid}\n")
    elif line.startswith("leonardo.build.usb_product="):
        new_lines.append(f'leonardo.build.usb_product="{new_name}"\n')
    elif line.startswith("leonardo.build.extra_flags="):
        if disable_com == "y":
            if "-DCDC_DISABLED" not in line:
                new_line = line.strip() + " -DCDC_DISABLED\n"
                new_lines.append(new_line)
            else:
                new_lines.append(line)
        else:
            new_line = re.sub(r"\s*-DCDC_DISABLED", "", line)
            new_lines.append(new_line)
        if not any("leonardo.build.usb_manufacturer=" in l for l in lines):
            new_lines.append(f'leonardo.build.usb_manufacturer="{new_manufacturer}"\n')
            usb_mfr_added = True
    else:
        new_lines.append(line)
 
with open(boards_path, "w", encoding="utf-8") as file:
    file.writelines(new_lines)
 
os.chmod(boards_path, stat.S_IREAD)
print("\n boards.txt updated")
 
usbcore_path = r"C:\Program Files (x86)\Arduino\hardware\arduino\avr\cores\arduino\USBCore.cpp"
if not os.path.exists(usbcore_path):
    raise FileNotFoundError(f"USBCore.cpp not found at {usbcore_path}. Do you have Arduino IDE 1.8.19?")
 
with open(usbcore_path, "r", encoding="utf-8") as file:
    lines = file.readlines()
 
if spoof_power == "y":
    usbcoreh_path = r"C:\Program Files (x86)\Arduino\hardware\arduino\avr\cores\arduino\USBCore.h"
    if not os.path.exists(usbcoreh_path):
        raise FileNotFoundError(f"USBCore.h not found at {usbcoreh_path}. Do you have Arduino IDE 1.8.19?")
 
    with open(usbcoreh_path, "r", encoding="utf-8") as file:
        lines = file.readlines()
 
    updated_lines = []
    inside_ifndef = False
 
    for i, line in enumerate(lines):
        stripped = line.strip()
 
        if stripped.startswith("#ifndef USB_CONFIG_POWER"):
            inside_ifndef = True
            updated_lines.append(line)
            continue
 
        if inside_ifndef and stripped.startswith("#define USB_CONFIG_POWER"):
            updated_lines.append(f" #define USB_CONFIG_POWER                      ({custom_power_limit})\n")
            inside_ifndef = False
            continue
 
        updated_lines.append(line)
 
    with open(usbcoreh_path, "w", encoding="utf-8") as file:
        file.writelines(updated_lines)
 
    print(f" Power limit spoofed to {custom_power_limit}.")
 
print(" Now upload any script to your Arduino and the device will appear spoofed.")