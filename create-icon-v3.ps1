
Add-Type -AssemblyName System.Drawing

$inputPath = "c:\Users\DELL\.gemini\antigravity\playground\triple-observatory\ThinkMine\new_logo.png"
$outputPath = "c:\Users\DELL\.gemini\antigravity\playground\triple-observatory\ThinkMine\app.ico"

if (-not (Test-Path $inputPath)) {
    Write-Host "Input file not found: $inputPath" -ForegroundColor Red
    exit
}

# Create 256x256 canvas with White background
$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::White)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

# Load source image
$source = [System.Drawing.Bitmap]::FromFile($inputPath)

# Calculate scaling to fit within 256x256 with some padding (e.g. 20px)
$padding = 20
$maxDim = $size - ($padding * 2)
$ratioX = $maxDim / $source.Width
$ratioY = $maxDim / $source.Height
$ratio = [Math]::Min($ratioX, $ratioY)

$newW = [int]($source.Width * $ratio)
$newH = [int]($source.Height * $ratio)

$x = [int](($size - $newW) / 2)
$y = [int](($size - $newH) / 2)

$g.DrawImage($source, $x, $y, $newW, $newH)
$g.Dispose()

# Save as Icon
$fs = [System.IO.File]::Create($outputPath)
# ICO Header
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([int16]0) # Reserved
$bw.Write([int16]1) # Type (1=Icon)
$bw.Write([int16]1) # Count

# Entry
$bw.Write([byte]0) # Width (0=256)
$bw.Write([byte]0) # Height (0=256)
$bw.Write([byte]0) # ColorCount
$bw.Write([byte]0) # Reserved
$bw.Write([int16]1) # Planes
$bw.Write([int16]32) # BitCount
$bw.Write([int]0) # SizeInBytes (Placeholder)
$bw.Write([int]22) # Offset

# PNG Data
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $ms.ToArray()
$sizeBytes = $pngBytes.Length

# Update Size
$bw.Seek(14, [System.IO.SeekOrigin]::Begin)
$bw.Write([int]$sizeBytes)

# Write Data
$bw.Seek(22, [System.IO.SeekOrigin]::Begin)
$bw.Write($pngBytes)

$bw.Close()
$fs.Close()
$source.Dispose()
$bmp.Dispose()
$ms.Dispose()

Write-Host "Created app.ico with White Background at $outputPath" -ForegroundColor Green
