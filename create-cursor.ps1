Add-Type -AssemblyName System.Drawing

function Create-Cursor {
    param (
        [string]$InputPath,
        [string]$OutputPath
    )

    if (-not (Test-Path $InputPath)) {
        Write-Error "Input file not found: $InputPath"
        return
    }

    # Load input image
    $srcImage = [System.Drawing.Image]::FromFile($InputPath)
    
    # Resize to 32x32 (standard cursor size)
    $cursorSize = 32
    $bitmap = New-Object System.Drawing.Bitmap $cursorSize, $cursorSize
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    
    $graphics.DrawImage($srcImage, 0, 0, $cursorSize, $cursorSize)
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
    $bw.Write([int16]0)          # Hotspot X (0 = Left)
    $bw.Write([int16]0)          # Hotspot Y (0 = Top)
    $bw.Write([int]$pngBytes.Length) # Size
    $bw.Write([int]22)           # Offset (6+16)

    # Image Data
    $bw.Write($pngBytes)

    $bw.Close()
    $fs.Close()

    Write-Host "Cursor created at $OutputPath"
}

$inputPng = "C:/Users/DELL/.gemini/antigravity/brain/d8d95c93-016d-4757-9f9b-461755b9ad16/uploaded_image_1764073241160.png"
Create-Cursor -InputPath $inputPng -OutputPath "custom_cursor.cur"
