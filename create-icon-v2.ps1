Add-Type -AssemblyName System.Drawing

function Create-Icon {
    param (
        [string]$OutputPath
    )

    $sizes = @(16, 32, 48, 64, 128, 256)
    $bitmaps = @()

    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap $size, $size
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

        # White Background (Rounded Rectangle)
        $rect = New-Object System.Drawing.Rectangle 0, 0, $size, $size
        # Slightly smaller to avoid edge clipping
        $drawRect = New-Object System.Drawing.Rectangle 1, 1, ($size - 2), ($size - 2)
        
        $brush = [System.Drawing.Brushes]::White
        $graphics.FillEllipse($brush, $drawRect) # Circle for a cleaner modern look

        # Text "TM"
        $fontSize = $size * 0.45
        $fontFamily = New-Object System.Drawing.FontFamily "Segoe UI"
        # FontStyle.Bold = 1, GraphicsUnit.Pixel = 2
        $font = New-Object System.Drawing.Font $fontFamily, $fontSize, 1, 2
        $textBrush = [System.Drawing.Brushes]::Black
        
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center

        # Adjust vertical position slightly for visual centering
        $textRect = New-Object System.Drawing.RectangleF 0, ($size * 0.05), $size, $size

        $graphics.DrawString("TM", $font, $textBrush, $textRect, $format)

        $bitmaps += $bitmap
        $graphics.Dispose()
    }

    # Save as ICO (Multi-size)
    # This is a simplified ICO saver. For true multi-icon support, we'd need a more complex binary writer.
    # For now, let's save the largest one as a PNG and convert, or just use a single high-res bitmap for the ICO if possible.
    # Actually, .NET's Icon.FromHandle doesn't support high-color well.
    # Let's use a simpler approach: Save the 256x256 PNG and let the user know, or try a basic ICO header write.
    
    # Better approach for this environment: Save the 256x256 image as PNG, then convert to ICO using a simple header writer.
    
    $largest = $bitmaps[-1]
    $pngPath = $OutputPath.Replace(".ico", ".png")
    $largest.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Simple ICO writer for the single largest image (Windows often scales down fine)
    $ms = New-Object System.IO.MemoryStream
    $largest.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $ms.ToArray()
    
    $fs = [System.IO.File]::Create($OutputPath)
    $bw = New-Object System.IO.BinaryWriter $fs
    
    # ICO Header
    $bw.Write([int16]0) # Reserved
    $bw.Write([int16]1) # Type (1=ICO)
    $bw.Write([int16]1) # Count (1 image)
    
    # Image Entry
    $bw.Write([byte]0)   # Width (0 = 256)
    $bw.Write([byte]0)   # Height (0 = 256)
    $bw.Write([byte]0)   # Colors
    $bw.Write([byte]0)   # Reserved
    $bw.Write([int16]1)  # Planes
    $bw.Write([int16]32) # BPP
    $bw.Write([int]$pngBytes.Length) # Size
    $bw.Write([int]22)   # Offset (6+16)
    
    # Image Data
    $bw.Write($pngBytes)
    
    $bw.Close()
    $fs.Close()
    
    Write-Host "Icon created at $OutputPath"
}

Create-Icon -OutputPath "thinkmine.ico"
