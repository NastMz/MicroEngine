param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('MicroEngine CL1 ' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($fixture) | Out-Null
Push-Location $root
try {
    if (-not $NoBuild) {
        dotnet restore examples/TurnExplorer/TurnExplorer.csproj --configfile NuGet.config -p:NuGetAudit=false -m:1
        if ($LASTEXITCODE -ne 0) { throw 'Consumer restore failed.' }
        dotnet build examples/TurnExplorer/TurnExplorer.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
        if ($LASTEXITCODE -ne 0) { throw 'Consumer build failed.' }
    }
    function Invoke-Probe([string]$arguments, [int]$expectedExit) {
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = 'dotnet'
        $info.Arguments = '"' + (Join-Path $root 'examples/TurnExplorer/bin/Release/net10.0/TurnExplorer.dll') + '" ' + $arguments
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
                $process.Kill(); $process.WaitForExit(); throw 'Persistence probe timed out.'
            }
            $output = $stdout.Result + $stderr.Result
            if ($process.ExitCode -ne $expectedExit) { throw "Exit $($process.ExitCode), expected $expectedExit for $arguments`n$output" }
            return $output
        }
        finally { $process.Dispose() }
    }
    function Require([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
    function SameBytes([string]$path, [byte[]]$expected) {
        Require (([Convert]::ToBase64String([IO.File]::ReadAllBytes($path))) -eq [Convert]::ToBase64String($expected)) 'Checkpoint bytes changed unexpectedly.'
    }

    $fresh = Join-Path $fixture 'fresh.bin'
    $normal = Invoke-Probe ('--smoke 2 --save-new "' + $fresh + '"') 0
    Require ($normal -match 'outcome:Stopped;frames:2;' -and $normal -match 'checkpoint:saved') 'Default save did not succeed.'
    Require ($normal.IndexOf('Window closed successfully') -ge 0 -and $normal.IndexOf('Window closed successfully') -lt $normal.IndexOf('checkpoint:saved')) 'Save was not after native close.'
    SameBytes $fresh ([byte[]](2,0,0,0, 0,0,0,0, 0,0,32,66, 0,0,36,67, 0,0,0,0, 0,0,0,0))

    $seed = Join-Path $fixture 'seed.bin'
    $resumed = Join-Path $fixture 'resumed.bin'
    [byte[]]$state = 1,0,0,0, 1,0,0,0, 0,0,26,68, 0,0,164,67, 7,0,0,0, 3,0,0,0
    [IO.File]::WriteAllBytes($seed, $state)
    $loaded = Invoke-Probe ('--load "' + $seed + '" --save-new "' + $resumed + '" --smoke 2') 0
    Require ($loaded -match 'checkpoint:loaded' -and $loaded -match 'checkpoint:saved' -and $loaded -match 'Window closed successfully') 'Process restart failed.'
    Require ($loaded.IndexOf('checkpoint:loaded') -lt $loaded.IndexOf('Initializing raylib')) 'Load was not before native acquisition.'
    [byte[]]$canonical = $state.Clone(); $canonical[0] = 2
    SameBytes $seed $state; SameBytes $resumed $canonical

    $completed = Join-Path $fixture 'completed.bin'
    $completedResumed = Join-Path $fixture 'completed-resumed.bin'
    [byte[]]$done = 2,0,0,0, 0,0,0,0, 0,0,155,67, 0,0,36,67, 9,0,0,0, 0,0,0,0
    [IO.File]::WriteAllBytes($completed, $done)
    $restart = Invoke-Probe ('--load "' + $completed + '" --save-new "' + $completedResumed + '" --smoke 2') 0
    Require ($restart -match 'checkpoint:loaded' -and $restart -match 'checkpoint:saved' -and $restart -match 'Window closed successfully' -and $restart -notmatch 'encounter:completed|menu:open|turn:phase:') 'Completed restart reopened or fabricated a completion event.'
    SameBytes $completed $done; SameBytes $completedResumed $done
    $completedExisting = Invoke-Probe ('--load "' + $completed + '" --save-new "' + $completed + '" --smoke 2') 1
    Require ($completedExisting -match 'checkpoint:save-failed:IOException:' -and $completedExisting -match 'Window closed successfully') 'Completed existing save was replaced.'
    SameBytes $completed $done

    $never = Join-Path $fixture 'never.bin'
    $missing = Join-Path $fixture 'missing.bin'
    $loadFailure = Invoke-Probe ('--load "' + $missing + '" --save-new "' + $never + '" --smoke 2') 1
    Require ($loadFailure -match 'checkpoint:load-failed:FileNotFoundException:' -and $loadFailure -notmatch 'Initializing raylib' -and -not [IO.File]::Exists($never)) 'Missing load was hidden or had effects.'
    $bad = Join-Path $fixture 'bad.bin'
    foreach ($bytes in @(([byte[]](9,8,7)), (New-Object byte[] 25))) {
        [IO.File]::WriteAllBytes($bad, $bytes)
        $corrupt = Invoke-Probe ('--load "' + $bad + '" --save-new "' + $never + '" --smoke 2') 1
        Require ($corrupt -match 'checkpoint:load-failed:FormatException:' -and $corrupt -notmatch 'Initializing raylib' -and -not [IO.File]::Exists($never)) 'Corrupt load had effects.'
        SameBytes $bad $bytes
    }
    foreach ($kind in @('v1-completed', 'completed-menu', 'completed-phase', 'unknown-version', 'unknown-flag')) {
        [byte[]]$bytes = $done.Clone()
        switch ($kind) {
            'v1-completed' { $bytes[0] = 1 }
            'completed-menu' { $bytes[16] = 11 }
            'completed-phase' { $bytes[20] = 1 }
            'unknown-version' { $bytes[0] = 3 }
            'unknown-flag' { $bytes[16] = 17 }
        }
        [IO.File]::WriteAllBytes($bad, $bytes)
        $invalidState = Invoke-Probe ('--load "' + $bad + '" --save-new "' + $never + '" --smoke 2') 1
        Require ($invalidState -match 'checkpoint:load-failed:FormatException:' -and $invalidState -notmatch 'Initializing raylib' -and -not [IO.File]::Exists($never)) 'Invalid version/outcome state had effects.'
        SameBytes $bad $bytes
    }

    $existing = Invoke-Probe ('--load "' + $seed + '" --save-new "' + $seed + '" --smoke 2') 1
    Require ($existing -match 'checkpoint:save-failed:IOException:' -and $existing -match 'Window closed successfully') 'Existing destination failure was hidden.'
    SameBytes $seed $state
    $missingParent = Join-Path $fixture 'absent'
    $saveFailure = Invoke-Probe ('--save-new "' + (Join-Path $missingParent 'save.bin') + '" --smoke 2') 1
    Require ($saveFailure -match 'checkpoint:save-failed:DirectoryNotFoundException:' -and $saveFailure -match 'Window closed successfully' -and -not [IO.Directory]::Exists($missingParent)) 'Missing save parent failure was hidden.'
    $failure = Invoke-Probe ('--save-new "' + $never + '" --smoke 60 --fail-update') 1
    Require ($failure -match 'outcome:Failed;' -and $failure -match 'Window closed successfully' -and $failure -notmatch 'checkpoint:saved' -and -not [IO.File]::Exists($never)) 'Runtime failure saved or leaked native state.'

    foreach ($arguments in @('--load', '--save-new', '--load --smoke 1', '--save-new " "', '--load a --load b', '--save-new a --save-new b',
        ('--smoke --load "' + $missing + '"'), ('--smoke --save-new "' + $never + '"'), '--smoke --fail-update')) {
        $invalid = Invoke-Probe $arguments 2
        Require ($invalid -match 'Usage: TurnExplorer' -and $invalid -match '--save-new PATH' -and $invalid -notmatch 'Initializing raylib|checkpoint:loaded|checkpoint:saved' -and -not [IO.File]::Exists($never)) 'New invalid CLI had effects or lacked usage.'
    }
    Require (([IO.Directory]::GetFiles($fixture, '.checkpoint-*.tmp')).Length -eq 0) 'Temporary file residue.'
    Write-Output 'PASS: 24 persistence CLI/native cases; exact exits0/1/2, v1-compatible/v2-completed restart, load before acquire/save after close, no overwrite/failure save.'
}
finally {
    Pop-Location
    $absolute = [IO.Path]::GetFullPath($fixture)
    $parent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $absolute.StartsWith($parent + 'MicroEngine CL1 ', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup path.' }
    foreach ($file in [IO.Directory]::GetFiles($absolute)) { [IO.File]::Delete($file) }
    [IO.Directory]::Delete($absolute)
}
