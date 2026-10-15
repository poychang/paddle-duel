#requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

$assetDirectory = Join-Path $PSScriptRoot '..\src\PaddleDuel.App\Assets'
New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null

function New-Image([int]$Width, [int]$Height, [string]$Name, [bool]$Transparent = $false, [bool]$DarkGlyph = $false) {
    $bitmap = [Drawing.Bitmap]::new($Width, $Height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $background = if ($Transparent) { [Drawing.Color]::Transparent } else { [Drawing.Color]::Black }
        $graphics.Clear($background)
        $brush = if ($DarkGlyph) { [Drawing.Brushes]::Black } else { [Drawing.Brushes]::White }
        $size = [Math]::Min($Width, $Height)
        $originX = ($Width - $size) / 2
        $originY = ($Height - $size) / 2
        # Original mark: staggered paddles and an off-centre square ball on a 100-unit canvas.
        foreach ($rectangle in @(
            @(20, 24, 10, 42), @(70, 34, 10, 42), @(45, 43, 10, 10))) {
            $x = [int][Math]::Round($originX + $rectangle[0] * $size / 100)
            $y = [int][Math]::Round($originY + $rectangle[1] * $size / 100)
            $w = [Math]::Max(1, [int][Math]::Round($rectangle[2] * $size / 100))
            $h = [Math]::Max(1, [int][Math]::Round($rectangle[3] * $size / 100))
            $graphics.FillRectangle($brush, $x, $y, $w, $h)
        }
        $bitmap.Save((Join-Path $assetDirectory $Name), [Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

foreach ($scale in @(100, 125, 150, 200, 400)) {
    foreach ($logo in @(
        @{ Name = 'StoreLogo'; Width = 50; Height = 50 },
        @{ Name = 'Square44x44Logo'; Width = 44; Height = 44 },
        @{ Name = 'Square150x150Logo'; Width = 150; Height = 150 },
        @{ Name = 'SplashScreen'; Width = 620; Height = 300 })) {
        $width = [int][Math]::Ceiling($logo.Width * $scale / 100)
        $height = [int][Math]::Ceiling($logo.Height * $scale / 100)
        New-Image $width $height "$($logo.Name).scale-$scale.png"
    }
}
foreach ($size in @(16, 24, 32, 48, 256)) {
    New-Image $size $size "Square44x44Logo.targetsize-$size.png"
    New-Image $size $size "Square44x44Logo.targetsize-$size`_altform-unplated.png" $true
    New-Image $size $size "Square44x44Logo.targetsize-$size`_altform-lightunplated.png" $true $true
}

$iconSizes = @(16, 24, 32, 48, 256)
$images = @($iconSizes | ForEach-Object {
    ,([IO.File]::ReadAllBytes((Join-Path $assetDirectory "Square44x44Logo.targetsize-$_.png")))
})
$writer = [IO.BinaryWriter]::new([IO.File]::Create((Join-Path $assetDirectory 'PaddleDuel.ico')))
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$iconSizes.Count)
    $offset = 6 + 16 * $iconSizes.Count
    for ($index = 0; $index -lt $iconSizes.Count; $index++) {
        $dimension = if ($iconSizes[$index] -eq 256) { 0 } else { $iconSizes[$index] }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $images[$index].Length
    }
    foreach ($image in $images) { $writer.Write([byte[]]$image) }
} finally {
    $writer.Dispose()
}
Write-Output 'Generated original Paddle Duel scale-qualified PNGs and multi-size Windows icon.'
