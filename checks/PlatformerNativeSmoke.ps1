param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $root
try {
    if (-not $NoBuild) {
        dotnet restore checks/Platformer.NativeProbe/Platformer.NativeProbe.csproj --configfile NuGet.config -p:NuGetAudit=false -m:1
        if ($LASTEXITCODE -ne 0) { throw 'Platformer probe restore failed.' }
        dotnet build checks/Platformer.NativeProbe/Platformer.NativeProbe.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw 'Platformer probe build failed.' }
    }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = 'dotnet'
    $info.Arguments = '"' + (Join-Path $root 'checks/Platformer.NativeProbe/bin/Release/net10.0/Platformer.NativeProbe.dll') + '"'
    $info.WorkingDirectory = $root
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw 'Platformer probe did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(30000)) { $process.Kill(); $process.WaitForExit(); throw 'Platformer probe timed out.' }
        $output = $stdout.Result + $stderr.Result
        if ($process.ExitCode -ne 0 -or $output -notmatch 'platformer:course:[0-9]+:verified' -or $output -notmatch 'platformer:closed' -or $output -notmatch 'platformer:reset:verified' -or $output -notmatch 'platformer:campaign:two-levels:verified' -or $output -notmatch 'platformer:patrol:poses-contact-retry-avoidance:verified' -or $output -notmatch 'Window closed successfully') {
            throw "Native platformer goal/reset was not proved.`n$output"
        }
        Write-Output 'PASS: real Kenney PNG/atlas pixels, budget isolation, full course/coins/camera/reset/replay and clean close.'
    }
    finally { $process.Dispose() }
}
finally { Pop-Location }

# The production CLI is also tested; scripted gameplay remains confined to the separate probe.
$gameDll = Join-Path $root 'examples/Platformer/bin/Release/net10.0/Platformer.dll'
$cases = @(
    @{ Args = '--smoke 8'; Exit = 0; Marker = 'outcome:Stopped;frames:8'; Native = $true },
    @{ Args = '--smoke 8 --fail-update'; Exit = 1; Marker = 'Requested update failure'; Native = $true },
    @{ Args = '--unknown'; Exit = 2; Marker = 'Usage: Platformer'; Native = $false },
    @{ Args = '--smoke 0'; Exit = 2; Marker = 'Usage: Platformer'; Native = $false },
    @{ Args = '--smoke 601'; Exit = 2; Marker = 'Usage: Platformer'; Native = $false },
    @{ Args = '--smoke'; Exit = 2; Marker = 'Usage: Platformer'; Native = $false },
    @{ Args = '--fail-update --fail-update'; Exit = 2; Marker = 'Usage: Platformer'; Native = $false }
)
foreach ($case in $cases) {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = 'dotnet'; $info.Arguments = '"' + $gameDll + '" ' + $case.Args
    $info.WorkingDirectory = $root; $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process; $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw 'Game did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(30000)) { $process.Kill(); $process.WaitForExit(); throw 'Game timed out.' }
        $output = $stdout.Result + $stderr.Result
        if ($process.ExitCode -ne $case.Exit -or $output -notmatch [regex]::Escape($case.Marker)) { throw "CLI failure $($case.Args): $output" }
        if ($case.Native -and $output -notmatch 'Window closed successfully') { throw "Native close missing: $output" }
        if (-not $case.Native -and $output -match 'Initialized.*raylib') { throw "Invalid CLI started native state: $output" }
    }
    finally { $process.Dispose() }
}
Write-Output 'PASS: Platformer production CLI/lifecycle seven cases, exact exits 0/1/2.'
