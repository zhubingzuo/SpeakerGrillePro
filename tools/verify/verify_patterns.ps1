$ErrorActionPreference = 'Stop'

# Geometric regression harness for the 8 hole patterns.
# Compiles tools\verify\Harness.cs and runs it against the freshly built add-in.
# See Harness.cs for what is and is not covered, and HANDOFF.md "Test commands / 3".

$here = $PSScriptRoot                                  # tools\verify
$repo = Split-Path -Parent (Split-Path -Parent $here)  # repository root
$bin = Join-Path $repo 'bin'
$dll = Join-Path $bin 'SpeakerGrillePro.dll'
$src = Join-Path $here 'Harness.cs'
$obj = Join-Path $here 'obj'
$exe = Join-Path $obj 'harness.exe'

Write-Host '============================================================'
Write-Host 'SpeakerGrillePro - geometric pattern regression harness'
Write-Host '============================================================'
Write-Host ''

if (-not (Test-Path -LiteralPath $src)) {
    Write-Host ('ERROR: ' + $src + ' is missing.') -ForegroundColor Red
    exit 2
}
if (-not (Test-Path -LiteralPath $dll)) {
    Write-Host 'ERROR: bin\SpeakerGrillePro.dll was not found.' -ForegroundColor Red
    Write-Host 'Build the add-in first: run build.bat, or 一键安装.bat (which also registers it).'
    exit 2
}

# The build scripts and the installer copy these three assemblies next to the add-in, so no
# separate SOLIDWORKS discovery is needed here.
$interop = @(
    'SolidWorks.Interop.sldworks.dll',
    'SolidWorks.Interop.swconst.dll',
    'SolidWorks.Interop.swpublished.dll'
)
$refs = New-Object System.Collections.Generic.List[string]
foreach ($name in $interop) {
    $path = Join-Path $bin $name
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Host ('ERROR: ' + $name + ' was not found in bin\.') -ForegroundColor Red
        Write-Host 'Run build.bat or 一键安装.bat first: it copies the SOLIDWORKS interop assemblies there.'
        exit 2
    }
    $refs.Add($path)
}

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $csc)) {
    Write-Host 'ERROR: the .NET Framework 4.x C# compiler (csc.exe) was not found.' -ForegroundColor Red
    exit 2
}

if (-not (Test-Path -LiteralPath $obj)) { New-Item -ItemType Directory -Path $obj | Out-Null }

Write-Host ('repository : ' + $repo)
Write-Host ('add-in     : ' + $dll)
Write-Host ('compiler   : ' + $csc)
Write-Host ('harness    : ' + $exe)
Write-Host ''

# The harness loads the add-in with Assembly.LoadFrom, and the CLR resolves the add-in's
# references from the harness directory, so the interop assemblies must sit beside it.
foreach ($name in $interop) {
    Copy-Item -LiteralPath (Join-Path $bin $name) -Destination (Join-Path $obj $name) -Force
}

Write-Host '[1/2] Compiling the harness...'
$cscArgs = @('/nologo', '/target:exe', '/platform:x64', ('/out:' + $exe))
foreach ($rf in $refs) { $cscArgs += ('/reference:' + $rf) }
# csc requires every option to precede the source file.
$cscArgs += $src
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'Compile FAILED.' -ForegroundColor Red
    exit 3
}
if (-not (Test-Path -LiteralPath $exe)) {
    Write-Host 'Compile reported success but harness.exe was not created.' -ForegroundColor Red
    exit 3
}
Write-Host ''

Write-Host '[2/2] Checking every pattern. The cut step is not simulated, so each run stops after'
Write-Host '      the holes are emitted - that is expected and does not affect the measurement.'
Write-Host ''

& $exe $dll
$rc = $LASTEXITCODE

Write-Host ''
if ($rc -eq 0) {
    Write-Host 'RESULT: PASS' -ForegroundColor Green
} else {
    Write-Host ('RESULT: FAIL (exit ' + $rc + ')') -ForegroundColor Red
}
Write-Host ''
Write-Host 'Note: the add-in appends its own diagnostics to bin\SpeakerGrillePro_runtime.log during'
Write-Host '      the run. That file is git-ignored and can be cleared freely.'
exit $rc
