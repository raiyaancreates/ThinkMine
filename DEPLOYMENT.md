# ThinkMine Deployment Guide

This guide explains how to publish and create an installer for ThinkMine.

## Option 1: Simple Self-Contained Executable (Recommended for Quick Distribution)

### Step 1: Publish as Self-Contained

Run this command to create a self-contained executable that doesn't require .NET to be installed:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

**What this does:**
- `-c Release`: Builds in Release mode (optimized)
- `-r win-x64`: Targets Windows 64-bit
- `--self-contained true`: Includes .NET runtime (no installation needed)
- `-p:PublishSingleFile=true`: Creates a single .exe file
- `-p:IncludeNativeLibrariesForSelfExtract=true`: Bundles all dependencies

**Output location:**
`bin\Release\net8.0-windows\win-x64\publish\ThinkMine.exe`

You can now distribute this single `.exe` file. Users just double-click to run!

---

## Option 2: Professional Installer with Inno Setup (Recommended for Distribution)

### Step 1: Install Inno Setup

Download and install Inno Setup from: https://jrsoftware.org/isdl.php

### Step 2: Publish the Application

First, publish the app:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

### Step 3: Create Installer Script

I've created an `installer.iss` file in your project directory. This script will:
- Create a professional Windows installer
- Add Start Menu shortcuts
- Add Desktop shortcut (optional)
- Handle uninstallation properly
- Set up file associations (optional)

### Step 4: Build the Installer

1. Open Inno Setup
2. Click **File → Open** and select `installer.iss`
3. Click **Build → Compile**
4. The installer will be created in the `Output` folder

The installer will be named: `ThinkMineSetup.exe`

---

## Option 3: ClickOnce Deployment (For Auto-Updates)

If you want automatic updates, you can use ClickOnce:

```powershell
dotnet publish -c Release -r win-x64 /p:PublishProfile=ClickOnceProfile
```

This requires setting up a ClickOnce profile in Visual Studio or manually configuring the `.pubxml` file.

---

## Recommended Approach

For most users, I recommend **Option 2 (Inno Setup)** because:
- Professional-looking installer
- Proper Start Menu integration
- Easy uninstallation
- Small installer size
- Widely trusted by Windows users

---

## File Size Optimization

If the file size is too large, you can:

1. **Use framework-dependent deployment** (requires .NET 8 installed):
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
   ```

2. **Enable trimming** (removes unused code):
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
   ```

---

## Distribution Checklist

Before distributing:
- [ ] Test the published .exe on a clean Windows machine
- [ ] Verify all fonts are working (or include font installation in setup)
- [ ] Test the installer installation and uninstallation
- [ ] Check that settings persist correctly
- [ ] Verify the save dialog works
- [ ] Test all keyboard shortcuts

---

## Next Steps

Run the publish command now, and I can help you create the installer script!
