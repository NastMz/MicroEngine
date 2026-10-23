param([switch]$NoBuild)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $root
try {
    if(-not $NoBuild){
        dotnet restore checks/Audio.NativeProbe/Audio.NativeProbe.csproj --configfile NuGet.config -p:NuGetAudit=false -m:1
        if($LASTEXITCODE -ne 0){throw 'Audio restore failed.'}
        dotnet build checks/Audio.NativeProbe/Audio.NativeProbe.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
        if($LASTEXITCODE -ne 0){throw 'Audio build failed.'}
    }
    dotnet checks/Audio.NativeProbe/bin/Release/net10.0/Audio.NativeProbe.dll
    if($LASTEXITCODE -eq 3){throw 'Audio hardware unavailable: native sound proof incomplete.'}
    if($LASTEXITCODE -ne 0){throw 'Audio native proof failed.'}
    Write-Output 'PASS: standalone original WAV decode/load/play requests, stop, close and reopen; no window. Audibility is not asserted.'
}
finally {Pop-Location}
