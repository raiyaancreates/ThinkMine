
Add-Type -AssemblyName System.Drawing

function Create-Cursor {
    param (
        [string]$InputPath,
        [string]$OutputPath,
        [string]$Type # "Arrow" or "Hand"
    )

    if (-not (Test-Path $InputPath)) {
        Write-Host "Error: Input file not found: $InputPath" -ForegroundColor Red
        return
    }

    $sourceImage = [System.Drawing.Bitmap]::FromFile($InputPath)
    
    # 1. Auto-Crop
    $minX = $sourceImage.Width
    $minY = $sourceImage.Height
    $maxX = 0
    $maxY = 0
    $hasPixels = $false

    for ($x = 0; $x -lt $sourceImage.Width; $x++) {
        for ($y = 0; $y -lt $sourceImage.Height; $y++) {
            $pixel = $sourceImage.GetPixel($x, $y)
            if ($pixel.A -gt 0) {
                # Non-transparent
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
                $hasPixels = $true
            }
        }
    }

    if (-not $hasPixels) {
        Write-Host "Error: Image is completely transparent." -ForegroundColor Red
        return
    }

    $cropWidth = $maxX - $minX + 1
    $cropHeight = $maxY - $minY + 1
    $cropRect = New-Object System.Drawing.Rectangle($minX, $minY, $cropWidth, $cropHeight)
    $croppedImage = $sourceImage.Clone($cropRect, $sourceImage.PixelFormat)

    # 2. Resize to fit 32x32
    $targetSize = 32
    $ratioX = $targetSize / $croppedImage.Width
    $ratioY = $targetSize / $croppedImage.Height
    $ratio = [Math]::Min($ratioX, $ratioY)
    
    $newWidth = [int]($croppedImage.Width * $ratio)
    $newHeight = [int]($croppedImage.Height * $ratio)
    
    $finalImage = New-Object System.Drawing.Bitmap($targetSize, $targetSize)
    $g = [System.Drawing.Graphics]::FromImage($finalImage)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    
    # Draw at 0,0 (Top-Left)
    $g.DrawImage($croppedImage, 0, 0, $newWidth, $newHeight)
    $g.Dispose()

    # 3. Create Icon and Patch to Cursor
    $hIcon = $finalImage.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    
    $fs = [System.IO.File]::OpenWrite($OutputPath)
    $icon.Save($fs)
    $fs.Close()
    
    [System.Runtime.InteropServices.Marshal]::DestroyIcon($hIcon) | Out-Null
    
    # Patch Header (ICO -> CUR)
    $bytes = [System.IO.File]::ReadAllBytes($OutputPath)
    $bytes[2] = 2 # Type = Cursor (was 1 for Icon)
    
    # Hotspot
    $hotspotX = 0
    $hotspotY = 0
    
    if ($Type -eq "Hand") {
        # Find tip X at Y=0
        for ($x = 0; $x -lt $newWidth; $x++) {
            if ($finalImage.GetPixel($x, 0).A -gt 0) {
                $hotspotX = $x
                break
            }
        }
    }

    # Offset 10 = HotspotX (2 bytes)
    $bytes[10] = [byte]($hotspotX -band 0xFF)
    $bytes[11] = [byte](($hotspotX -shr 8) -band 0xFF)
    
    # Offset 12 = HotspotY (2 bytes)
    $bytes[12] = [byte]($hotspotY -band 0xFF)
    $bytes[13] = [byte](($hotspotY -shr 8) -band 0xFF)
    
    [System.IO.File]::WriteAllBytes($OutputPath, $bytes)

    Write-Host "Created Cursor: $OutputPath (Hotspot: $hotspotX, $hotspotY)" -ForegroundColor Green
    
    $sourceImage.Dispose()
    $croppedImage.Dispose()
    $finalImage.Dispose()
    $icon.Dispose()
}

# Paths
$arrowInput = "C:\Users\DELL\.gemini\antigravity\brain\d8d95c93-016d-4757-9f9b-461755b9ad16\uploaded_image_0_1764101760307.png"
$handInput = "C:\Users\DELL\.gemini\antigravity\brain\d8d95c93-016d-4757-9f9b-461755b9ad16\uploaded_image_0_1764103990547.png"

$outputDir = "c:\Users\DELL\.gemini\antigravity\playground\triple-observatory\ThinkMine"
$arrowOutput = "$outputDir\cursor_arrow.cur"
$handOutput = "$outputDir\cursor_hand.cur"

Create-Cursor -InputPath $arrowInput -OutputPath $arrowOutput -Type "Arrow"
Create-Cursor -InputPath $handInput -OutputPath $handOutput -Type "Hand"
