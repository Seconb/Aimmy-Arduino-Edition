# i will not lie chatgpt did do a lot of the work for this code
# only out of my own laziness


from colorama import Style, Fore
from time import sleep
from os import system, path, chmod
import win32com.client
import re
import ctypes
import sys

vid_pattern = re.compile(r"leonardo\.vid(\.\d+)?=0x[0-9A-F]+")
pid_pattern = re.compile(r"leonardo\.pid(\.\d+)?=0x[0-9A-F]+")
name_pattern = re.compile(r'leonardo\.name=.*')
usb_product_pattern = re.compile(r'leonardo\.build\.usb_product=.*')
build_vid_pattern = re.compile(r'leonardo\.build\.vid=0x[0-9A-F]+')
build_pid_pattern = re.compile(r'leonardo\.build\.pid=0x[0-9A-F]+')
extra_flags_pattern = re.compile(r'leonardo\.build\.extra_flags=\{build\.usb_flags\}')

def list_mice_devices():
    wmi = win32com.client.GetObject("winmgmts:")
    devices = wmi.InstancesOf("Win32_PointingDevice")
    mice_list = []

    for device in devices:
        name = device.Name
        match = re.search(r'VID_(\w+)&PID_(\w+)', device.PNPDeviceID)

        vid, pid = match.groups() if match else (None, None)
        mice_list.append((name, vid, pid))

    return mice_list

def main():
    system("cls")
    print(Style.BRIGHT + Fore.CYAN + "[+] Seconb's Arduino Spoofer [+]")
    print(Style.RESET_ALL + "[1] Spoof Arduino")
    print("[2] Undo Spoof")
    mainselection = int(input("Select an option: "))
    if mainselection != 1 and mainselection != 2:
        system("cls")
        print(Style.BRIGHT + Fore.CYAN + "[+] Seconb's Arduino Spoofer [+]")
        print(Style.BRIGHT + Fore.RED + "Invalid response... pick again!")
        sleep(2)
        main()
    else:
        if mainselection == 1:
            spoofarduino()
        if mainselection == 2:
            undospoof()

def undospoof():
    system('cls')
    print(f"{Style.BRIGHT + Fore.BLUE}[+] Unspoofing Arduino back to original... [+]")
    locations = [
        path.expandvars("%LOCALAPPDATA%/Arduino15/packages/arduino/hardware/avr/1.8.6/boards.txt"),
        path.expandvars("%LOCALAPPDATA%/Arduino15/packages/arduino/hardware/avr/1.8.5/boards.txt"),
        path.expandvars("%programfiles(x86)%/Arduino/hardware/arduino/avr/boards.txt")
    ]
    
    script_directory = path.dirname(path.abspath(__file__))
    boards_txt_path = path.join(script_directory, "boards.txt")
    
    if not path.exists(boards_txt_path):
        print(f"{Style.BRIGHT + Fore.RED}Couldn't find the old boards.txt... Redownload the script maybe? \n")
        sleep(4)
        main()

    try:
        with open(boards_txt_path, 'r') as file:
            boards_txt_content = file.read()
    except Exception as e:
        print(f"{Style.BRIGHT + Fore.RED}Error reading boards.txt file: {e}")
        return
    
    for file_path in locations:
        if path.exists(file_path):
            try:
                chmod(file_path, 0o777)
                
                with open(file_path, 'w') as file:
                    file.write(boards_txt_content)
                
                print(f"{Style.BRIGHT + Fore.GREEN}Successfully restored {file_path} to original.\n")
            except Exception as e:
                print(f"{Style.BRIGHT + Fore.RED}Error restoring {file_path} to original: {e}\n")
                sleep(4)
                main()
    print(f"{Style.BRIGHT + Fore.BLUE}Unspoofing complete.\n")
    sleep(4)
    main()

