# ThinkMine

A tiny, distraction-free writing app for Windows.

## Features
- **Distraction-Free**: Borderless window, clean white canvas.
- **Minimal Controls**: Only Font, Color, and Timer controls (top-right).
- **Timer**: 
    - Click to edit (mm:ss).
    - Enter to start countdown.
    - Click to pause/resume.
    - Beeps and flashes when done.
- **Persistence**: Remembers your font, color, window size, and timer settings.

## Build & Run

**Prerequisites**: .NET 8 SDK.

1. Open a terminal in the `ThinkMine` directory.
2. Run the app:
   ```powershell
   dotnet run
   ```
3. Or build a release version:
   ```powershell
   dotnet build -c Release
   ```

## V2 Features
- **Background Cycling**: Right-click on the canvas to cycle through calm, low-eye-strain background colors. Shift+Right-click to reverse.
- **Font Cycling**: Middle-click on the canvas (or click the Font label) to cycle through minimalist fonts. Shift+Middle-click to reverse. Double-click Font label for a custom picker.
- **Saving**: 
    - `Ctrl+S`: Save (defaults to `Documents/ThinkMine/Thoughts-YYYY-MM-DD...`).
    - `Ctrl+Shift+S`: Save As.
    - Unsaved changes prompt on exit.

## Timer Logic
The timer operates as a simple state machine:
1. **Idle**: Shows `00:00` or remaining time.
2. **Edit Mode**: Click the timer text to enter edit mode. Type minutes:seconds (e.g., `25:00`) and press **Enter**.
3. **Running**: The timer counts down. Click it to **Pause**.
4. **Paused**: Click again to **Resume**.
5. **Finished**: When it hits zero, it beeps and flashes red, then resets to Idle.

## Shortcuts
- **Esc**: Exit.
- **F11**: Toggle Fullscreen.
- **Ctrl + / -**: Increase/Decrease text size.
- **Ctrl + F**: Change Font (Dialog).
- **Ctrl + Shift + C**: Change Color.
- **Ctrl + T**: Edit Timer.
- **Ctrl + S**: Save.
- **Ctrl + Shift + S**: Save As.
- **Right-Click**: Cycle Background.
- **Middle-Click**: Cycle Font.
