; ThinkMine Complete Installer with Fonts
; This installer includes all required fonts and installs them automatically

#define MyAppName "ThinkMine"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "ThinkMine Publisher"
#define MyAppURL "https://github.com/raiyaancreates/ThinkMine"
#define MyAppExeName "ThinkMine.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName=E:\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=ThinkMineSetup-v{#MyAppVersion}
WizardImageFile=logo.bmp
WizardSmallImageFile=logo.bmp
SetupIconFile=app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "installfonts"; Description: "Install required fonts (recommended)"; GroupDescription: "Additional options:"; Flags: checkedonce

[Files]
; Application files
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Font files (bundled but not registered by Inno)
Source: "Fonts\*.ttf"; DestDir: "{app}\Fonts"; Flags: ignoreversion

; Font Installer Script
Source: "LocalInstallFonts.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Run the font installer script
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\LocalInstallFonts.ps1"""; StatusMsg: "Installing fonts..."; Flags: runhidden waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// No custom code needed for fonts anymore
