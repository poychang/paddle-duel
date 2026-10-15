#requires -Version 5.1
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [switch]$RequireUnsigned
)

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

    if ($RequireUnsigned -and $archive.GetEntry('AppxSignature.p7x')) {
        throw 'Expected an unsigned Store submission artifact.'
    }
    $extension = [IO.Path]::GetExtension($PackagePath)
    if ($extension -in @('.msixbundle', '.msixupload')) {
        $temporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) "PaddleDuel-Bundle-$([Guid]::NewGuid().ToString('N'))"
        $payloadPath = $null
        New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
        try {
            if ($extension -eq '.msixupload') {
                $bundles = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.msixbundle') })
                if ($bundles.Count -ne 1 -or $archive.Entries.Count -ne 1) {
                    throw 'Expected a Store upload containing exactly one bundle and no symbols.'
                }
                $entry = $bundles[0]
                $payloadPath = Join-Path $temporaryDirectory 'payload.msixbundle'
            } else {
                [xml]$bundle = Read-PackageText 'AppxMetadata/AppxBundleManifest.xml'
                if ($bundle.Bundle.Identity.Name -ne '25526PoyChang.PaddleDuel' -or
                    $bundle.Bundle.Identity.Publisher -ne 'CN=DC46547E-28C9-45C1-91CC-5380ACD55B44') {
                    throw 'Unexpected bundle identity.'
                }
                $packages = @($bundle.Bundle.Packages.Package)
                if ($packages.Count -ne 1 -or $packages[0].Type -ne 'application' -or
                    $packages[0].Architecture -ne 'x64') {
                    throw 'Expected exactly one x64 application with all resources embedded.'
                }
                $families = @($packages[0].SelectNodes("*[local-name()='Dependencies']/*[local-name()='TargetDeviceFamily']"))
                if ($families.Count -ne 1 -or $families[0].Name -ne 'Windows.Desktop' -or
                    $families[0].MinVersion -ne '10.0.19045.0' -or
                    $families[0].MaxVersionTested -ne '10.0.26100.0') {
                    throw 'Incorrect bundle Windows.Desktop OS requirements.'
                }
                $entry = $archive.GetEntry($packages[0].FileName)
                $embeddedPackages = @($archive.Entries | Where-Object { $_.FullName -match '\.(msix|appx)$' })
                if (-not $entry -or $embeddedPackages.Count -ne 1 -or
                    $entry.Length -ne [long]$packages[0].Size) {
                    throw 'Bundle payload is missing or differs from its manifest.'
                }
                $payloadPath = Join-Path $temporaryDirectory 'payload.msix'
            }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $payloadPath)
            if ($extension -eq '.msixbundle') {
                $payload = [IO.Compression.ZipFile]::OpenRead($payloadPath)
                try {
                    $reader = [IO.StreamReader]::new($payload.GetEntry('AppxManifest.xml').Open())
                    try { [xml]$appManifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
                    if ($appManifest.Package.Identity.Version -ne $packages[0].Version -or
                        $appManifest.Package.Identity.Version -ne $bundle.Bundle.Identity.Version) {
                        throw 'Bundle and application versions must agree.'
                    }
                } finally { $payload.Dispose() }
            }
            & $PSCommandPath -PackagePath $payloadPath -RequireUnsigned:$RequireUnsigned
            Write-Output "PASS: $extension container and embedded payload."
        } finally {
            if ($payloadPath -and (Test-Path -LiteralPath $payloadPath)) { Remove-Item -LiteralPath $payloadPath }
            Remove-Item -LiteralPath $temporaryDirectory
        }
        return
    }

    [xml]$manifest = Read-PackageText 'AppxManifest.xml'
    if ($manifest.Package.Identity.Name -ne '25526PoyChang.PaddleDuel' -or
        $manifest.Package.Identity.Publisher -ne 'CN=DC46547E-28C9-45C1-91CC-5380ACD55B44' -or
        $manifest.Package.Identity.ProcessorArchitecture -ne 'x64') {
        throw 'Unexpected Store package identity or architecture.'
    }
    if ($manifest.Package.Properties.PublisherDisplayName -ne 'Poy Chang' -or
        $manifest.Package.Properties.DisplayName -ne 'ms-resource:AppDisplayName') {
        throw 'Incorrect publisher or localized package name.'
    }
    $languages = @($manifest.Package.Resources.Resource |
        Where-Object { $_.HasAttribute('Language') } |
        ForEach-Object { $_.Language.ToLowerInvariant() })
    if ($languages.Count -ne 2 -or $languages -notcontains 'en-us' -or $languages -notcontains 'zh-tw') {
        throw 'The generated package must declare en-US and zh-TW.'
    }

    $families = @($manifest.Package.Dependencies.TargetDeviceFamily)
    if ($families.Count -ne 1 -or $families[0].Name -ne 'Windows.Desktop' -or
        $families[0].MinVersion -ne '10.0.19045.0' -or
        $families[0].MaxVersionTested -ne '10.0.26100.0') {
        throw 'Incorrect generated Windows.Desktop OS requirements.'
    }
    if ($manifest.SelectNodes("//*[local-name()='PackageDependency']").Count -ne 0) {
        throw 'Self-contained game package must not require framework packages.'
    }

    $apps = @($manifest.Package.Applications.Application)
    if ($apps.Count -ne 1 -or $apps[0].Id -ne 'App' -or
        $apps[0].Executable -ne 'PaddleDuel.App.exe' -or
        $apps[0].EntryPoint -ne 'Windows.FullTrustApplication' -or
        $apps[0].VisualElements.DisplayName -ne 'ms-resource:AppDisplayName' -or
        $apps[0].VisualElements.Description -ne 'ms-resource:AppDescription') {
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
        @{ Path = $manifest.Package.Properties.Logo; Width = 50; Height = 50 },
        @{ Path = $apps[0].VisualElements.Square44x44Logo; Width = 44; Height = 44 },
        @{ Path = $apps[0].VisualElements.Square150x150Logo; Width = 150; Height = 150 },
        @{ Path = $apps[0].VisualElements.SplashScreen.Image; Width = 620; Height = 300 }
    )
    foreach ($logo in $logos) {
        foreach ($scale in @(100, 125, 150, 200, 400)) {
            $path = $logo.Path.Replace('\', '/').Replace('.png', ".scale-$scale.png")
            $entry = $archive.GetEntry($path)
            if (-not $entry) { throw "Missing scaled manifest artwork: $path" }
            $stream = $entry.Open()
            try {
                $image = [Drawing.Image]::FromStream($stream)
                try {
                    if ($image.Width -ne [Math]::Ceiling($logo.Width * $scale / 100) -or
                        $image.Height -ne [Math]::Ceiling($logo.Height * $scale / 100)) {
                        throw "Incorrect artwork dimensions: $path"
                    }
                } finally { $image.Dispose() }
            } finally { $stream.Dispose() }
        }
    }
    foreach ($size in @(16, 24, 32, 48, 256)) {
        foreach ($suffix in @('', '_altform-unplated', '_altform-lightunplated')) {
            $path = "Assets/Square44x44Logo.targetsize-$size$suffix.png"
            $entry = $archive.GetEntry($path)
            if (-not $entry) { throw "Missing taskbar icon: $path" }
            $stream = $entry.Open()
            try {
                $image = [Drawing.Image]::FromStream($stream)
                try {
                    if ($image.Width -ne $size -or $image.Height -ne $size) {
                        throw "Incorrect taskbar icon dimensions: $path"
                    }
                } finally { $image.Dispose() }
            } finally { $stream.Dispose() }
        }
    }
    if (-not $archive.GetEntry('Assets/PaddleDuel.ico')) { throw 'Missing desktop icon.' }

    $makePri = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.26100.0\x64\makepri.exe"
    if (-not (Test-Path -LiteralPath $makePri)) { throw "Missing Windows SDK tool: $makePri" }
    $temporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) "PaddleDuel-Pri-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
    $priPath = Join-Path $temporaryDirectory 'resources.pri'
    $dumpPath = Join-Path $temporaryDirectory 'resources.xml'
    try {
        [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry('resources.pri'), $priPath)
        & $makePri dump /if $priPath /of $dumpPath /dt detailed | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect packaged PRI.' }
        [xml]$pri = Get-Content -LiteralPath $dumpPath -Raw -Encoding UTF8
        $strings = @($pri.SelectNodes("//ResourceMapSubtree[@name='Resources']//NamedResource"))
        foreach ($language in @('en-US', 'zh-TW')) {
            [xml]$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\src\PaddleDuel.App\Strings\$language\Resources.resw") -Raw -Encoding UTF8
            foreach ($data in $source.root.data) {
                # PRI preserves dots inside an attached property's [using:...] namespace.
                $resourcePath = $data.name -replace '\.(?![^\[]*\])', '/'
                $uri = "ms-resource://25526PoyChang.PaddleDuel/Resources/$resourcePath"
                $resource = @($strings | Where-Object { $_.uri -eq $uri })
                if ($resource.Count -ne 1) { throw "Missing packaged string: $uri" }
                $candidates = @($resource[0].Candidate | Where-Object {
                    $_.QualifierSet.Qualifier | Where-Object { $_.name -eq 'Language' -and $_.value -eq $language }
                })
                if ($candidates.Count -ne 1 -or $candidates[0].Value -cne $data.value) {
                    throw "Incorrect packaged translation: $language / $($data.name)"
                }
            }
        }
    } finally {
        foreach ($path in @($priPath, $dumpPath)) {
            if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
        }
        Remove-Item -LiteralPath $temporaryDirectory
    }
    Write-Output 'PASS: official identity, x64, Windows.Desktop 19045 minimum / 26100 target.'
    Write-Output 'PASS: self-contained game, scaled artwork, splash and en-US / zh-TW packaged strings.'
} finally {
    $archive.Dispose()
}