def replace_and_save_boards_txt(mouse_vid, mouse_pid, mouse_name, com_choice):
    print(f"{Fore.BLUE}Configuring Arduino spoofing with VID: {mouse_vid}, PID: {mouse_pid}, Name: {mouse_name}")
    locations = [
        path.expandvars("%LOCALAPPDATA%/Arduino15/packages/arduino/hardware/avr/1.8.6/boards.txt"),
        path.expandvars("%LOCALAPPDATA%/Arduino15/packages/arduino/hardware/avr/1.8.5/boards.txt"),
        path.expandvars("%programfiles(x86)%/Arduino/hardware/arduino/avr/boards.txt")
    ]

    for file_path in locations:
        if path.exists(file_path):
            try:
                chmod(file_path, 0o777) # make sure the file isnt read only before editing
                
                with open(file_path, 'r') as file:
                    lines = file.readlines()

                with open(file_path, 'w') as file:
                    for line in lines:
                        if vid_pattern.match(line):
                            line = f"leonardo.vid={mouse_vid}\n"
                        elif pid_pattern.match(line):
                            line = f"leonardo.pid={mouse_pid}\n"
                        elif build_vid_pattern.match(line):
                            line = f"leonardo.build.vid={mouse_vid}\n"
                        elif build_pid_pattern.match(line):
                            line = f"leonardo.build.pid={mouse_pid}\n"
                        elif name_pattern.match(line) or usb_product_pattern.match(line):
                            if 'name' in line:
                                # Do not add quotes for leonardo.name
                                line = f"leonardo.name={mouse_name}\n"
                            else:
                                # Add quotes for leonardo.build.usb_product
                                line = f'leonardo.build.usb_product="{mouse_name}"\n'
                        elif com_choice.upper() == 'Y' and extra_flags_pattern.match(line):
                            line = 'leonardo.build.extra_flags={build.usb_flags} -DCDC_DISABLED\n'
                        elif com_choice.upper() == 'N' and extra_flags_pattern.match(line):
                            line = 'leonardo.build.extra_flags={build.usb_flags}\n'
                        file.write(line)

                chmod(file_path, 0o444) # set it to read only so that arduino ide cant edit anything

                print(f"{Fore.GREEN}Successfully modified: {file_path}")
            except Exception as e:
                print(f"{Fore.RED}Error modifying {file_path}: {e}")
                print(Style.BRIGHT + Fore.CYAN + "Are you sure Arduino IDE 1.8.19 is installed correctly? Returning to menu soon.")
                sleep(5)
                main()
    print(f"{Fore.BLUE}Spoofing complete. Now continue the tutorial.")
    sleep(5)
    main()

def select_mouse_and_configure():
    global vid, pid, name
    print(Fore.CYAN + "\nDetecting mice devices...")
    mice = list_mice_devices()

    if not mice:
        print(Fore.RED + "No mouse devices found. Exiting...")
        sleep(5)
        exit()

    for idx, (name, vid, pid) in enumerate(mice, 1):
        print(f"{Fore.CYAN}{idx} →{Fore.RESET} {name} | VID: {vid or 'Not found'}, PID: {pid or 'Not found'}")

    choice = int(input(Fore.CYAN + "Select your mouse number: ")) - 1
    name, vid, pid = mice[choice]
    name = input(Fore.CYAN + "What is your mouse's name?: ")
    comChoice = input(Fore.YELLOW + "Disable COM port? (Recommended) Y/N: ").strip().upper()
    replace_and_save_boards_txt("0x" + vid, "0x" + pid, name, comChoice)

def spoofarduino():
    system("cls")
    print(Style.BRIGHT + Fore.CYAN + "[+] Seconb's Arduino Spoofer [+]")
    print(Style.BRIGHT + Fore.YELLOW + "\nSelect your mouse...")
    print(Style.BRIGHT + Fore.GREEN + "[?] Don't know which one's your mouse? Open Control Panel, click View devices and printers, right click your mouse, go to properties, hardware, properties again, details tab, and check the Device instance path to see the PID and VID")
    select_mouse_and_configure()
if __name__ == "__main__":
    main()
