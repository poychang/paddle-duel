#requires -Version 5.1
param([Parameter(Mandatory = $true)][string]$PackagePath)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem, System.Drawing

$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
try {
    function Read-PackageText([string]$Name) {
        $entry = $archive.GetEntry($Name)
        if (-not $entry) { throw "Missing package entry: $Name" }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
    }

    [xml]$manifest = Read-PackageText 'AppxManifest.xml'
    if ($manifest.Package.Identity.Name -ne 'PaddleDuel.Development' -or
        $manifest.Package.Identity.Publisher -ne 'CN=PaddleDuel Development' -or
        $manifest.Package.Identity.ProcessorArchitecture -ne 'x64') {
        throw 'Unexpected development package identity or architecture.'
    }

    $families = @($manifest.Package.Dependencies.TargetDeviceFamily)
    if ($families.Count -ne 1 -or $families[0].Name -ne 'Windows.Desktop' -or
        $families[0].MinVersion -ne '10.0.19045.0' -or
        $families[0].MaxVersionTested -ne '10.0.26100.0') {
        throw 'Incorrect generated Windows.Desktop OS requirements.'
    }
    if ($manifest.SelectNodes("//*[local-name()='PackageDependency']").Count -ne 0) {
        throw 'Self-contained development package must not require framework packages.'
    }

    $apps = @($manifest.Package.Applications.Application)
    if ($apps.Count -ne 1 -or $apps[0].Id -ne 'App' -or
        $apps[0].Executable -ne 'PaddleDuel.App.exe' -or
        $apps[0].EntryPoint -ne 'Windows.FullTrustApplication' -or
        $apps[0].VisualElements.DisplayName -ne 'Paddle Duel') {
        throw 'Incorrect game activation or display metadata.'
    }

    foreach ($name in @(
        'PaddleDuel.App.exe', 'PaddleDuel.App.dll', 'PaddleDuel.Core.dll',
        'PaddleDuel.Infrastructure.dll', 'coreclr.dll', 'hostfxr.dll',
        'hostpolicy.dll', 'Microsoft.ui.xaml.dll', 'resources.pri')) {
        if (-not $archive.GetEntry($name)) { throw "Missing self-contained game payload: $name" }
    }
    $runtime = (Read-PackageText 'PaddleDuel.App.runtimeconfig.json' | ConvertFrom-Json).runtimeOptions
    if ($runtime.tfm -ne 'net10.0' -or
        $runtime.PSObject.Properties.Name -contains 'framework' -or
        $runtime.PSObject.Properties.Name -contains 'frameworks') {
        throw 'Game must use a self-contained .NET 10 runtime.'
    }

    $logos = @(
        @{ Path = $manifest.Package.Properties.Logo; Size = 50 },
        @{ Path = $apps[0].VisualElements.Square44x44Logo; Size = 44 },
        @{ Path = $apps[0].VisualElements.Square150x150Logo; Size = 150 }
    )
    foreach ($logo in $logos) {
        $entry = $archive.GetEntry($logo.Path.Replace('\', '/'))
        if (-not $entry) { throw "Missing manifest logo: $($logo.Path)" }
        $stream = $entry.Open()
        try {
            $image = [Drawing.Image]::FromStream($stream)
            try {
                if ($image.Width -ne $logo.Size -or $image.Height -ne $logo.Size) {
                    throw "Incorrect placeholder logo dimensions: $($logo.Path)"
                }
            } finally { $image.Dispose() }
        } finally { $stream.Dispose() }
    }
    Write-Output 'PASS: development identity, x64, Windows.Desktop 19045 minimum / 26100 target.'
    Write-Output 'PASS: game activation, self-contained .NET 10 / Windows App SDK payload and logos.'
} finally {
    $archive.Dispose()
}
