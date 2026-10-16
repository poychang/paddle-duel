#requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

$browser = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $browser) { throw 'Google Chrome is required to render the SVG artwork.' }

$source = Join-Path $PSScriptRoot '..\sites\favicon.svg'
$output = Join-Path $PSScriptRoot '..\docs\store'
$svg = Get-Content -LiteralPath $source -Raw -Encoding UTF8
[xml]$document = $svg
$background = $document.svg.rect.GetAttribute('fill')
if ($document.svg.GetAttribute('viewBox') -ne '0 0 64 64' -or $background -notmatch '^#[0-9a-fA-F]{6}$') {
    throw 'The favicon geometry or background changed; review the Store layout before regenerating.'
}
$work = Join-Path ([IO.Path]::GetTempPath()) "PaddleDuel-StoreArt-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $work | Out-Null
try {
    foreach ($art in @(
        @{ Name = 'poster-1440x2160.png'; Width = 1440; Height = 2160 },
        @{ Name = 'box-2160x2160.png'; Width = 2160; Height = 2160 }
    )) {
        # Keep the original square mark above Store text overlays in the bottom third.
        $size = [int][Math]::Min($art.Width, $art.Height * 2 / 3)
        $left = [int](($art.Width - $size) / 2)
        $html = @"
<!doctype html>
<html><head><meta charset="utf-8"><style>
html,body { margin:0; width:100%; height:100%; overflow:hidden; background:$background; }
svg { position:absolute; left:${left}px; top:0; width:${size}px; height:${size}px; }
</style></head><body>$svg</body></html>
"@
        $htmlPath = Join-Path $work 'art.html'
        [IO.File]::WriteAllText($htmlPath, $html, [Text.UTF8Encoding]::new($false))
        $pngPath = Join-Path $work $art.Name
        $profile = Join-Path $work 'profile'
        $uri = [Uri]::new($htmlPath).AbsoluteUri
        $arguments = "--headless --disable-gpu --no-first-run --no-default-browser-check --hide-scrollbars --force-device-scale-factor=1 --user-data-dir=`"$profile`" --screenshot=`"$pngPath`" --window-size=$($art.Width),$($art.Height) `"$uri`""
        $process = Start-Process -FilePath $browser -ArgumentList $arguments -PassThru
        if (-not $process.WaitForExit(60000)) {
            Stop-Process -Id $process.Id -Force
            throw 'SVG rendering timed out.'
        }
        if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $pngPath)) {
            throw "SVG rendering failed: $($art.Name), exit $($process.ExitCode)"
        }
        $image = [Drawing.Bitmap]::new($pngPath)
        try {
            if ($image.Width -ne $art.Width -or $image.Height -ne $art.Height -or
                $image.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid -or
                (Get-Item -LiteralPath $pngPath).Length -ge 50000000) {
                throw "Invalid Store PNG dimensions, format or size: $($art.Name)"
            }
            $backgroundArgb = [Drawing.ColorTranslator]::FromHtml($background).ToArgb()
            for ($y = [int]($art.Height * 2 / 3); $y -lt $art.Height; $y++) {
                for ($x = 0; $x -lt $art.Width; $x++) {
                    if ($image.GetPixel($x, $y).ToArgb() -ne $backgroundArgb) {
                        throw "Artwork overlaps the reserved bottom third: $($art.Name)"
                    }
                }
            }
        } finally { $image.Dispose() }
        Copy-Item -LiteralPath $pngPath -Destination (Join-Path $output $art.Name)
        Write-Output "PASS: $($art.Name), $($art.Width)x$($art.Height), opaque PNG with reserved bottom third."
    }
} finally {
    Remove-Item -LiteralPath $work -Recurse -Force
}
