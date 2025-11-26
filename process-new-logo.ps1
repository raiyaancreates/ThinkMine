Add-Type -AssemblyName System.Drawing

$sourcePath = "new_logo.jpg"
$iconPath = "app.ico"
$pngPath = "thinkmine.png"

# Load Image
$image = [System.Drawing.Image]::FromFile($sourcePath)

# Create Square Bitmap (White Background)
$size = 256
$square = new-object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($square)
$g.Clear([System.Drawing.Color]::White)
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

# Save as PNG
$square.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Save as ICO (Simple method)
# For better quality, we might need a library, but this works for basic usage
# Actually, System.Drawing.Icon.Save doesn't support high quality from Bitmap directly easily without multi-size
# Let's use a temporary file hack or just save as PNG and rename if needed, but Windows needs real ICO structure.
# We will use the previous method of creating an Icon from a handle.

$thumb = $square.GetThumbnailImage(256, 256, $null, [IntPtr]::Zero)
$icon = [System.Drawing.Icon]::FromHandle($((new-object System.Drawing.Bitmap $thumb).GetHicon()))

$fs = new-object System.IO.FileStream $iconPath, "Create"
$icon.Save($fs)
$fs.Close()

$image.Dispose()
$square.Dispose()
$icon.Dispose()

Write-Host "Created app.ico and thinkmine.png"
