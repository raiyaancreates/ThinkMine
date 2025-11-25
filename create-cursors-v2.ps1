Add-Type -AssemblyName System.Drawing

function Create-Cursor {
    param (
        [string]$InputPath,
        [string]$OutputPath,
        [int]$HotspotX = 0,
        [int]$HotspotY = 0
    )

    if (-not (Test-Path $InputPath)) {
        Write-Error "Input file not found: $InputPath"
        return
    }

    # Load input image
    $srcImage = [System.Drawing.Image]::FromFile($InputPath)
    
    # Target size
    $cursorSize = 32
    
    # Create transparent canvas
    $bitmap = New-Object System.Drawing.Bitmap $cursorSize, $cursorSize
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Calculate Aspect Ratio
    $ratioX = $cursorSize / $srcImage.Width
    $ratioY = $cursorSize / $srcImage.Height
    $ratio = [Math]::Min($ratioX, $ratioY)

    $newWidth = [int]($srcImage.Width * $ratio)
    $newHeight = [int]($srcImage.Height * $ratio)

    # Draw image (Top-Left aligned to ensure hotspot at 0,0 is valid for pointers)
    # For cursors, usually we want the "active" point at 0,0.
    # If the image is a big arrow, shrinking it and putting it at 0,0 is correct.
    $graphics.DrawImage($srcImage, 0, 0, $newWidth, $newHeight)
    
    $graphics.Dispose()
    $srcImage.Dispose()

    # Save as PNG to memory
    $ms = New-Object System.IO.MemoryStream
    $bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $ms.ToArray()
    $bitmap.Dispose()

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
Create-Cursor -InputPath $arrowInput -OutputPath "cursor_arrow.cur" -HotspotX 0 -HotspotY 0
Create-Cursor -InputPath $handInput -OutputPath "cursor_hand.cur" -HotspotX 0 -HotspotY 0
