$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../examples/Adventure/Assets'
foreach ($manifest in @((Join-Path $root 'provenance.json'), (Join-Path $root 'ArMM1998/provenance.json'))) {
    $data = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
    if ($data.license -ne 'CC0-1.0') { throw "Unexpected license: $manifest" }
    foreach ($file in $data.files) {
        $path = Join-Path (Split-Path $manifest) $file.file
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) {
            throw "Original asset hash mismatch: $path"
        }
    }
}
Write-Output 'PASS: original Kenney and ArMM1998 adventure assets match provenance SHA256.'
