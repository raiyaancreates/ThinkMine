$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$fontsDir = Join-Path $scriptPath "Fonts"

if (!(Test-Path $fontsDir)) {
    Write-Host "Fonts directory not found at $fontsDir"
    exit
}

$fonts = Get-ChildItem -Path $fontsDir -Filter "*.ttf"
$installedFonts = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts" -ErrorAction SilentlyContinue

foreach ($font in $fonts) {
    try {
        # Check if font is already registered in Windows
        $fontName = $font.BaseName
        $isInstalled = $false
        
        if ($installedFonts) {
            foreach ($prop in $installedFonts.PSObject.Properties) {
                if ($prop.Value -like "*$($font.Name)*") {
                    $isInstalled = $true
                    break
                }
            }
        }
        
        if ($isInstalled) {
            Write-Host "Skipping $($font.Name) (already installed)"
            continue
        }

        Write-Host "Installing $($font.Name)..."
        
        # Copy to Windows Fonts folder
        $destPath = Join-Path "C:\Windows\Fonts" $font.Name
        Copy-Item -Path $font.FullName -Destination $destPath -Force
        
        # Register in registry
        $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"
        $regName = "$fontName (TrueType)"
        New-ItemProperty -Path $regPath -Name $regName -Value $font.Name -PropertyType String -Force | Out-Null
        
        Write-Host "Installed $($font.Name)"
    }
    catch {
        Write-Host "Failed to install $($font.Name): $_"
    }
}

Write-Host "Font installation complete"
