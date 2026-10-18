param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $root
try {
    if (-not $NoBuild) {
        dotnet restore checks/Goal.NativeProbe/Goal.NativeProbe.csproj --configfile NuGet.config -p:NuGetAudit=false -m:1
        if ($LASTEXITCODE -ne 0) { throw 'Goal probe restore failed.' }
        dotnet build checks/Goal.NativeProbe/Goal.NativeProbe.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw 'Goal probe build failed.' }
    }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = 'dotnet'
    $info.Arguments = '"' + (Join-Path $root 'checks/Goal.NativeProbe/bin/Release/net10.0/Goal.NativeProbe.dll') + '"'
    $info.WorkingDirectory = $root
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw 'Goal probe did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(15000)) { $process.Kill(); $process.WaitForExit(); throw 'Goal probe timed out.' }
        $output = $stdout.Result + $stderr.Result
        if ($process.ExitCode -ne 0 -or $output -notmatch 'goal:course:verified' -or $output -notmatch 'goal:closed' -or $output -notmatch 'goal:reset:verified' -or $output -notmatch 'Window closed successfully') {
            throw "Native platformer goal/reset was not proved.`n$output"
        }
        Write-Output 'PASS: native obstacle-to-goal course/reset/replay, goal/HUD/avatar RGB and clean close.'
    }
    finally { $process.Dispose() }
}
finally { Pop-Location }
