Add-Type -AssemblyName System.Drawing

function Create-Cursor {
    param (
        [string]$InputPath,
        [string]$OutputPath,
        [int]$HotspotX = 0,
        [int]$HotspotY = 0,
        [bool]$AutoCrop = $true
    )

    if (-not (Test-Path $InputPath)) {
        Write-Error "Input file not found: $InputPath"
        return
    }

    # Load input image
    $srcImage = [System.Drawing.Image]::FromFile($InputPath)
    $bitmap = New-Object System.Drawing.Bitmap $srcImage

    $cropRect = New-Object System.Drawing.Rectangle(0, 0, $bitmap.Width, $bitmap.Height)

    if ($AutoCrop) {
        $minX = $bitmap.Width
        $minY = $bitmap.Height
        $maxX = 0
        $maxY = 0
        $foundPixel = $false

        for ($x = 0; $x -lt $bitmap.Width; $x++) {
            for ($y = 0; $y -lt $bitmap.Height; $y++) {
                $pixel = $bitmap.GetPixel($x, $y)
                if ($pixel.A -gt 0) {
                    if ($x -lt $minX) { $minX = $x }
                    if ($x -gt $maxX) { $maxX = $x }
                    if ($y -lt $minY) { $minY = $y }
                    if ($y -gt $maxY) { $maxY = $y }
                    $foundPixel = $true
                }
            }
        }

        if ($foundPixel) {
            $width = $maxX - $minX + 1
            $height = $maxY - $minY + 1
            $cropRect = New-Object System.Drawing.Rectangle($minX, $minY, $width, $height)
            Write-Host "Auto-cropped to $width x $height"
        }
    }

    # Target size
    $cursorSize = 32
    
    # Create transparent canvas
    $finalBitmap = New-Object System.Drawing.Bitmap $cursorSize, $cursorSize
    $graphics = [System.Drawing.Graphics]::FromImage($finalBitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Calculate Aspect Ratio for the cropped area
    $ratioX = $cursorSize / $cropRect.Width
    $ratioY = $cursorSize / $cropRect.Height
    $ratio = [Math]::Min($ratioX, $ratioY)

    $newWidth = [int]($cropRect.Width * $ratio)
    $newHeight = [int]($cropRect.Height * $ratio)

    # Draw cropped image scaled to fit
    $destRect = New-Object System.Drawing.Rectangle(0, 0, $newWidth, $newHeight)
    $graphics.DrawImage($bitmap, $destRect, $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
    
    $graphics.Dispose()
    $bitmap.Dispose()
    $srcImage.Dispose()

    # Save as PNG to memory
    $ms = New-Object System.IO.MemoryStream
    $finalBitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $ms.ToArray()
    $finalBitmap.Dispose()

    # Write CUR file
    $fs = [System.IO.File]::Create($OutputPath)
    $bw = New-Object System.IO.BinaryWriter $fs

    # CUR Header
    $bw.Write([int16]0) # Reserved
    $bw.Write([int16]2) # Type (2=CUR)
    $bw.Write([int16]1) # Count (1 image)

    # Image Entry
    $bw.Write([byte]$cursorSize) # Width
    $bw.Write([byte]$cursorSize) # Height
    $bw.Write([byte]0)           # Color Count
    $bw.Write([byte]0)           # Reserved
    
    # Adjust hotspot based on scaling if needed, but for now keep it simple (0,0 for arrow)
    # For hand, we might want to adjust.
    $bw.Write([int16]$HotspotX)  # Hotspot X
    $bw.Write([int16]$HotspotY)  # Hotspot Y
    
    $bw.Write([int]$pngBytes.Length) # Size
    $bw.Write([int]22)           # Offset (6+16)

    # Image Data
    $bw.Write($pngBytes)

    $bw.Close()
    $fs.Close()

    Write-Host "Cursor created at $OutputPath"
}

# Input Paths (User Uploaded Images)
$arrowInput = "C:/Users/DELL/.gemini/antigravity/brain/d8d95c93-016d-4757-9f9b-461755b9ad16/uploaded_image_0_1764077501828.png"
$handInput = "C:/Users/DELL/.gemini/antigravity/brain/d8d95c93-016d-4757-9f9b-461755b9ad16/uploaded_image_1_1764077501828.png"

# Generate Cursors
# Arrow: Hotspot at 0,0 (Top Left)
Create-Cursor -InputPath $arrowInput -OutputPath "cursor_arrow.cur" -HotspotX 0 -HotspotY 0

# Hand: Hotspot needs to be the pointer finger tip. 
# Assuming the hand points up-left or up, 0,0 might be okay if cropped tight to the finger. 
# If it's a standard hand pointer, the tip is usually around (10, 0) relative to a 32x32 box if centered, 
# but with auto-crop to top-left, (5,0) or (0,0) might be safer. Let's try (10, 0) as a safe bet for a hand cursor.
Create-Cursor -InputPath $handInput -OutputPath "cursor_hand.cur" -HotspotX 10 -HotspotY 0
