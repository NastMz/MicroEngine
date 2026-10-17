param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $root
try {
    if (-not $NoBuild) {
        dotnet restore checks/Label.NativeProbe/Label.NativeProbe.csproj --configfile NuGet.config -p:NuGetAudit=false -m:1
        if ($LASTEXITCODE -ne 0) { throw 'Label probe restore failed.' }
        dotnet build checks/Label.NativeProbe/Label.NativeProbe.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw 'Label probe build failed.' }
    }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = 'dotnet'
    $info.Arguments = '"' + (Join-Path $root 'checks/Label.NativeProbe/bin/Release/net10.0/Label.NativeProbe.dll') + '"'
    $info.WorkingDirectory = $root
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw 'Label probe did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(15000)) { $process.Kill(); $process.WaitForExit(); throw 'Label probe timed out.' }
        $output = $stdout.Result + $stderr.Result
        if ($process.ExitCode -ne 0 -or $output -notmatch 'labels:baseline:0;pixels:([1-9][0-9]+)' -or $output -notmatch 'labels:closed' -or $output -notmatch 'Window closed successfully') {
            throw "Native labels were not proved.`n$output"
        }
        $pixels = [regex]::Match($output, 'labels:baseline:0;pixels:([1-9][0-9]+)').Groups[1].Value
        Write-Output ('PASS: native label RGB readback, ' + $pixels + ' glyph pixels, clean close.')
    }
    finally { $process.Dispose() }
}
finally { Pop-Location }
