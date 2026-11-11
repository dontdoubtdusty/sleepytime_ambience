# Draws the app icon (crescent moon and stars on a dusky night tile with an ember glow)
# and writes icon.ico (exe, shortcut, taskbar) and Assets\icon.png (window icon).
# Run from the repo root: powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent

# Draws the icon once at 1024 x 1024; smaller sizes are scaled down from this.
function New-Master {
    $N = 1024
    $bmp = New-Object System.Drawing.Bitmap $N, $N, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded tile, indigo at the top fading to plum.
    $tile = New-Object System.Drawing.Drawing2D.GraphicsPath
    $m = 32; $r = 220; $w = $N - 2 * $m
    $tile.AddArc($m, $m, $r, $r, 180, 90); $tile.AddArc($m + $w - $r, $m, $r, $r, 270, 90)
    $tile.AddArc($m + $w - $r, $m + $w - $r, $r, $r, 0, 90); $tile.AddArc($m, $m + $w - $r, $r, $r, 90, 90)
    $tile.CloseFigure()
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, $m), (New-Object System.Drawing.Point 0, ($N - $m)),
        ([System.Drawing.Color]::FromArgb(255, 34, 30, 74)), ([System.Drawing.Color]::FromArgb(255, 84, 40, 62))
    $g.FillPath($bg, $tile)
    $g.SetClip($tile)

    # Ember glow rising from the bottom edge.
    $ember = New-Object System.Drawing.Drawing2D.GraphicsPath
    $ember.AddEllipse(112, 700, 800, 560)
    $eb = New-Object System.Drawing.Drawing2D.PathGradientBrush $ember
    $eb.CenterColor = [System.Drawing.Color]::FromArgb(170, 255, 140, 60)
    $eb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 140, 60))
    $g.FillPath($eb, $ember)

    # Soft halo behind the moon.
    $halo = New-Object System.Drawing.Drawing2D.GraphicsPath
    $halo.AddEllipse(150, 130, 620, 620)
    $hb = New-Object System.Drawing.Drawing2D.PathGradientBrush $halo
    $hb.CenterColor = [System.Drawing.Color]::FromArgb(90, 255, 220, 150)
    $hb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 220, 150))
    $g.FillPath($hb, $halo)

    # Crescent: a moon disc with a second disc cut out of its upper right.
    $disc = New-Object System.Drawing.Drawing2D.GraphicsPath
    $disc.AddEllipse(210, 190, 500, 500)
    $moon = New-Object System.Drawing.Region $disc
    $cut = New-Object System.Drawing.Drawing2D.GraphicsPath
    $cut.AddEllipse(370, 110, 450, 450)
    $moon.Exclude($cut)
    $mb = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 210, 190), (New-Object System.Drawing.Point 600, 690),
        ([System.Drawing.Color]::FromArgb(255, 255, 236, 176)), ([System.Drawing.Color]::FromArgb(255, 246, 182, 92))
    $g.FillRegion($mb, $moon)

    # Stars: small four-point sparkles in the moon's opening and the sky.
    $star = [System.Drawing.Color]::FromArgb(255, 255, 238, 196)
    foreach ($s in @(@(700, 330, 52), @(230, 160, 34), @(810, 560, 28), @(590, 205, 20))) {
        $x = $s[0]; $y = $s[1]; $a = $s[2]; $b = $a * 0.28
        $pts = @(
            (New-Object System.Drawing.PointF $x, ($y - $a)), (New-Object System.Drawing.PointF ($x + $b), ($y - $b)),
            (New-Object System.Drawing.PointF ($x + $a), $y), (New-Object System.Drawing.PointF ($x + $b), ($y + $b)),
            (New-Object System.Drawing.PointF $x, ($y + $a)), (New-Object System.Drawing.PointF ($x - $b), ($y + $b)),
            (New-Object System.Drawing.PointF ($x - $a), $y), (New-Object System.Drawing.PointF ($x - $b), ($y - $b)))
        $g.FillPolygon((New-Object System.Drawing.SolidBrush $star), [System.Drawing.PointF[]]$pts)
    }
    $g.Dispose()
    return $bmp
}

# Scales the master down to one square size and returns it as PNG bytes.
function Get-Png($master, $size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'; $g.PixelOffsetMode = 'HighQuality'; $g.CompositingQuality = 'HighQuality'
    $g.DrawImage($master, 0, 0, $size, $size)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return , $ms.ToArray()
}

$master = New-Master
$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = foreach ($s in $sizes) { , (Get-Png $master $s) }

# Window icon for raylib.
[System.IO.File]::WriteAllBytes((Join-Path $root "Assets\icon.png"), $pngs[-1])

# .ico file: a header, one directory entry per size, then each size as PNG data.
$ms = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $ms
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $dim = if ($s -ge 256) { 0 } else { $s }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$pngs[$i].Length); $w.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $w.Write([byte[]]$p) }
$w.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $root "icon.ico"), $ms.ToArray())
$master.Dispose()
"Wrote icon.ico ($($sizes -join ', ') px) and Assets\icon.png"
