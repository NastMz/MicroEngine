$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../examples/Rpg/Assets'
$manifest = Get-Content -LiteralPath (Join-Path $root 'provenance.json') -Raw | ConvertFrom-Json
if ($manifest.runtime_images.Count -ne 3 -or $manifest.sources.Count -ne 3) { throw 'RPG selected bundle differs from approved three-sheet contract.' }
foreach ($source in $manifest.sources) {
    if ($source.license -ne 'CC0-1.0') { throw "Unexpected declared license: $($source.id)" }
}
foreach ($file in $manifest.files) {
    $path = Join-Path $root $file.file
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) {
        throw "Original RPG asset hash mismatch: $path"
    }
}
foreach ($image in $manifest.runtime_images) {
    if (-not ($manifest.files | Where-Object { $_.file -eq $image })) { throw "Runtime image lacks provenance: $image" }
}
Write-Output 'PASS: RPG three-sheet bundle and seven original files match approved CC0 provenance SHA256.'
