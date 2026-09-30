param(
    [Parameter(Mandatory = $true)]
    [string] $ReleaseManifest,
    [string] $CatalogPath = 'manifest.json'
)

$ErrorActionPreference = 'Stop'
$incoming = Get-Content -LiteralPath $ReleaseManifest -Raw | ConvertFrom-Json -NoEnumerate
$catalog = Get-Content -LiteralPath $CatalogPath -Raw | ConvertFrom-Json -NoEnumerate
$releasedPlugin = $incoming[0]
$release = $releasedPlugin.versions[0]
$plugin = $catalog | Where-Object { $_.guid -eq $releasedPlugin.guid } | Select-Object -First 1
if (-not $plugin -or -not $release.version -or -not $release.checksum -or -not $release.sourceUrl) {
    throw 'Release metadata does not match the WatchCircle catalog.'
}
$existing = $plugin.versions | Where-Object { $_.version -eq $release.version } | Select-Object -First 1
if ($existing -and ($existing.checksum -ne $release.checksum -or $existing.sourceUrl -ne $release.sourceUrl)) {
    throw 'This version is already published with a different package. Use a new version.'
}
if (-not $existing) {
    $plugin.versions = @(@($plugin.versions) + @($release) | Sort-Object { [Version]$_.version } -Descending)
    [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($CatalogPath), (ConvertTo-Json -InputObject $catalog -Depth 10) + "`n", [System.Text.UTF8Encoding]::new($false))
}
