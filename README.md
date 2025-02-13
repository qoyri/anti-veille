# Anti Veille

**Anti Veille** is a Windows utility that monitors user activity—via both a connected webcam (using OpenCV for face detection) and system input (mouse, keyboard, touch, etc.)—and automatically puts the computer into lock mode after a configurable period of inactivity. It also runs quietly in the system tray, offering easy control over its operation and ensuring it starts automatically with Windows.

## Features

- **Dual Activity Detection:**  
  Combines face detection (via webcam) and system input monitoring to reliably determine user activity.

- **Automatic Lock session:**  
  When no user activity is detected for a specified period, Anti Veille triggers Windows lock mode.

- **System Tray Integration:**  
  Runs in the background with a tray icon that allows you to:
    - Open a debug window.
    - Pause or resume the detection.
    - Quit the application.

- **Self-Installation & Auto-Startup:**  
  On first run, the application:
    - Automatically moves itself to a dedicated folder in `%LocalAppData%\AntiVeille` for proper deployment.
    - Creates a startup shortcut so that it launches automatically with Windows.

- **Robust Error Handling:**
    - If the cascade file (used for face detection) is missing, the app downloads it automatically.
    - In case of download failure, a user-friendly pop-up provides manual download instructions.

## How It Works

1. **Activity Monitoring:**
    - **Webcam Detection:** Uses OpenCV and a Haar Cascade classifier to detect faces.
    - **User Input Monitoring:** Utilizes Windows API (`GetLastInputInfo`) to check for recent keyboard, mouse, or touch activity.

2. **Inactivity Timer:**  
   Both detection methods reset a countdown timer. If no activity is detected (by either method) within a preset time (e.g., 10 minutes), the app puts the computer to lock session.

3. **Auto-Installation:**  
   Upon first launch, if the executable is not located in `%LocalAppData%\AntiVeille`, the app:
    - Copies itself to that folder.
    - Launches the copied version.
    - Creates a startup shortcut in the Windows Startup folder.

## Installation

**For End Users:**

1. **Download the Installer:**  
   Simply download the latest release (an executable file) from the [Releases](https://github.com/qoyri/anti-veille/releases) page.

2. **Run the EXE:**  
   Double-click the executable to start the application. On first launch:
    - The application will check its current location.
    - If it is not in `%LocalAppData%\AntiVeille`, it will automatically copy itself there and restart.
    - A startup shortcut will be created so that Anti Veille runs automatically each time Windows starts.

3. **Control via System Tray:**
    - A tray icon will appear (green when active, orange when paused, and red if an error occurs).
    - Right-click the tray icon for options like Debug, Pause/Resume, or Quit.

**Manual Installation:**

If the automatic move fails, copy the executable manually to:
```C:\Users<YourUsername>\AppData\Local\AntiVeille```

Then create a shortcut of the EXE in your Startup folder:
```%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup```


## Requirements

- Windows 10 or later
- .NET 6.0 (or .NET 8.0 if using the latest preview) – Ensure you have the appropriate runtime installed.
- A webcam (for face detection)

## Configuration

- **Lock Timeout:**  
  The Lock timeout is automatically determined by Windows power settings (via `PowerHelper.GetSleepTimeoutAC()`) and can be overridden in the configuration.

- **Detection Sensitivity:**  
  The sensitivity for face detection (Haar Cascade) can be adjusted via a slider in the application's UI. This value is saved in the registry and reloaded on startup.

## Contributing

Contributions are welcome! Please fork the repository and create a pull request for any improvements or bug fixes.

1. Fork the repository.
2. Create your feature branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin feature/my-feature`
5. Open a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [OpenCV](https://opencv.org/) for the powerful computer vision library.
- Microsoft for .NET and WPF.
