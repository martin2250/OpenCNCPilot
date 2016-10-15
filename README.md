# OpenCNCPilot
####autolevelling gcode-sender for grbl

OpenCNCPilot is a GRBL compatible G-Code Sender.

It's main feature is it's ability to **probe user-defined areas for warpage and wrap the toolpath around the curved surface**.
This is especially useful for engraving metal surfaces with V-shaped cutters where any deviation in the Z-direction will result in wider or narrower traces, eg for **isolation milling PCBs**.

![Screenshot](https://raw.githubusercontent.com/martin2250/OpenCNCPilot/master/img/Screenshot.png)

It is written in C# and uses WPF for it's UI. Sadly this means that it will not run under linux as Mono does not support WPF.
The 3D viewport is managed with HelixToolkit.

###Installation
Go to the [Releases section](https://github.com/martin2250/OpenCNCPilot/releases/latest) and download the latest binaries (or compile it from source).
Unzip **all** files to your hard drive and run "OpenCNCPilot.exe"

###Quick Start Guide
Before the first run, you have to select a Serial Port, the selector is hidden in the Settings menu that you can access in the "Machine" tab. Other than that you don't need to modify any settings.  
Now you can connect to your machine.

Open gcode or height map files by dragging them into the window, or using the according buttons.

To create a new height map, open the "Probing" tab and click "Create New". You will be asked to enter the dimensions.  
**Be sure to enter the actual coordinates** eg when your toolpath is in the negative X-direction, enter "-50" to "0" instead of "0" to "50".
You will see a preview of the area and the individual points in the main window

To probe the area, set up your work coordinate system by entering "G92 X0 Y0 Z0" at your selected origin, **make sure to connect A5 of your Arduino to the tool and GND to your surface**, and hit "Run".

Once it's done probing the surface, load the gcode file you want to run and hit the "Apply HeightMap" button in the "Edit" tab.
Now you can run the code with the "Start" button in the "File" tab.