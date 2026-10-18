param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $root
try {
    if (-not $NoBuild) {
        dotnet restore checks/Rpg.NativeProbe/Rpg.NativeProbe.csproj --configfile NuGet.config -p:NuGetAudit=false -m:1
        if ($LASTEXITCODE -ne 0) { throw 'RPG restore failed.' }
        dotnet build checks/Rpg.NativeProbe/Rpg.NativeProbe.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw 'RPG build failed.' }
    }
    function Invoke-Captured([string]$relativeDll, [string]$arguments, [int]$expectedExit) {
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = 'dotnet'; $info.Arguments = '"' + (Join-Path $root $relativeDll) + '" ' + $arguments
        $info.WorkingDirectory = $root; $info.UseShellExecute = $false; $info.CreateNoWindow = $true
        $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
        $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
        $process = New-Object Diagnostics.Process; $process.StartInfo = $info
        try {
            if (-not $process.Start()) { throw 'RPG process did not start.' }
            $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(30000)) { $process.Kill(); $process.WaitForExit(); throw 'RPG process timed out.' }
            $output = $stdout.Result + $stderr.Result
            if ($process.ExitCode -ne $expectedExit) { throw "Unexpected RPG exit $($process.ExitCode): $output" }
            return $output
        }
        finally { $process.Dispose() }
    }
    $probe = Invoke-Captured 'checks/Rpg.NativeProbe/bin/Release/net10.0/Rpg.NativeProbe.dll' '' 0
    foreach ($marker in @('rpg:Victory:verified','rpg:Defeat:verified','rpg:Retreated:verified','rpg:four-images:verified','rpg:closed','Window closed successfully')) {
        if ($probe -notmatch [regex]::Escape($marker)) { throw "RPG proof missing $marker`: $probe" }
    }
    Write-Output 'PASS: public RPG exploration/portals, three outcomes, resume/retry, actual art/HP/menu/result pixels, four shared images and native close.'
    $cases = @(
        @{ Args = '--smoke 20'; Exit = 0; Marker = 'outcome:Stopped'; Native = $true },
        @{ Args = '--fail-update'; Exit = 1; Marker = 'primary:ApplicationException:Requested update failure.'; Native = $true },
        @{ Args = '--unknown'; Exit = 2; Marker = 'Usage: Rpg'; Native = $false },
        @{ Args = '--smoke 0'; Exit = 2; Marker = 'Usage: Rpg'; Native = $false },
        @{ Args = '--smoke 601'; Exit = 2; Marker = 'Usage: Rpg'; Native = $false },
        @{ Args = '--smoke'; Exit = 2; Marker = 'Usage: Rpg'; Native = $false },
        @{ Args = '--fail-update --fail-update'; Exit = 2; Marker = 'Usage: Rpg'; Native = $false }
    )
    foreach ($case in $cases) {
        $output = Invoke-Captured 'examples/Rpg/bin/Release/net10.0/Rpg.dll' $case.Args $case.Exit
        if ($output -notmatch [regex]::Escape($case.Marker)) { throw "RPG CLI marker missing: $output" }
        if ($case.Native -and $output -notmatch 'Window closed successfully') { throw "Native cleanup missing: $output" }
        if (-not $case.Native -and $output -match 'Initializing raylib') { throw "Invalid CLI acquired native state: $output" }
    }
    Write-Output 'PASS: RPG production CLI/lifecycle seven cases, exact exits 0/1/2.'
}
finally { Pop-Location }
