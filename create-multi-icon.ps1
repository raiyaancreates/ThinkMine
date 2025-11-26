Add-Type -AssemblyName System.Drawing

$sourcePath = "new_logo_v2.png"
$iconPath = "app.ico"

# Helper to resize image
function Resize-Image {
    param($img, $size)
    $bmp = new-object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($img, 0, 0, $size, $size)
    $g.Dispose()
    return $bmp
}

# Load Source
$srcImage = [System.Drawing.Image]::FromFile($sourcePath)

# Sizes to include
$sizes = @(16, 32, 48, 64, 256)
$images = @()

foreach ($s in $sizes) {
    $images += Resize-Image $srcImage $s
}

# Create ICO File Stream
$fs = [System.IO.File]::Create($iconPath)
$bw = New-Object System.IO.BinaryWriter $fs

# Write ICO Header
# Reserved (2 bytes), Type (2 bytes, 1=ICO), Count (2 bytes)
$bw.Write([int16]0)
$bw.Write([int16]1)
$bw.Write([int16]$images.Count)

$offset = 6 + (16 * $images.Count)

foreach ($img in $images) {
    # Calculate size
    $ms = New-Object System.IO.MemoryStream
    $img.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $data = $ms.ToArray()
    $size = $data.Length
    
    # Width (1 byte)
    $w = if ($img.Width -eq 256) { 0 } else { $img.Width }
    $bw.Write([byte]$w)
    # Height (1 byte)
    $h = if ($img.Height -eq 256) { 0 } else { $img.Height }
    $bw.Write([byte]$h)
    # Color Count (1 byte, 0 for >=8bpp)
    $bw.Write([byte]0)
    # Reserved (1 byte)
    $bw.Write([byte]0)
    # Planes (2 bytes, 1) or Color Planes
    $bw.Write([int16]1)
    # BitCount (2 bytes, 32)
    $bw.Write([int16]32)
    # SizeInBytes (4 bytes)
    $bw.Write([int32]$size)
    # FileOffset (4 bytes)
    $bw.Write([int32]$offset)
    
    $offset += $size
    $ms.Dispose()
}

# Write Image Data
foreach ($img in $images) {
    $ms = New-Object System.IO.MemoryStream
    $img.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $data = $ms.ToArray()
    $bw.Write($data)
    $ms.Dispose()
    $img.Dispose()
}

$bw.Close()
$fs.Close()
$srcImage.Dispose()

Write-Host "Created multi-size app.ico"
