$ErrorActionPreference = "Stop"

$appPath = Join-Path $PSScriptRoot "bin\Debug\net8.0-windows\ThinkMine.exe"
$iconPath = Join-Path $PSScriptRoot "thinkmine.ico"
$progId = "ThinkMine.Document"

if (-not (Test-Path $appPath)) {
    Write-Warning "Executable not found at $appPath. Please build the project first."
    exit
}

if (-not (Test-Path $iconPath)) {
    Write-Warning "Icon not found at $iconPath."
    exit
}

# Register .tm extension
New-Item -Path "HKCU:\Software\Classes\.tm" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\.tm" -Name "(default)" -Value $progId

# Register ProgID
New-Item -Path "HKCU:\Software\Classes\$progId" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\$progId" -Name "(default)" -Value "ThinkMine Document"

# Register DefaultIcon
New-Item -Path "HKCU:\Software\Classes\$progId\DefaultIcon" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\$progId\DefaultIcon" -Name "(default)" -Value $iconPath

# Register Open Command
New-Item -Path "HKCU:\Software\Classes\$progId\shell\open\command" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\$progId\shell\open\command" -Name "(default)" -Value "`"$appPath`" `"%1`""

Write-Host "Registered .tm extension with ThinkMine."
Write-Host "You may need to restart Explorer or sign out/in for icon changes to appear."
