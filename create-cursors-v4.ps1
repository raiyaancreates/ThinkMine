
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

    # 2. Resize to fit 32x32 (Standard Cursor)
    # Maintain Aspect Ratio
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
    
    # Draw at 0,0 (Top-Left) to ensure hotspot is at the tip
    $g.DrawImage($croppedImage, 0, 0, $newWidth, $newHeight)
    $g.Dispose()

    # 3. Save as .cur (Icon format with custom header for hotspot)
    # .cur format is identical to .ico but with a different type in header (2 instead of 1)
    # and hotspot coordinates.
    
    # We will use a temporary .ico file and then patch the header
    $tempIco = $OutputPath + ".temp.ico"
    
    # Convert Bitmap to Icon (this is tricky in pure PS without losing quality, 
    # but let's try saving as PNG then converting via header manipulation)
    
    # Save as PNG to memory stream
    $ms = New-Object System.IO.MemoryStream
    $finalImage.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $ms.ToArray()
    $ms.Dispose()

    # Construct CUR file manually
    $fs = [System.IO.File]::Create($OutputPath)
    $bw = New-Object System.IO.BinaryWriter($fs)

    # ICONDIR
    $bw.Write([int16]0)      # Reserved
    $bw.Write([int16]2)      # Type (2 = Cursor)
    $bw.Write([int16]1)      # Count (1 image)

    # ICONDIRENTRY
    $bw.Write([byte]$targetSize) # Width
    $bw.Write([byte]$targetSize) # Height
    $bw.Write([byte]0)       # ColorCount
    $bw.Write([byte]0)       # Reserved
    
    # Hotspot X, Y
    # For Arrow: 0, 0
    # For Hand: 0, 0 (Assuming index finger is top-left after crop)
    # Actually, for hand, if it's a pointing hand, the tip is usually top-left or top-center.
    # Given "crop to edges", the highest pixel is at Y=0. The leftmost pixel is at X=0.
    # If the hand is pointing up-left, (0,0) is correct.
    # If it's pointing straight up, X might need to be centered.
    # Let's assume (0,0) for now as it's safest for "accurate clicks" on the tip.
    
    $hotspotX = 0
    $hotspotY = 0
    
    # If Hand, maybe adjust? User said "accurate clicks". 
    # Usually pointing hand tip is the hotspot.
    # If I crop to edges, the tip is definitely at Y=0.
    # Is it at X=0? Maybe not.
    # Let's check the first row of pixels in the cropped image to find the tip X.
    if ($Type -eq "Hand") {
        # Find the first non-transparent pixel in the first row (Y=0) of the FINAL image
        for ($x = 0; $x -lt $newWidth; $x++) {
            if ($finalImage.GetPixel($x, 0).A -gt 0) {
                $hotspotX = $x
                break
            }
        }
        # If no pixel at Y=0 (unlikely due to crop), search Y=1...
    }

    $bw.Write([int16]$hotspotX) 
    $bw.Write([int16]$hotspotY)

    $bw.Write([int]$pngBytes.Length) # SizeInBytes
    $bw.Write([int]22)       # ImageOffset (6 + 16 = 22)

    # Image Data (PNG)
    $bw.Write($pngBytes)

    $bw.Close()
    $fs.Close()

    Write-Host "Created Cursor: $OutputPath (Hotspot: $hotspotX, $hotspotY)" -ForegroundColor Green
    
    $sourceImage.Dispose()
    $croppedImage.Dispose()
    $finalImage.Dispose()
}

# Paths
$arrowInput = "C:\Users\DELL\.gemini\antigravity\brain\d8d95c93-016d-4757-9f9b-461755b9ad16\uploaded_image_0_1764101760307.png"
$handInput = "C:\Users\DELL\.gemini\antigravity\brain\d8d95c93-016d-4757-9f9b-461755b9ad16\uploaded_image_1_1764101760307.png"

$outputDir = "c:\Users\DELL\.gemini\antigravity\playground\triple-observatory\ThinkMine"
$arrowOutput = "$outputDir\cursor_arrow.cur"
$handOutput = "$outputDir\cursor_hand.cur"

Create-Cursor -InputPath $arrowInput -OutputPath $arrowOutput -Type "Arrow"
Create-Cursor -InputPath $handInput -OutputPath $handOutput -Type "Hand"
