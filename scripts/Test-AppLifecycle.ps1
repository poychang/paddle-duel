#requires -Version 5.1
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [switch]$AllowTemporaryInstall
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $AllowTemporaryInstall) {
    throw 'Explicit -AllowTemporaryInstall consent is required for signing, trust, install, upgrade and removal.'
}
$packageName = '25526PoyChang.PaddleDuel'
$familyName = '25526PoyChang.PaddleDuel_hwmjejkjqxx7e'
$publisher = 'CN=DC46547E-28C9-45C1-91CC-5380ACD55B44'
$packageData = Join-Path $env:LOCALAPPDATA "Packages\$familyName"
$localState = Join-Path $packageData 'LocalState'
$sdk = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.26100.0\x64"
$windowsPowerShell = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$runId = [Guid]::NewGuid().ToString()
$work = Join-Path ([IO.Path]::GetTempPath()) "PaddleDuel-Lifecycle-$runId"
$certificate = $null
$ownsInstallation = $false
$appProcessId = 0
$legacyDirectory = Join-Path $env:LOCALAPPDATA 'Arcade1972'

if (Get-AppxPackage -Name $packageName) { throw 'Existing official installation must not be overwritten.' }
if (Test-Path -LiteralPath $packageData) { throw "Existing package data must not be overwritten: $packageData" }
if (Get-Process | Where-Object { $_.ProcessName -in @('PaddleDuel.App', 'Arcade1972.App') }) {
    throw 'Close existing game processes before testing.'
}
foreach ($tool in @('makeappx.exe', 'signtool.exe')) {
    if (-not (Test-Path (Join-Path $sdk $tool))) { throw "Missing Windows SDK tool: $tool" }
}
Get-Command dotnet -ErrorAction Stop | Out-Null
& "$PSScriptRoot\Test-AppPackage.ps1" -PackagePath $PackagePath
$legacyHashes = @{}
if (Test-Path -LiteralPath $legacyDirectory) {
    foreach ($file in Get-ChildItem -LiteralPath $legacyDirectory -File -Recurse -Force) {
        $legacyHashes[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash
    }
}

function Invoke-Elevated([string]$Command) {
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes(
        "`$ErrorActionPreference='Stop'; try { $Command; exit 0 } catch { Write-Error `$_ -ErrorAction Continue; exit 1 }"))
    $process = Start-Process $windowsPowerShell -Verb RunAs `
        -ArgumentList "-NoProfile -EncodedCommand $encoded" -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Elevated certificate operation failed: $($process.ExitCode)" }
}

function Get-StateHashes {
    $hashes = @{}
    foreach ($file in Get-ChildItem -LiteralPath $localState -File -Recurse -Force) {
        $hashes[$file.FullName.Substring($localState.Length)] = (Get-FileHash -LiteralPath $file.FullName).Hash
    }
    return $hashes
}

function Assert-Removed {
    $deadline = (Get-Date).AddSeconds(30)
    do {
        if (-not (Get-AppxPackage -Name $packageName) -and -not (Test-Path -LiteralPath $packageData)) { return }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    throw "Uninstall did not remove registration and package data: $packageData"
}

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.IO.Compression.FileSystem
Add-Type @'
using System;
using System.Runtime.InteropServices;
[ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ILifecycleActivationManager
{
    [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string id,
        [MarshalAs(UnmanagedType.LPWStr)] string args, uint options, out uint pid);
    [PreserveSig] int ActivateForFile(IntPtr items, [MarshalAs(UnmanagedType.LPWStr)] string verb, out uint pid);
    [PreserveSig] int ActivateForProtocol(IntPtr items, [MarshalAs(UnmanagedType.LPWStr)] string verb, out uint pid);
}
public static class LifecycleActivation
{
    public static uint Activate(string id)
    {
        object instance = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")));
        try
        {
            uint pid;
            Marshal.ThrowExceptionForHR(((ILifecycleActivationManager)instance).ActivateApplication(id, "", 0, out pid));
            return pid;
        }
        finally { Marshal.ReleaseComObject(instance); }
    }
}
'@

function Find-Control([string]$Id) {
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $Id)
    $deadline = (Get-Date).AddSeconds(10)
    do {
        $control = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
        if ($control -and -not $control.Current.IsOffscreen) { return $control }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $deadline)
    throw "Missing visible control: $Id"
}

function Assert-Quota([int]$Remaining) {
    $deadline = (Get-Date).AddSeconds(10)
    do {
        $text = (Find-Control 'QuotaStatusText').Current.Name
        if ($text -match "  $Remaining / 3$") { return }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $deadline)
    throw "Unexpected quota display: $text"
}

function Open-Game([int]$Remaining) {
    # Never let the production App retry the synthetic journal through a real Store gateway.
    $journalPath = Join-Path $localState 'pending-fulfillments.json'
    if (Test-Path -LiteralPath $journalPath) {
        $pending = @(Get-Content -LiteralPath $journalPath -Raw -Encoding UTF8 | ConvertFrom-Json)
        if ($pending.Count -gt 0) { throw 'Refusing App launch while synthetic fulfillment is pending.' }
    }
    $script:appProcessId = [LifecycleActivation]::Activate("$familyName!App")
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$appProcessId)
    $deadline = (Get-Date).AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 200
        $script:window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
            [System.Windows.Automation.TreeScope]::Children, $condition)
    } while (-not $window -and (Get-Date) -lt $deadline)
    if (-not $window -or $window.Current.Name -ne 'Paddle Duel' -or $window.Current.IsOffscreen) {
        throw 'Packaged game failed to create a visible window.'
    }
    Assert-Quota $Remaining
}

function Close-Game {
    $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
    $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
    if ($process -and -not $process.WaitForExit(10000)) { throw 'Game did not exit normally.' }
    $script:appProcessId = 0
}

try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $baseline = Join-Path $work 'baseline.msix'
    $upgrade = Join-Path $work 'upgrade.msix'
    $unpacked = Join-Path $work 'unpacked'
    Copy-Item -LiteralPath (Resolve-Path -LiteralPath $PackagePath).Path -Destination $baseline
    & "$sdk\makeappx.exe" unpack /p $baseline /d $unpacked /o | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to unpack upgrade fixture.' }
    $manifestPath = Join-Path $unpacked 'AppxManifest.xml'
    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8
    $baseVersion = [Version]$manifest.Package.Identity.Version
    if ($baseVersion.Build -ge 65535) { throw 'No available build version for the upgrade fixture.' }
    $upgradeVersion = [Version]::new($baseVersion.Major, $baseVersion.Minor, $baseVersion.Build + 1, 0)
    $manifest.Package.Identity.Version = $upgradeVersion.ToString()
    $manifest.Save($manifestPath)
    & "$sdk\makeappx.exe" pack /d $unpacked /p $upgrade /o | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to pack upgrade fixture.' }
    & "$PSScriptRoot\Test-AppPackage.ps1" -PackagePath $upgrade

    $certificate = New-SelfSignedCertificate -Type Custom -KeyUsage DigitalSignature `
        -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy NonExportable `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}') `
        -Subject $publisher -FriendlyName 'Temporary Paddle Duel Lifecycle Test' -NotAfter (Get-Date).AddDays(1)
    Write-Output "Temporary certificate: $($certificate.Thumbprint)"
    foreach ($package in @($baseline, $upgrade)) {
        & "$sdk\signtool.exe" sign /fd SHA256 /sha1 $certificate.Thumbprint /s My $package | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Signing failed: $package" }
    }
    $cerPath = Join-Path $work 'lifecycle.cer'
    Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null
    $escapedCerPath = $cerPath.Replace("'", "''")
    Invoke-Elevated "Import-Certificate -FilePath '$escapedCerPath' -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null"
    foreach ($package in @($baseline, $upgrade)) {
        & "$sdk\signtool.exe" verify /pa $package | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Signature verification failed: $package" }
    }
    # Recheck immediately before acquiring ownership of the temporary installation.
    if ((Get-AppxPackage -Name $packageName) -or (Test-Path -LiteralPath $packageData)) {
        throw 'An installation or data appeared during preparation; refusing to overwrite it.'
    }
    Add-AppxPackage -Path $baseline
    $ownsInstallation = $true
    $installed = Get-AppxPackage -Name $packageName
    if ($installed.Version -ne $baseVersion -or $installed.PackageFamilyName -ne $familyName -or $installed.Status -ne 'Ok') {
        throw 'Incorrect baseline registration.'
    }
    Open-Game 3
    $bounds = $window.Current.BoundingRectangle
    if ($bounds.Width -ne 1024 -or $bounds.Height -ne 720) { throw "Unexpected fresh window size: $bounds" }
    Close-Game
    if (-not (Test-Path (Join-Path $localState 'free-play-quota.json')) -or
        -not (Test-Path (Join-Path $localState 'window-state.json'))) { throw 'App did not persist in package LocalState.' }
    Write-Output "PASS: fresh install $baseVersion, visible window and package LocalState."

    [IO.File]::WriteAllText((Join-Path $localState 'lifecycle-test-owner.txt'), $runId)
    & dotnet run --file "$PSScriptRoot\LifecycleProbe.cs" -- prepare $localState $runId
    if ($LASTEXITCODE -ne 0) { throw 'State preparation probe failed.' }
    $windowPath = Join-Path $localState 'window-state.json'
    $state = Get-Content -LiteralPath $windowPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $state.Width = 960
    $state.Height = 680
    $state.IsFullScreen = $false
    [IO.File]::WriteAllText($windowPath, ($state | ConvertTo-Json))
    $before = Get-StateHashes
    $journalPath = Join-Path $localState 'pending-fulfillments.json'
    $pendingBytes = [IO.File]::ReadAllBytes($journalPath)

    Add-AppxPackage -Path $upgrade
    $installed = Get-AppxPackage -Name $packageName
    if ($installed.Version -ne $upgradeVersion -or $installed.PackageFamilyName -ne $familyName -or $installed.Status -ne 'Ok') {
        throw 'Incorrect upgraded registration.'
    }
    $after = Get-StateHashes
    if ($after.Count -ne $before.Count) { throw 'Upgrade changed the set of LocalState files.' }
    foreach ($path in $before.Keys) {
        if ($after[$path] -ne $before[$path]) { throw "Upgrade changed LocalState bytes: $path" }
    }
    Write-Output "PASS: in-place upgrade $baseVersion -> $upgradeVersion preserved every LocalState byte."
    & dotnet run --file "$PSScriptRoot\LifecycleProbe.cs" -- retry $localState $runId
    if ($LASTEXITCODE -ne 0) { throw 'Post-upgrade retry probe failed.' }

    Open-Game 1
    $bounds = $window.Current.BoundingRectangle
    if ($bounds.Width -ne 960 -or $bounds.Height -ne 680) { throw "Upgrade did not restore window size: $bounds" }
    [LifecycleActivation]::Activate("$familyName!App") | Out-Null
    Start-Sleep -Seconds 1
    $running = @(Get-Process | Where-Object { $_.ProcessName -eq 'PaddleDuel.App' })
    if ($running.Count -ne 1 -or $running[0].Id -ne $appProcessId) { throw 'Duplicate instance after upgrade.' }
    Close-Game
    Open-Game 1
    Close-Game
    Write-Output 'PASS: upgraded App restores quota/window, retains rollback protection and stays single-instance.'

    # Confirm normal uninstall also removes an unresolved journal, without sending it to Store.
    [IO.File]::WriteAllBytes($journalPath, $pendingBytes)
    Remove-AppxPackage -Package $installed.PackageFullName
    Assert-Removed
    Write-Output 'PASS: normal uninstall removed package data, including quota, settings, quarantine and pending journal.'
    Add-AppxPackage -Path $upgrade
    if (Test-Path -LiteralPath $localState) {
        if (Get-ChildItem -LiteralPath $localState -File -Recurse -Force) { throw 'Reinstall resurrected old LocalState.' }
    }
    Open-Game 3
    $bounds = $window.Current.BoundingRectangle
    if ($bounds.Width -ne 1024 -or $bounds.Height -ne 720) { throw 'Reinstall retained old window settings.' }
    Close-Game
    Write-Output 'PASS: reinstall starts with fresh local allowance and default window, not the previous installation state.'
} finally {
    try {
        if ($appProcessId -ne 0) {
            $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
            if ($process) { Stop-Process -Id $appProcessId -ErrorAction Stop }
        }
        if ($ownsInstallation) {
            Get-AppxPackage -Name $packageName | Remove-AppxPackage -ErrorAction Stop
            Assert-Removed
        }
    } finally {
        try {
            if ($certificate) {
                try {
                    $trustPath = "Cert:\LocalMachine\TrustedPeople\$($certificate.Thumbprint)"
                    if (Test-Path -LiteralPath $trustPath) {
                        Invoke-Elevated "Remove-Item -LiteralPath '$trustPath' -ErrorAction Stop"
                    }
                    if (Test-Path -LiteralPath $trustPath) { throw 'Test trust cleanup failed.' }
                } finally {
                    Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -DeleteKey -ErrorAction Stop
                }
            }
        } finally {
            if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction Stop }
        }
    }
    $afterLegacy = @()
    if (Test-Path -LiteralPath $legacyDirectory) {
        $afterLegacy = @(Get-ChildItem -LiteralPath $legacyDirectory -File -Recurse -Force)
    }
    if ($afterLegacy.Count -ne $legacyHashes.Count) { throw 'Legacy data file set changed.' }
    foreach ($file in $afterLegacy) {
        if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne $legacyHashes[$file.FullName]) {
            throw "Legacy data changed: $($file.FullName)"
        }
    }
    Write-Output 'PASS: test installation, package data, trust, private key and temporary packages removed; legacy data unchanged.'
}
