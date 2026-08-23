$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

Write-Host '============================================================'
Write-Host 'SpeakerGrillePro - SOLIDWORKS 2025 build helper v3'
Write-Host '============================================================'
Write-Host ''
Write-Host '[1/4] Locating SOLIDWORKS interop assemblies...'

function Find-InteropFile([string]$name) {
    $candidates = New-Object System.Collections.Generic.List[string]

    $pf = ${env:ProgramFiles}
    $pf86 = ${env:ProgramFiles(x86)}
    if ($pf) {
        $candidates.Add((Join-Path $pf 'SOLIDWORKS Corp\SOLIDWORKS\api\redist'))
        $candidates.Add((Join-Path $pf 'SOLIDWORKS Corp\SOLIDWORKS\api\redist\CLR4'))
        $candidates.Add((Join-Path $pf 'SOLIDWORKS Corp\SOLIDWORKS 2025\api\redist'))
        $candidates.Add((Join-Path $pf 'SOLIDWORKS Corp\SOLIDWORKS 2025\api\redist\CLR4'))
    }
    if ($pf86) {
        $candidates.Add((Join-Path $pf86 'SOLIDWORKS Corp\SOLIDWORKS\api\redist'))
        $candidates.Add((Join-Path $pf86 'SOLIDWORKS Corp\SOLIDWORKS 2025\api\redist'))
    }

    $regPaths = @(
      'HKLM:\SOFTWARE\SolidWorks\SOLIDWORKS 2025\Setup',
      'HKLM:\SOFTWARE\WOW6432Node\SolidWorks\SOLIDWORKS 2025\Setup'
    )
    foreach ($rp in $regPaths) {
        try {
            $p = Get-ItemProperty -Path $rp -ErrorAction Stop
            foreach ($prop in @('SolidWorksFolder','InstallDir','Path')) {
                $base = $p.$prop
                if ($base) {
                    $candidates.Add($base)
                    $candidates.Add((Join-Path $base 'api\redist'))
                    $candidates.Add((Join-Path $base 'api\redist\CLR4'))
                }
            }
        } catch { }
    }

    foreach ($dir in ($candidates | Select-Object -Unique)) {
        if ($dir -and (Test-Path -LiteralPath $dir)) {
            $p = Join-Path $dir $name
            if (Test-Path -LiteralPath $p) { return (Resolve-Path -LiteralPath $p).Path }
        }
    }

    $roots = @()
    if ($pf) { $roots += (Join-Path $pf 'SOLIDWORKS Corp') }
    if ($pf86) { $roots += (Join-Path $pf86 'SOLIDWORKS Corp') }
    if ($env:WINDIR) { $roots += (Join-Path $env:WINDIR 'Microsoft.NET\assembly') }

    foreach ($r in $roots) {
        if (Test-Path -LiteralPath $r) {
            try {
                $hit = Get-ChildItem -LiteralPath $r -Filter $name -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($hit) { return $hit.FullName }
            } catch { }
        }
    }
    return $null
}

$sld = Find-InteropFile 'SolidWorks.Interop.sldworks.dll'
$swc = Find-InteropFile 'SolidWorks.Interop.swconst.dll'
$swp = Find-InteropFile 'SolidWorks.Interop.swpublished.dll'

if (-not $sld -or -not $swc -or -not $swp) {
    Write-Host ''
    Write-Host 'ERROR: SOLIDWORKS interop DLLs were not found automatically.' -ForegroundColor Red
    Write-Host ''
    Write-Host 'Please search This PC for these three files:'
    Write-Host '  SolidWorks.Interop.sldworks.dll'
    Write-Host '  SolidWorks.Interop.swconst.dll'
    Write-Host '  SolidWorks.Interop.swpublished.dll'
    Write-Host ''
    Write-Host 'Typical folder:'
    Write-Host '  C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist'
    Write-Host ''
    Write-Host 'You can also run this script with explicit paths, for example:'
    Write-Host '  powershell -ExecutionPolicy Bypass -File .\build_manual.ps1'
    exit 1
}

Write-Host ('  sldworks   : ' + $sld)
Write-Host ('  swconst    : ' + $swc)
Write-Host ('  swpublished: ' + $swp)
Write-Host ''

Write-Host '[2/4] Locating MSBuild...'
$msbuild = $null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $found = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null | Select-Object -First 1
    if ($found) { $msbuild = $found }
}
if (-not $msbuild) {
    $fallbacks = @(
      "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe",
      "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    )
    foreach ($f in $fallbacks) { if (Test-Path -LiteralPath $f) { $msbuild = $f; break } }
}
if (-not $msbuild) {
    Write-Host 'ERROR: MSBuild was not found.' -ForegroundColor Red
    Write-Host 'Install Visual Studio 2022 Build Tools with .NET desktop build tools.'
    exit 2
}
Write-Host ('  MSBuild: ' + $msbuild)
Write-Host ''

Write-Host '[3/4] Building x64 Release...'
$proj = Join-Path $root 'src\SpeakerGrillePro.csproj'
$args = @(
    $proj,
    '/t:Rebuild',
    '/p:Configuration=Release',
    '/p:Platform=AnyCPU',
    "/p:SldWorksInterop=$sld",
    "/p:SwConstInterop=$swc",
    "/p:SwPublishedInterop=$swp",
    '/v:minimal'
)
& $msbuild @args
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'BUILD FAILED.' -ForegroundColor Red
    Write-Host 'Please send me the complete output beginning with the first error CSxxxx.'
    exit 3
}

$dll = Join-Path $root 'bin\SpeakerGrillePro.dll'
if (-not (Test-Path -LiteralPath $dll)) {
    Write-Host 'ERROR: Build reported success but bin\SpeakerGrillePro.dll is missing.' -ForegroundColor Red
    exit 4
}

Write-Host ''
Write-Host ('[4/4] Build complete: ' + $dll) -ForegroundColor Green
Write-Host 'Next: right-click install_admin.bat and choose Run as administrator.'
