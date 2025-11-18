# Asset Generation Script for MicroEngine
# Generates simple PNG sprites for demos using System.Drawing

param(
    [string]$OutputDir = "assets/textures"
)

Add-Type -AssemblyName System.Drawing

$assetsPath = Join-Path $PSScriptRoot ".." $OutputDir
if (-not (Test-Path $assetsPath)) {
    New-Item -ItemType Directory -Path $assetsPath -Force | Out-Null
}

function New-Sprite {
    param(
        [string]$Name,
        [int]$Width,
        [int]$Height,
        [scriptblock]$DrawAction
    )
    
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    
    # Transparent background
    $graphics.Clear([System.Drawing.Color]::Transparent)
    
    # Execute custom drawing
    & $DrawAction $graphics
    
    $outputPath = Join-Path $assetsPath "$Name.png"
    $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    $graphics.Dispose()
    
    Write-Host "Created: $outputPath" -ForegroundColor Green
}

Write-Host "Generating sprite assets..." -ForegroundColor Cyan

# Player sprite (blue square with border)
New-Sprite -Name "player" -Width 32 -Height 32 -DrawAction {
    param($g)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 100, 150, 255))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 2)
    $g.FillRectangle($brush, 2, 2, 28, 28)
    $g.DrawRectangle($pen, 2, 2, 28, 28)
    $brush.Dispose()
    $pen.Dispose()
}

# Enemy sprite (red circle)
New-Sprite -Name "enemy" -Width 32 -Height 32 -DrawAction {
    param($g)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 80, 80))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::DarkRed, 2)
    $g.FillEllipse($brush, 4, 4, 24, 24)
    $g.DrawEllipse($pen, 4, 4, 24, 24)
    $brush.Dispose()
    $pen.Dispose()
}

# Coin sprite (yellow circle with shine)
New-Sprite -Name "coin" -Width 24 -Height 24 -DrawAction {
    param($g)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 215, 0))
    $shineBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 255, 255, 200))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 200, 150, 0), 2)
    $g.FillEllipse($brush, 2, 2, 20, 20)
    $g.DrawEllipse($pen, 2, 2, 20, 20)
    # Shine effect
    $g.FillEllipse($shineBrush, 6, 6, 8, 8)
    $brush.Dispose()
    $shineBrush.Dispose()
    $pen.Dispose()
}

# Bullet sprite (small white oval)
New-Sprite -Name "bullet" -Width 16 -Height 8 -DrawAction {
    param($g)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::LightGray, 1)
    $g.FillEllipse($brush, 1, 1, 14, 6)
    $g.DrawEllipse($pen, 1, 1, 14, 6)
    $brush.Dispose()
    $pen.Dispose()
}

# Star sprite (yellow 5-pointed star)
New-Sprite -Name "star" -Width 32 -Height 32 -DrawAction {
    param($g)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 100))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 255, 200, 0), 2)
    
    # Star points (5-pointed)
    $centerX = 16
    $centerY = 16
    $outerRadius = 14
    $innerRadius = 6
    
    $points = @()
    for ($i = 0; $i -lt 10; $i++) {
        $angle = [Math]::PI / 2 + $i * [Math]::PI / 5
        $radius = if ($i % 2 -eq 0) { $outerRadius } else { $innerRadius }
        $x = $centerX + $radius * [Math]::Cos($angle)
        $y = $centerY - $radius * [Math]::Sin($angle)
        $points += New-Object System.Drawing.PointF($x, $y)
    }
    
    $g.FillPolygon($brush, $points)
    $g.DrawPolygon($pen, $points)
    $brush.Dispose()
    $pen.Dispose()
}

# Box sprite (brown crate)
New-Sprite -Name "box" -Width 32 -Height 32 -DrawAction {
    param($g)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 139, 90, 43))
    $darkBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 100, 60, 30))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 80, 50, 25), 2)
    
    # Main box
    $g.FillRectangle($brush, 2, 2, 28, 28)
    $g.DrawRectangle($pen, 2, 2, 28, 28)
    
    # Planks/lines
    $g.DrawLine($pen, 10, 2, 10, 30)
    $g.DrawLine($pen, 22, 2, 22, 30)
    $g.DrawLine($pen, 2, 10, 30, 10)
    $g.DrawLine($pen, 2, 22, 30, 22)
    
    $brush.Dispose()
    $darkBrush.Dispose()
    $pen.Dispose()
}

# Heart sprite (red heart)
New-Sprite -Name "heart" -Width 32 -Height 32 -DrawAction {
    param($g)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 50, 80))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 200, 0, 40), 2)
    
    # Heart shape using path
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(6, 6, 10, 10, 180, 180)
    $path.AddArc(16, 6, 10, 10, 180, 180)
    $path.AddLine(26, 11, 16, 26)
    $path.AddLine(16, 26, 6, 11)
    $path.CloseFigure()
    
    $g.FillPath($brush, $path)
    $g.DrawPath($pen, $path)
    
    $path.Dispose()
    $brush.Dispose()
    $pen.Dispose()
}

# Background tile (grass texture)
New-Sprite -Name "tile_grass" -Width 32 -Height 32 -DrawAction {
    param($g)
    $brush1 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 100, 180, 80))
    $brush2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 90, 160, 70))
    
    $g.FillRectangle($brush1, 0, 0, 32, 32)
    
    # Grass blades pattern
    $random = New-Object System.Random(42) # Fixed seed for consistency
    for ($i = 0; $i -lt 15; $i++) {
        $x = $random.Next(0, 32)
        $y = $random.Next(0, 32)
        $g.FillRectangle($brush2, $x, $y, 2, 4)
    }
    
    $brush1.Dispose()
    $brush2.Dispose()
}

# Background tile (stone texture)
New-Sprite -Name "tile_stone" -Width 32 -Height 32 -DrawAction {
    param($g)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 140, 140, 140))
    $darkBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 100, 100, 100))
    
    $g.FillRectangle($brush, 0, 0, 32, 32)
    
    # Stone cracks/details
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 80, 80, 80), 1)
    $g.DrawLine($pen, 0, 10, 15, 10)
    $g.DrawLine($pen, 15, 0, 15, 32)
    $g.DrawLine($pen, 20, 20, 32, 22)
    
    $pen.Dispose()
    $brush.Dispose()
    $darkBrush.Dispose()
}

Write-Host "`nAsset generation complete!" -ForegroundColor Green
Write-Host "Generated sprites in: $assetsPath" -ForegroundColor Cyan
