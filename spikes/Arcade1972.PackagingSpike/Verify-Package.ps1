#requires -Version 5.1
param([Parameter(Mandatory = $true)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$packageName = 'Arcade1972.PackagingSpike'
$signTool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$certificate = $null
$appProcessId = 0
$cerPath = Join-Path $PSScriptRoot 'packaging-spike.cer'
if (Get-AppxPackage -Name $packageName) { throw 'Existing spike installation must not be overwritten.' }
if (Test-Path $cerPath) { throw "Existing certificate file must not be overwritten: $cerPath" }

function Invoke-Elevated([string]$Command) {
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes(
        "`$ErrorActionPreference = 'Stop'; try { $Command; exit 0 } catch { Write-Error `$_ -ErrorAction Continue; exit 1 }"))
    $process = Start-Process "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" `
        -Verb RunAs -ArgumentList "-NoProfile -EncodedCommand $encoded" -PassThru
    Write-Output "Elevated certificate operation PID: $($process.Id)"
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Certificate operation failed: $($process.ExitCode)" }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
    $reader = New-Object IO.StreamReader($zip.GetEntry('AppxManifest.xml').Open())
    try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($manifest.Package.Identity.Name -ne $packageName -or
        $manifest.Package.Identity.ProcessorArchitecture -ne 'x64' -or
        $manifest.Package.Identity.Publisher -ne 'CN=Arcade1972 Packaging Spike') {
        throw 'Unexpected package identity.'
    }
    $families = @($manifest.Package.Dependencies.TargetDeviceFamily)
    if ($families.Count -ne 1 -or $families[0].Name -ne 'Windows.Desktop' -or
        $families[0].MinVersion -ne '10.0.19045.0' -or $families[0].MaxVersionTested -ne '10.0.26100.0') {
        throw 'Incorrect generated OS requirements.'
    }
    foreach ($name in @('coreclr.dll', 'Microsoft.ui.xaml.dll', 'Arcade1972.PackagingSpike.exe')) {
        if (-not $zip.GetEntry($name)) { throw "Missing self-contained payload: $name" }
    }
    Write-Output "PASS: x64 manifest, Windows.Desktop 19045 minimum, self-contained payload."
} finally { $zip.Dispose() }

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type @'
using System;
using System.Runtime.InteropServices;
[ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IApplicationActivationManager
{
    [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
        [MarshalAs(UnmanagedType.LPWStr)] string arguments, uint options, out uint processId);
    [PreserveSig] int ActivateForFile(IntPtr items, [MarshalAs(UnmanagedType.LPWStr)] string verb, out uint processId);
    [PreserveSig] int ActivateForProtocol(IntPtr items, out uint processId);
}
public static class SpikeActivation
{
    public static uint Activate(string appId)
    {
        object instance = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")));
        try
        {
            uint processId;
            int result = ((IApplicationActivationManager)instance).ActivateApplication(appId, "", 0, out processId);
            Marshal.ThrowExceptionForHR(result);
            return processId;
        }
        finally { Marshal.ReleaseComObject(instance); }
    }
}
'@

try {
    $certificate = New-SelfSignedCertificate -Type Custom -KeyUsage DigitalSignature `
        -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy NonExportable `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}') `
        -Subject 'CN=Arcade1972 Packaging Spike' -FriendlyName 'Temporary Packaging Spike Validation' `
        -NotAfter (Get-Date).AddDays(1)
    Write-Output "Temporary certificate: $($certificate.Thumbprint)"
    & $signTool sign /fd SHA256 /sha1 $certificate.Thumbprint /s My $PackagePath
    if ($LASTEXITCODE -ne 0) { throw "Signing failed: $LASTEXITCODE" }
    Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null
    $escapedPath = $cerPath.Replace("'", "''")
    Invoke-Elevated "Import-Certificate -FilePath '$escapedPath' -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null"
    & $signTool verify /pa /v $PackagePath
    if ($LASTEXITCODE -ne 0) { throw "Signature validation failed: $LASTEXITCODE" }
    Add-AppxPackage -Path $PackagePath
    $installed = Get-AppxPackage -Name $packageName
    if (-not $installed -or $installed.Status -ne 'Ok') { throw 'Installation verification failed.' }
    Write-Output "PASS: installed $($installed.PackageFullName)"
    $appProcessId = [SpikeActivation]::Activate("$($installed.PackageFamilyName)!App")
    Write-Output "Activated PID: $appProcessId"
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]$appProcessId)
    $window = $null
    $deadline = (Get-Date).AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 250
        $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
            [System.Windows.Automation.TreeScope]::Children, $condition)
    } while (-not $window -and (Get-Date) -lt $deadline)
    if (-not $window) { throw 'No UI Automation window after activation.' }
    if ($window.Current.Name -ne '1972 Arcade Packaging Spike' -or $window.Current.IsOffscreen) {
        throw 'Expected visible spike window was not found.'
    }
    function Find-Control([string]$Id) {
        $controlCondition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $Id)
        $result = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $controlCondition)
        if (-not $result) { throw "Missing UI control: $Id" }
        return $result
    }
    $identity = (Find-Control 'PackageIdentity').Current.Name
    $runtime = (Find-Control 'RuntimeDetails').Current.Name
    if ($identity -ne $installed.PackageFullName) { throw "Incorrect UI identity: $identity" }
    if ($runtime -notmatch '^\.NET 10\.\d+\.\d+ \| X64$') { throw "Incorrect runtime: $runtime" }
    Write-Output "PASS: visible WinUI window, package identity $identity, runtime $runtime"
    $close = Find-Control 'CloseSpike'
    $pattern = $close.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
    if ($process -and -not $process.WaitForExit(10000)) { throw 'Close button did not exit the spike.' }
    Write-Output 'PASS: UI Automation CLOSE button exited the process.'
} finally {
    try {
        if ($appProcessId -ne 0) {
            $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
            if ($process) { Stop-Process -Id $appProcessId -ErrorAction Stop }
        }
        Get-AppxPackage -Name $packageName | Remove-AppxPackage -ErrorAction Stop
        if (Get-AppxPackage -Name $packageName) { throw 'Spike package cleanup failed.' }
    } finally {
        if ($certificate) {
            try {
                $trustPath = "Cert:\LocalMachine\TrustedPeople\$($certificate.Thumbprint)"
                if (Test-Path $trustPath) {
                    Invoke-Elevated "Remove-Item -LiteralPath '$trustPath' -ErrorAction Stop"
                }
                if (Test-Path $trustPath) { throw "Trusted certificate cleanup failed: $trustPath" }
            } finally {
                Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -DeleteKey -ErrorAction Stop
                if (Test-Path $cerPath) { Remove-Item -LiteralPath $cerPath -ErrorAction Stop }
            }
        }
    }
    Write-Output 'PASS: test package, trust certificate, signing key and public certificate file removed.'
}
