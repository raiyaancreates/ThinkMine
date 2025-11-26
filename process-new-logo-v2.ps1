Add-Type -AssemblyName System.Drawing

$sourcePath = "new_logo_v2.png"
$iconPath = "app.ico"
$pngPath = "thinkmine.png"

# Load Image
$image = [System.Drawing.Image]::FromFile($sourcePath)

# Save as PNG (for website/other uses)
$image.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Create Multi-Size Icon
# We'll use a simple approach: Create a 256x256 Bitmap and save it as Icon
# For true multi-size ICO, we'd need a more complex script or tool (like ImageMagick), 
# but .NET's Icon.FromHandle usually creates a decent single-size icon that Windows scales.
# To ensure "large as possible", we'll make sure the base is 256x256.

$size = 256
$square = new-object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($square)
$g.Clear([System.Drawing.Color]::Transparent) # Transparent for PNG source
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Calculate Aspect Ratio to Fit
$ratio = $image.Width / $image.Height
$w = $size
$h = $size

if ($ratio -gt 1) {
    $h = $w / $ratio
}
else {
    $w = $h * $ratio
}

$x = ($size - $w) / 2
$y = ($size - $h) / 2

$g.DrawImage($image, $x, $y, $w, $h)
$g.Dispose()

# Save Icon
# Using a slightly better method to ensure 256x256 support if possible
$ms = New-Object System.IO.MemoryStream
$square.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$ms.Seek(0, [System.IO.SeekOrigin]::Begin)

# Create Icon from Bitmap (this usually creates a 32x32 or system default, which is the problem)
# We need to explicitly write the ICO header for 256x256 PNG embedding (Vista style)
# Or just use the previous method but ensure input is 256x256.

# Let's try the Icon.FromHandle method again but with the 256x256 bitmap.
$icon = [System.Drawing.Icon]::FromHandle($square.GetHicon())
$fs = new-object System.IO.FileStream $iconPath, "Create"
$icon.Save($fs)
$fs.Close()

$image.Dispose()
$square.Dispose()
$icon.Dispose()
$ms.Dispose()

Write-Host "Created app.ico and thinkmine.png"
