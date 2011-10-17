Last Changed: 17, March, 2008
Version: 0.1
Author: Eran Kampf (http://www.ekampf.com/)


WPF Screen Saver Application Readme
----------------------------------
This project builds a WPF screen saver application.

The screensaver application creates an instance of Main for each monitor so that the visuals
will be duplicated on each monitor.


Deployment Instructions
-----------------------
In order to deploy your screen saver, you must rename the 
RELEASE build of the screen saver executable to have a ".scr" extension:
	1.  Go to bin\Release folder of your project.
	2.  Rename the .exe to .scr.  
	3.  If using .NET Application Settings, see important note below.

User Installation Instructions
------------------------------
To install on a client machine:
	1.  Copy the .scr file (& any dependent files) to convenient location.
	2.  Right click the .scr file.
	3.  Select Install.

To configure on a client machine:
	1.  In the Windows Screen Saver Dialog, select the screen saver.
	2.  Click Settings button.

To uninstall:
	1.  Delete .scr file.

Files
-----
The following main files are used in this project:
	1.  App.xaml/App.xaml.cs - Sets up the screensaver applications.
	2.  Main.xaml/Main.xaml.cs - Main screensaver window.
	3.  Settings.xaml/Settings.xaml.cs - Settings window.


Modes
-----
The screen saver can be launched with different command line arguments:
	<no args>  Display screen saver
	"/s"	   Display screen saver
	"/c"	   Show settings window


Debugging 
---------
In DEBUG configuration, the screensaver's window is not set top Topmost and to shutdown on keypress\mousemove..
Use ALT-F4 to close the window.

Debugging the settings window:
	1.  Go to the project's properties pane. 
	    (Right click the project in the Solution Explorer & select "Properties"). 
	2.  Select Debug on the left tabs.  
	3.  Find "Command Line Args" textbox under "Start Options".  
	4.  Enter: /c


Saving Screen Saver Settings
----------------------------
I highly recommend using the .NET Application Settings feature to 
save user-configured screen saver settings.  

More details at http://msdn2.microsoft.com/en-us/library/k4s6c3a0.aspx


Important Note:
	By default, the .NET Application Settings framework stores 
	user settings based on the executing assembly name.  

	The Windows Screen Saver Dialog launches the screen saver (for 
	settings & preview) using the full assembly name 
	(e.g. MyCoolScreenSaver.scr).

	However, when launching the screensaver for real, Windows 
	uses the shortened version (e.g. MYCOOL~1.SCR).  Since the 
	name is different, the settings are loaded from a different place.

	If you use .NET Application Settings with the default settings store,
	YOU MUST GIVE YOUR SCREEN SAVER ASSEMBLY A NAME WITH 
	8 CHARACTERS OR LESS.


Release Notes
-------------
- Does not support real time preview in the Windows Screen Saver Dialog's 
  embedded display.



