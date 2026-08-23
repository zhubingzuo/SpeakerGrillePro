$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

Write-Host 'Manual SOLIDWORKS interop build helper'
Write-Host 'Paste the FULL path to each DLL. Quotes are optional.'
Write-Host ''
$sld = (Read-Host 'SolidWorks.Interop.sldworks.dll').Trim('"')
$swc = (Read-Host 'SolidWorks.Interop.swconst.dll').Trim('"')
$swp = (Read-Host 'SolidWorks.Interop.swpublished.dll').Trim('"')
foreach ($p in @($sld,$swc,$swp)) {
    if (-not (Test-Path -LiteralPath $p)) { throw "File not found: $p" }
}

$msbuild = $null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null | Select-Object -First 1
}
if (-not $msbuild) {
  foreach($f in @("$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe","$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe")) {
    if(Test-Path -LiteralPath $f){$msbuild=$f;break}
  }
}
if(-not $msbuild){throw 'MSBuild not found. Install Visual Studio 2022 Build Tools (.NET desktop build tools).'}

& $msbuild 'src\SpeakerGrillePro.csproj' /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU "/p:SldWorksInterop=$sld" "/p:SwConstInterop=$swc" "/p:SwPublishedInterop=$swp" /v:minimal
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
Write-Host ''
Write-Host 'Build complete. Run install_admin.bat as Administrator.' -ForegroundColor Green
