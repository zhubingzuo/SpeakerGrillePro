$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$log = Join-Path $root 'install_log.txt'
Start-Transcript -Path $log -Force | Out-Null

function Fail([string]$msg, [int]$code) {
    Write-Host ''
    Write-Host ('ERROR: ' + $msg) -ForegroundColor Red
    Write-Host ('Log: ' + $log)
    try { Stop-Transcript | Out-Null } catch {}
    exit $code
}

try {
    Write-Host '============================================================'
    Write-Host 'SpeakerGrillePro - SOLIDWORKS 2025 SP5 One-Click Installer v27.4 3Dconnexion Safe'
    Write-Host '============================================================'
    Write-Host ''

    $swExe = 'D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe'
    if (-not (Test-Path -LiteralPath $swExe)) { Fail "SOLIDWORKS was not found at $swExe" 10 }
    $swRoot = Split-Path -Parent $swExe
    Write-Host ('[1/5] SOLIDWORKS: ' + $swExe)

    # v27.4: registration/build must be performed with SOLIDWORKS closed.
    # Do NOT terminate it automatically: this prevents data loss and avoids changing
    # the permission context of the user's normal SOLIDWORKS session.
    $runningSw = Get-Process -Name 'SLDWORKS' -ErrorAction SilentlyContinue
    if ($runningSw) { Fail 'SOLIDWORKS is currently running. Save your work, close SOLIDWORKS, then run this installer again.' 9 }

    # v27.4 safety rule: never modify registration belonging to any other SOLIDWORKS add-in.
    # In particular, 3Dconnexion/3DxWare uses the same SolidWorks\AddIns registry area.

    Write-Host '[2/5] Locating SOLIDWORKS interop assemblies...'
    function Find-SwDll([string]$name) {
        $priority = @(
            (Join-Path $swRoot 'api\redist'),
            (Join-Path $swRoot 'api\redist\CLR4'),
            (Join-Path $swRoot 'api\redist\CLR4\64bit'),
            $swRoot
        )
        foreach ($dir in $priority) {
            if (Test-Path -LiteralPath $dir) {
                $p = Join-Path $dir $name
                if (Test-Path -LiteralPath $p) { return (Resolve-Path -LiteralPath $p).Path }
            }
        }
        $corpRoot = 'D:\Program Files\SOLIDWORKS Corp'
        if (Test-Path -LiteralPath $corpRoot) {
            $hit = Get-ChildItem -LiteralPath $corpRoot -Filter $name -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($hit) { return $hit.FullName }
        }
        return $null
    }

    $sld = Find-SwDll 'SolidWorks.Interop.sldworks.dll'
    $swc = Find-SwDll 'SolidWorks.Interop.swconst.dll'
    $swp = Find-SwDll 'SolidWorks.Interop.swpublished.dll'
    if (-not $sld -or -not $swc -or -not $swp) { Fail 'One or more SOLIDWORKS interop DLLs could not be found.' 11 }
    Write-Host ('  sldworks    = ' + $sld)
    Write-Host ('  swconst     = ' + $swc)
    Write-Host ('  swpublished = ' + $swp)

    Write-Host '[3/5] Locating .NET Framework C# compiler...'
    $cscCandidates = @(
        "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    )
    $csc = $cscCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $csc) { Fail '.NET Framework 4.x C# compiler csc.exe was not found.' 12 }
    Write-Host ('  CSC = ' + $csc)

    Write-Host '[4/5] Building SpeakerGrillePro with C# 5 / .NET Framework 4.x...'
    $bin = Join-Path $root 'bin'
    $runtimeLog = Join-Path $bin 'SpeakerGrillePro_runtime.log'
    Remove-Item -LiteralPath $runtimeLog -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path -LiteralPath $bin)) { New-Item -ItemType Directory -Path $bin | Out-Null }
    $dll = Join-Path $bin 'SpeakerGrillePro.dll'
    if (Test-Path -LiteralPath $dll) { Remove-Item -LiteralPath $dll -Force }

    $src1 = Join-Path $root 'src\SpeakerGrillePro.cs'
    $src2 = Join-Path $root 'src\Properties\AssemblyInfo.cs'
    $snk = Join-Path $root 'src\SpeakerGrillePro.snk'
    if (-not (Test-Path -LiteralPath $snk)) { Fail 'Strong-name key SpeakerGrillePro.snk was not found.' 13 }
    $args = @(
        '/nologo', '/target:library', '/platform:x64', '/optimize+', '/langversion:5',
        ('/out:"' + $dll + '"'),
        ('/keyfile:"' + $snk + '"'),
        '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll',
        ('/reference:"' + $sld + '"'),
        ('/reference:"' + $swc + '"'),
        ('/reference:"' + $swp + '"'),
        ('"' + $src1 + '"'), ('"' + $src2 + '"')
    )
    $compileOutput = & $csc $args 2>&1
    $compileOutput | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { Fail 'C# build failed. The complete compiler output is in install_log.txt.' 13 }
    if (-not (Test-Path -LiteralPath $dll)) { Fail 'Compiler finished but SpeakerGrillePro.dll was not created.' 14 }

    # v27.2 toolbar icon assets. SOLIDWORKS IconList/MainIconList require file paths,
    # so copy the multi-size speaker PNGs beside the built DLL.
    $iconSource = Join-Path $root 'src\Icons'
    $iconDest = Join-Path $bin 'Icons'
    if (-not (Test-Path -LiteralPath $iconSource)) { Fail 'Toolbar icon folder src\Icons was not found.' 14 }
    if (-not (Test-Path -LiteralPath $iconDest)) { New-Item -ItemType Directory -Path $iconDest | Out-Null }
    Copy-Item -LiteralPath (Join-Path $iconSource '*') -Destination $iconDest -Force
    Write-Host ('  Toolbar icons copied to: ' + $iconDest)

    Write-Host '[5/5] Preparing runtime dependencies and registering add-in...'
    $regasm = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
    if (-not (Test-Path -LiteralPath $regasm)) { Fail '64-bit RegAsm.exe was not found.' 15 }

    # RegAsm is a separate .NET process. It does NOT automatically probe the
    # SOLIDWORKS api\redist directory, so the add-in can compile successfully
    # yet registration can fail with RA0000 on SolidWorks.Interop.swpublished.
    # Put the exact interop assemblies used for compilation beside our DLL.
    $interopSources = @($sld, $swc, $swp)
    foreach ($interop in $interopSources) {
        $dest = Join-Path $bin ([System.IO.Path]::GetFileName($interop))
        Copy-Item -LiteralPath $interop -Destination $dest -Force
        Write-Host ('  Copied runtime dependency: ' + $dest)
    }

    # Copy any additional SOLIDWORKS Interop DLLs from the same redist folder.
    # This makes registration/runtime robust if one of the three primary DLLs
    # has an indirect SOLIDWORKS interop dependency on this installation.
    $redistDir = Split-Path -Parent $sld
    Get-ChildItem -LiteralPath $redistDir -Filter 'SolidWorks.Interop.*.dll' -File -ErrorAction SilentlyContinue | ForEach-Object {
        $dest = Join-Path $bin $_.Name
        if (-not (Test-Path -LiteralPath $dest)) {
            Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
            Write-Host ('  Copied extra dependency: ' + $dest)
        }
    }

    # The assembly is strong-name signed; v27 keeps the same trusted registration path. RegAsm is still run as a separate process so stderr output cannot be mistaken for a PowerShell terminating error.
    # registration succeeds. PowerShell 5.1 + ErrorActionPreference=Stop can
    # incorrectly turn that warning into a terminating error. Run RegAsm through
    # Start-Process, capture stdout/stderr separately, and trust only ExitCode.
    function Invoke-RegAsm([string[]]$Arguments, [string]$Tag) {
        $outFile = Join-Path $bin ("regasm_" + $Tag + "_out.txt")
        $errFile = Join-Path $bin ("regasm_" + $Tag + "_err.txt")
        Remove-Item -LiteralPath $outFile,$errFile -Force -ErrorAction SilentlyContinue
        $proc = Start-Process -FilePath $regasm -ArgumentList $Arguments -WorkingDirectory $bin -Wait -PassThru -NoNewWindow -RedirectStandardOutput $outFile -RedirectStandardError $errFile
        if (Test-Path -LiteralPath $outFile) { Get-Content -LiteralPath $outFile | ForEach-Object { if ($_){ Write-Host ('  ' + $_) } } }
        if (Test-Path -LiteralPath $errFile) { Get-Content -LiteralPath $errFile | ForEach-Object { if ($_){ Write-Host ('  ' + $_) -ForegroundColor Yellow } } }
        return $proc.ExitCode
    }

    [void](Invoke-RegAsm @('"' + $dll + '"','/unregister') 'unregister')
    $regExit = Invoke-RegAsm @('"' + $dll + '"','/codebase') 'register'
    if ($regExit -ne 0) { Fail ('COM registration failed. RegAsm exit code: ' + $regExit) 16 }

    # Verify both COM registration and the SOLIDWORKS add-in keys produced by
    # the [ComRegisterFunction] callback. This catches a "RegAsm succeeded but
    # SOLIDWORKS cannot see the add-in" situation before launching SOLIDWORKS.
    $addinGuid = '{7A88B123-7C5D-4B8C-9E2B-7E7314B42650}'
    $comKey = 'Registry::HKEY_CLASSES_ROOT\CLSID\' + $addinGuid
    $swKey = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\SolidWorks\Addins\' + $addinGuid
    $startupKey = 'Registry::HKEY_CURRENT_USER\Software\SolidWorks\AddInsStartup\' + $addinGuid
    if (-not (Test-Path $comKey)) { Fail 'RegAsm returned success, but the COM CLSID key was not created.' 17 }
    if (-not (Test-Path $swKey)) { Fail 'COM registration succeeded, but the SOLIDWORKS Addins registry key was not created.' 18 }
    if (-not (Test-Path $startupKey)) {
        New-Item -Path $startupKey -Force | Out-Null
        Set-ItemProperty -Path $startupKey -Name '(default)' -Value 1 -Type DWord -ErrorAction SilentlyContinue
    }
    Write-Host '  Registration verification: OK' -ForegroundColor Green

    Write-Host ''
    Write-Host 'SUCCESS: SpeakerGrillePro has been built and registered.' -ForegroundColor Green
    Write-Host ''
    Write-Host 'IMPORTANT: SOLIDWORKS will NOT be started by this installer.' -ForegroundColor Yellow
    Write-Host 'Close this installer, then start SOLIDWORKS normally from your usual shortcut.'
    Write-Host 'Do NOT use "Run as administrator" unless 3DxWare is also running elevated.'
    Write-Host 'This avoids the Windows permission mismatch that can disable a 3Dconnexion SpaceMouse.'
    Write-Host ('Installation log saved to: ' + $log)
    Stop-Transcript | Out-Null
    exit 0
}
catch {
    Fail $_.Exception.Message 99
}
