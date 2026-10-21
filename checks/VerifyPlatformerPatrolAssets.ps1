param()
$ErrorActionPreference='Stop'
$assets=Join-Path $PSScriptRoot '../examples/Platformer/Assets'
$manifest=Get-Content (Join-Path $assets 'patrol-provenance.json') -Raw|ConvertFrom-Json
if($manifest.license -ne 'CC0-1.0' -or @($manifest.files).Count -ne 2){throw 'Unexpected patrol provenance.'}
foreach($file in $manifest.files){
    $actual=(Get-FileHash (Join-Path $assets $file.file) -Algorithm SHA256).Hash.ToLowerInvariant()
    if($actual -ne $file.sha256){throw "Patrol original hash mismatch: $($file.file)"}
}
if((Get-FileHash (Join-Path $assets $manifest.license_file) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.license_sha256){throw 'Patrol license hash mismatch.'}
Write-Output 'PASS: two original same-pack patrol PNGs and CC0 license match provenance.'
