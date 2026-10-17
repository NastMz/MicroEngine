param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $root
try {
    if (-not $NoBuild) {
        dotnet restore examples/MapExplorer/MapExplorer.csproj --configfile NuGet.config -m:1
        if ($LASTEXITCODE -ne 0) { throw 'Consumer restore failed.' }
        dotnet build examples/MapExplorer/MapExplorer.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw 'Consumer build failed.' }
    }

    function Invoke-Probe([string]$arguments, [int]$expectedExit) {
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = 'dotnet'
        $info.Arguments = '"' + (Join-Path $root 'examples/MapExplorer/bin/Release/net10.0/MapExplorer.dll') + '" ' + $arguments
        $info.WorkingDirectory = $root
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        $process = New-Object Diagnostics.Process
        $process.StartInfo = $info
        try {
            if (-not $process.Start()) { throw 'Probe did not start.' }
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(15000)) {
                $process.Kill()
                $process.WaitForExit()
                throw "Probe timed out: $arguments"
            }
            $output = $stdout.Result + $stderr.Result
            if ($process.ExitCode -ne $expectedExit) { throw "Exit $($process.ExitCode), expected $expectedExit for $arguments`n$output" }
            return $output
        }
        finally { $process.Dispose() }
    }

    $normal = Invoke-Probe '--smoke 60' 0
    if ($normal -notmatch 'outcome:Stopped;frames:60;updates:([1-9][0-9]*)' -or $normal -notmatch 'Window closed successfully') {
        throw "Normal smoke did not prove update, presentation and native close (desktop focus required).`n$normal"
    }
    $failure = Invoke-Probe '--smoke 60 --fail-update' 1
    if ($failure -notmatch 'outcome:Failed;' -or $failure -notmatch 'primary:ApplicationException:Requested update failure\.' -or $failure -notmatch 'Window closed successfully') {
        throw "Failure smoke did not prove reported callback failure and native close.`n$failure"
    }
    foreach ($arguments in @('--unknown', '--smoke 0', '--smoke NaN', '--smoke 601', '--smoke 1 --smoke 2', '--smoke', '--fail-update --fail-update', '--smoke --fail-update', '--smoke NaN --fail-update')) {
        $invalid = Invoke-Probe $arguments 2
        if ($invalid -notmatch 'Usage: MapExplorer' -or $invalid -match 'Initializing raylib') {
            throw "Invalid arguments did not fail before native acquisition: $arguments"
        }
    }
    $reverse = Invoke-Probe '--fail-update --smoke 60' 1
    if ($reverse -notmatch 'primary:ApplicationException:Requested update failure\.' -or $reverse -notmatch 'Window closed successfully') {
        throw "Reverse flag order did not preserve requested failure/cleanup.`n$reverse"
    }
    Write-Output 'PASS: native60 presentations/updates/close; callback failure/close in both flag orders; nine invalid CLI cases, exact exits0/1/2.'
}
finally { Pop-Location }
