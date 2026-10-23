param()
$ErrorActionPreference='Stop'
$assets=Join-Path $PSScriptRoot '../examples/Platformer/Assets/Audio'
$m=Get-Content (Join-Path $assets 'provenance.json') -Raw|ConvertFrom-Json
if($m.license -ne 'CC0-1.0' -or @($m.files).Count -ne 4 -or $m.embedded_license){throw 'Unexpected audio provenance.'}
foreach($file in $m.files){if((Get-FileHash (Join-Path $assets $file.file)).Hash.ToLowerInvariant() -ne $file.sha256){throw "Original audio hash mismatch: $($file.file)"}}
Write-Output 'PASS: four original CC0-declared WAV files match provenance; no embedded license is fabricated.'
