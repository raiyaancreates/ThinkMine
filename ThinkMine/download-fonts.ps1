# Font Download Script for ThinkMine Installer
# This script downloads all required fonts for bundling with the installer

$fonts = @(
    @{Name = "Inter"; Url = "https://github.com/rsms/inter/releases/download/v4.0/Inter-4.0.zip" },
    @{Name = "Space Grotesk"; Url = "https://github.com/floriankarsten/space-grotesk/archive/refs/heads/master.zip" },
    @{Name = "Quicksand"; Url = "https://github.com/andrew-paglinawan/QuicksandFamily/archive/refs/heads/master.zip" },
    @{Name = "Playfair Display"; Url = "https://github.com/clauseggers/Playfair-Display/archive/refs/heads/master.zip" },
    @{Name = "Merriweather"; Url = "https://github.com/SorkinType/Merriweather/archive/refs/heads/master.zip" },
    @{Name = "Cormorant Garamond"; Url = "https://github.com/CatharsisFonts/Cormorant/archive/refs/heads/master.zip" },
    @{Name = "Space Mono"; Url = "https://github.com/googlefonts/spacemono/archive/refs/heads/main.zip" },
    @{Name = "Fira Code"; Url = "https://github.com/tonsky/FiraCode/releases/download/6.2/Fira_Code_v6.2.zip" },
    @{Name = "Courier Prime"; Url = "https://github.com/quoteunquoteapps/CourierPrime/archive/refs/heads/master.zip" },
    @{Name = "Oswald"; Url = "https://github.com/googlefonts/OswaldFont/archive/refs/heads/master.zip" },
    @{Name = "Syne"; Url = "https://github.com/bonjour-monde/syne-typeface/archive/refs/heads/master.zip" }
)

$fontsDir = "Fonts"
$tempDir = "Fonts_Temp"

Write-Host "Creating directories..."
New-Item -ItemType Directory -Force -Path $fontsDir | Out-Null
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

foreach ($font in $fonts) {
    try {
        Write-Host "`nDownloading $($font.Name)..."
        $zipPath = "$tempDir\$($font.Name).zip"
        
        Invoke-WebRequest -Uri $font.Url -OutFile $zipPath -ErrorAction Stop
        
        Write-Host "Extracting..."
        $extractPath = "$tempDir\$($font.Name)"
        Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force
        
        Write-Host "Copying TTF files..."
        $ttfFiles = Get-ChildItem -Path $extractPath -Include *.ttf -Recurse
        foreach ($ttf in $ttfFiles) {
            Copy-Item $ttf.FullName -Destination $fontsDir -Force
            Write-Host "  -> $($ttf.Name)"
        }
    }
    catch {
        Write-Host "Failed to download $($font.Name): $_" -ForegroundColor Yellow
    }
}

Write-Host "`nCleaning up..."
Remove-Item $tempDir -Recurse -Force

$fontCount = (Get-ChildItem -Path $fontsDir -Filter *.ttf).Count
Write-Host "`nDone! Downloaded $fontCount font files to '$fontsDir' folder." -ForegroundColor Green
Write-Host "`nNext steps:"
Write-Host "1. Download Inno Setup from: https://jrsoftware.org/isdl.php"
Write-Host "2. Open 'installer-with-fonts.iss' in Inno Setup"
Write-Host "3. Click 'Build -> Compile'"
Write-Host "4. Your installer will be in the 'Output' folder"
