#requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$validator = Join-Path $PSScriptRoot '..\scripts\Test-AppPackage.ps1'
$temporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) "PaddleDuel-PackageTests-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null

function Write-Archive([string]$Path, [hashtable]$Entries) {
    $zip = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $Entries.Keys) {
            $stream = $zip.CreateEntry($name).Open()
            try {
                [byte[]]$bytes = $Entries[$name]
                $stream.Write($bytes, 0, $bytes.Length)
            } finally { $stream.Dispose() }
        }
    } finally { $zip.Dispose() }
}

function Assert-Rejected([string]$Name, [string]$Extension, [hashtable]$Entries, [string]$ExpectedError) {
    $path = Join-Path $temporaryDirectory "$Name.$Extension"
    Write-Archive $path $Entries
    $message = $null
    try { & $validator -PackagePath $path -RequireUnsigned | Out-Null }
    catch { $message = $_.Exception.Message }
    if (-not $message -or -not $message.Contains($ExpectedError)) {
        throw "$Name expected rejection '$ExpectedError', got '$message'."
    }
    Write-Output "PASS: $Name"
}

try {
    $identity = 'Name="25526PoyChang.PaddleDuel" Publisher="CN=DC46547E-28C9-45C1-91CC-5380ACD55B44" Version="1.0.0.0"'
    $innerPath = Join-Path $temporaryDirectory 'inner.msix'
    Write-Archive $innerPath @{
        'AppxManifest.xml' = [Text.Encoding]::UTF8.GetBytes("<Package><Identity $identity /></Package>")
    }
    $inner = [IO.File]::ReadAllBytes($innerPath)
    $dependencies = '<Dependencies><TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19045.0" MaxVersionTested="10.0.26100.0" /></Dependencies>'
    $manifest = "<Bundle><Identity $identity /><Packages><Package Type=`"application`" Version=`"1.0.0.0`" Architecture=`"x64`" FileName=`"game.msix`" Size=`"$($inner.Length)`">$dependencies</Package></Packages></Bundle>"
    $manifestName = 'AppxMetadata/AppxBundleManifest.xml'
    $manifestBytes = [Text.Encoding]::UTF8.GetBytes($manifest)

    Assert-Rejected 'wrong-identity' 'msixbundle' @{
        $manifestName = [Text.Encoding]::UTF8.GetBytes($manifest.Replace('25526PoyChang.PaddleDuel', 'Wrong.Identity'))
    } 'Unexpected bundle identity.'
    Assert-Rejected 'wrong-architecture' 'msixbundle' @{
        $manifestName = [Text.Encoding]::UTF8.GetBytes($manifest.Replace('Architecture="x64"', 'Architecture="arm64"'))
    } 'Expected exactly one x64 application'
    Assert-Rejected 'wrong-bundle-minimum-os' 'msixbundle' @{
        $manifestName = [Text.Encoding]::UTF8.GetBytes($manifest.Replace('10.0.19045.0', '10.0.26100.0'))
    } 'Incorrect bundle Windows.Desktop OS requirements.'
    Assert-Rejected 'missing-payload' 'msixbundle' @{
        $manifestName = $manifestBytes
    } 'Bundle payload is missing'
    Assert-Rejected 'extra-payload' 'msixbundle' @{
        $manifestName = $manifestBytes
        'game.msix' = $inner
        'extra.msix' = $inner
    } 'Bundle payload is missing or differs'
    Assert-Rejected 'wrong-size' 'msixbundle' @{
        $manifestName = [Text.Encoding]::UTF8.GetBytes($manifest.Replace("Size=`"$($inner.Length)`"", 'Size="1"'))
        'game.msix' = $inner
    } 'Bundle payload is missing or differs'
    Assert-Rejected 'version-mismatch' 'msixbundle' @{
        $manifestName = [Text.Encoding]::UTF8.GetBytes($manifest.Replace('1.0.0.0', '1.0.1.0'))
        'game.msix' = $inner
    } 'Bundle and application versions must agree.'
    Assert-Rejected 'signed-bundle' 'msixbundle' @{
        'AppxSignature.p7x' = [byte[]]@(1)
    } 'Expected an unsigned Store submission artifact.'
    Assert-Rejected 'extra-upload-entry' 'msixupload' @{
        'game.msixbundle' = [byte[]]@(1)
        'unexpected.txt' = [byte[]]@(1)
    } 'Expected a Store upload containing exactly one bundle'

    $signedPath = Join-Path $temporaryDirectory 'signed-inner.msix'
    Write-Archive $signedPath @{
        'AppxManifest.xml' = [Text.Encoding]::UTF8.GetBytes("<Package><Identity $identity /></Package>")
        'AppxSignature.p7x' = [byte[]]@(1)
    }
    $signedInner = [IO.File]::ReadAllBytes($signedPath)
    Assert-Rejected 'signed-nested-payload' 'msixbundle' @{
        $manifestName = [Text.Encoding]::UTF8.GetBytes($manifest.Replace("Size=`"$($inner.Length)`"", "Size=`"$($signedInner.Length)`""))
        'game.msix' = $signedInner
    } 'Expected an unsigned Store submission artifact.'
} finally {
    Get-ChildItem -LiteralPath $temporaryDirectory -File | Remove-Item
    Remove-Item -LiteralPath $temporaryDirectory
}
