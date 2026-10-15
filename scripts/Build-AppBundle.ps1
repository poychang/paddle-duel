#requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repository = Split-Path -Parent $PSScriptRoot
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere)) { throw "Missing Visual Studio locator: $vswhere" }
$vs = & $vswhere -latest -products '*' -version '[18.0,19.0)' `
    -requires Microsoft.VisualStudio.Component.WindowsAppSdkSupport.CSharp `
              Microsoft.VisualStudio.ComponentGroup.MSIX.Packaging `
    -property installationPath
if ($LASTEXITCODE -ne 0 -or -not $vs) { throw 'Visual Studio 2026 with MSIX packaging tools is required.' }

$output = Join-Path $repository "BundleArtifacts\Release-x64\$([Guid]::NewGuid().ToString('N'))\"
$project = Join-Path $repository 'src\PaddleDuel.App\PaddleDuel.App.csproj'

# Keep both languages and every artwork scale in the application package.
& "$vs\MSBuild\Current\Bin\MSBuild.exe" $project -t:Rebuild `
    -p:Configuration=Release -p:Platform=x64 `
    -p:GenerateAppxPackageOnBuild=true -p:UapAppxPackageBuildMode=StoreUpload `
    -p:AppxBundle=Always -p:AppxBundlePlatforms=x64 `
    -p:AppxBundleAutoResourcePackageQualifiers=DXFeatureLevel `
    '-p:AppxDefaultResourceQualifiers=Language=en-US%3Bzh-TW' `
    -p:AppxPackageSigningEnabled=false -p:AppxSymbolPackageEnabled=false `
    "-p:AppxPackageDir=$output" -verbosity:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw "Release bundle build failed. Inspect output under: $output" }

$bundles = @(Get-ChildItem -LiteralPath $output -Recurse -Filter '*.msixbundle')
if ($bundles.Count -ne 1) { throw "Expected one MSIX bundle under: $output" }
$uploads = @(Get-ChildItem -LiteralPath $output -Filter '*.msixupload')
if ($uploads.Count -ne 1) { throw "Expected one Store upload under: $output" }
foreach ($artifact in @($bundles[0], $uploads[0])) {
    & "$PSScriptRoot\Test-AppPackage.ps1" -PackagePath $artifact.FullName -RequireUnsigned
}
Write-Output "Unsigned Release x64 bundle: $($bundles[0].FullName)"
Write-Output "Store upload (not submitted): $($uploads[0].FullName)"
Get-FileHash -LiteralPath $bundles[0].FullName, $uploads[0].FullName -Algorithm SHA256
