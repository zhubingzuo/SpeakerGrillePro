$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

Write-Host '============================================================'
Write-Host 'SpeakerGrillePro - SOLIDWORKS 2025 build helper v3'
Write-Host '============================================================'
Write-Host ''
Write-Host '[1/4] Locating SOLIDWORKS interop assemblies...'

function Get-FixedDrives {
    $drives = @()
    try {
        $drives = @(Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue |
                    Where-Object { $_.Root -match '^[A-Za-z]:\\$' } |
                    Select-Object -ExpandProperty Root)
    } catch { $drives = @() }
    if ($drives.Count -eq 0) { $drives = @('C:\') }
    return $drives
}

function Find-InteropFile([string]$name) {
    $candidates = New-Object System.Collections.Generic.List[string]
    $searchBases = New-Object System.Collections.Generic.List[string]

    # Every fixed drive, in case SOLIDWORKS is not on the system drive.
    foreach ($d in (Get-FixedDrives)) {
        foreach ($pfDir in @('Program Files', 'Program Files (x86)')) {
            $corp = Join-Path $d (Join-Path $pfDir 'SOLIDWORKS Corp')
            $searchBases.Add($corp)
            foreach ($sub in @('SOLIDWORKS', 'SOLIDWORKS 2025')) {
                $candidates.Add((Join-Path $corp ($sub + '\api\redist')))
                $candidates.Add((Join-Path $corp ($sub + '\api\redist\CLR4')))
                $candidates.Add((Join-Path $corp ($sub + '\api\redist\CLR4\64bit')))
            }
        }
        $candidates.Add((Join-Path $d 'SOLIDWORKS Corp\SOLIDWORKS\api\redist'))
    }
    if ($env:WINDIR) { $searchBases.Add((Join-Path $env:WINDIR 'Microsoft.NET\assembly')) }

    # Registry: any SOLIDWORKS release year, 64-bit and 32-bit views.
    foreach ($regRoot in @('HKLM:\SOFTWARE\SolidWorks', 'HKLM:\SOFTWARE\WOW6432Node\SolidWorks')) {
        if (-not (Test-Path -LiteralPath $regRoot)) { continue }
        $years = @(Get-ChildItem -LiteralPath $regRoot -ErrorAction SilentlyContinue |
                   Where-Object { $_.PSChildName -like 'SOLIDWORKS 20*' })
        foreach ($year in $years) {
            $rp = Join-Path $year.PSPath 'Setup'
            try {
                $p = Get-ItemProperty -Path $rp -ErrorAction Stop
                foreach ($prop in @('SolidWorksFolder','InstallDir','Path')) {
                    $base = $p.$prop
                    if ($base) {
                        $candidates.Add($base)
                        $candidates.Add((Join-Path $base 'api\redist'))
                        $candidates.Add((Join-Path $base 'api\redist\CLR4'))
                        $searchBases.Add($base)
                    }
                }
            } catch { }
        }
    }

    foreach ($dir in ($candidates | Select-Object -Unique)) {
        if ($dir -and (Test-Path -LiteralPath $dir)) {
            $p = Join-Path $dir $name
            if (Test-Path -LiteralPath $p) { return (Resolve-Path -LiteralPath $p).Path }
        }
    }

    foreach ($r in ($searchBases | Select-Object -Unique)) {
        if (Test-Path -LiteralPath $r) {
            try {
                $hit = Get-ChildItem -LiteralPath $r -Filter $name -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($hit) { return $hit.FullName }
            } catch { }
        }
    }

    # Last resort: a copy a previous install left beside our own DLL.
    $localCopy = Join-Path (Join-Path $root 'bin') $name
    if (Test-Path -LiteralPath $localCopy) { return (Resolve-Path -LiteralPath $localCopy).Path }

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
$msbuildCandidates = New-Object System.Collections.Generic.List[string]
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $found = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null)
    foreach ($f in $found) { if ($f) { $msbuildCandidates.Add($f) } }
}
# The .NET Framework MSBuild tolerates a missing .NET 4.0 targeting pack (it falls back to
# the GAC reference assemblies), while newer VS MSBuild turns that same situation into the
# hard error MSB3644. Keep both so the helper works whether or not the targeting pack exists.
foreach ($f in @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe")) {
    if (Test-Path -LiteralPath $f) { $msbuildCandidates.Add($f) }
}
$msbuildCandidates = @($msbuildCandidates | Select-Object -Unique)
if ($msbuildCandidates.Count -eq 0) {
    Write-Host 'ERROR: MSBuild was not found.' -ForegroundColor Red
    Write-Host 'Install Visual Studio 2022 Build Tools with .NET desktop build tools.'
    exit 2
}

Write-Host '[3/4] Building x64 Release...'
$proj = Join-Path $root 'src\SpeakerGrillePro.csproj'
$built = $false
foreach ($msbuild in $msbuildCandidates) {
    Write-Host ('  MSBuild: ' + $msbuild)
    $msbuildArgs = @(
        $proj,
        '/t:Rebuild',
        '/p:Configuration=Release',
        '/p:Platform=AnyCPU',
        "/p:SldWorksInterop=$sld",
        "/p:SwConstInterop=$swc",
        "/p:SwPublishedInterop=$swp",
        '/v:minimal'
    )
    & $msbuild @msbuildArgs
    if ($LASTEXITCODE -eq 0) { $built = $true; break }
    Write-Host ''
    Write-Host ('  This MSBuild failed (exit ' + $LASTEXITCODE + '). Trying the next candidate...') -ForegroundColor Yellow
}
if (-not $built) {
    Write-Host ''
    Write-Host 'BUILD FAILED.' -ForegroundColor Red
    Write-Host 'Every MSBuild candidate failed. The usual cause is the missing .NET Framework 4.0'
    Write-Host 'reference assemblies (error MSB3644). Install the .NET Framework 4.x Developer Pack,'
    Write-Host 'or build with csc.exe instead (that is what the one-click installer does).'
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
