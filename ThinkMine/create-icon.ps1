Add-Type -AssemblyName System.Drawing

# Load the source image
$sourceImage = [System.Drawing.Image]::FromFile("new_logo.png")

# Create icon with multiple sizes
$sizes = @(256, 128, 64, 48, 32, 16)
$iconStream = New-Object System.IO.MemoryStream

# Icon header
$iconStream.WriteByte(0)  # Reserved
$iconStream.WriteByte(0)
$iconStream.WriteByte(1)  # Type: Icon
$iconStream.WriteByte(0)
$iconStream.WriteByte($sizes.Count)  # Number of images
$iconStream.WriteByte(0)

$imageDataStreams = @()
$offset = 6 + ($sizes.Count * 16)  # Header + directory entries

foreach ($size in $sizes) {
    # Create bitmap with transparency
    $resized = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    
    # Fill with transparent background
    $graphics = [System.Drawing.Graphics]::FromImage($resized)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $graphics.DrawImage($sourceImage, 0, 0, $size, $size)
    $graphics.Dispose()
    
    # Save to PNG stream (preserves transparency)
    $pngStream = New-Object System.IO.MemoryStream
    $resized.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngData = $pngStream.ToArray()
    $pngStream.Dispose()
    $resized.Dispose()
    
    # Write directory entry
    $iconStream.WriteByte([Math]::Min($size, 255))  # Width
    $iconStream.WriteByte([Math]::Min($size, 255))  # Height
    $iconStream.WriteByte(0)  # Color palette
    $iconStream.WriteByte(0)  # Reserved
    $iconStream.WriteByte(1)  # Color planes
    $iconStream.WriteByte(0)
    $iconStream.WriteByte(32) # Bits per pixel (32 for transparency)
    $iconStream.WriteByte(0)
    
    # Size of image data
    $sizeBytes = [BitConverter]::GetBytes([int]$pngData.Length)
    $iconStream.Write($sizeBytes, 0, 4)
    
    # Offset to image data
    $offsetBytes = [BitConverter]::GetBytes([int]$offset)
    $iconStream.Write($offsetBytes, 0, 4)
    
    $imageDataStreams += $pngData
    $offset += $pngData.Length
}

# Write all image data
foreach ($data in $imageDataStreams) {
    $iconStream.Write($data, 0, $data.Length)
}

# Save icon file
$iconBytes = $iconStream.ToArray()
[System.IO.File]::WriteAllBytes("app.ico", $iconBytes)

$iconStream.Dispose()
$sourceImage.Dispose()

Write-Host "Icon with transparency created successfully"
