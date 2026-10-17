param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$fixture = [IO.Directory]::CreateDirectory((Join-Path ([IO.Path]::GetTempPath()) ('MicroEngine TURN2 ' + [guid]::NewGuid().ToString('N')))).FullName
Push-Location $root
try {
    if (-not $NoBuild) {
        dotnet restore checks/TurnOutcome.NativeProbe/TurnOutcome.NativeProbe.csproj --configfile NuGet.config -p:NuGetAudit=false -m:1
        if ($LASTEXITCODE -ne 0) { throw 'Turn outcome probe restore failed.' }
        dotnet build checks/TurnOutcome.NativeProbe/TurnOutcome.NativeProbe.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw 'Turn outcome probe build failed.' }
    }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = 'dotnet'
    $info.Arguments = '"' + (Join-Path $root 'checks/TurnOutcome.NativeProbe/bin/Release/net10.0/TurnOutcome.NativeProbe.dll') + '"'
    $info.Arguments += ' "' + $fixture + '"'
    $info.WorkingDirectory = $root
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw 'Turn outcome probe did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(15000)) { $process.Kill(); $process.WaitForExit(); throw 'Turn outcome probe timed out.' }
        $output = $stdout.Result + $stderr.Result
        if ($process.ExitCode -ne 0 -or $output -notmatch 'turn-outcome:native:verified' -or $output -notmatch 'turn-outcome:closed' -or $output -notmatch 'turn-outcome:durable:verified' -or $output -notmatch 'Window closed successfully') {
            throw "Native turn outcome and durable restart were not proved.`n$output"
        }
        Write-Output 'PASS: native four-turn completion/RGB/closed-menu and24byte v2 save-reload, clean close.'
    }
    finally { $process.Dispose() }
}
finally {
    Pop-Location
    $file = Join-Path $fixture 'completed.bin'
    if ([IO.File]::Exists($file)) { [IO.File]::Delete($file) }
    if ([IO.Directory]::GetFileSystemEntries($fixture).Length -eq 0) { [IO.Directory]::Delete($fixture) }
}
