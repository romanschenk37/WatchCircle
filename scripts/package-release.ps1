param(
    [Parameter(Mandatory = $true)]
    [string] $Tag,
    [string] $Repository = 'romanschenk37/WatchCircle',
    [string] $PublishDirectory = 'bin',
    [string] $OutputDirectory = 'artifacts/release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$props = [xml](Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props') -Raw)
$version = [string]$props.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$' -or $Tag -cne "v$version") {
    throw "The release tag must be v$version, matching Directory.Build.props."
}
if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
    throw 'Repository must have the form owner/name.'
}

$build = Get-Content -LiteralPath (Join-Path $repoRoot 'build.yaml') -Raw
$buildVersion = [regex]::Match($build, '(?m)^version: "([^"]+)"\r?$').Groups[1].Value
$targetAbi = [regex]::Match($build, '(?m)^targetAbi: "([^"]+)"\r?$').Groups[1].Value
if ($buildVersion -cne $version -or $targetAbi -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw 'build.yaml must contain the release version and a valid targetAbi.'
}

$catalog = Get-Content -LiteralPath (Join-Path $repoRoot 'manifest.json') -Raw | ConvertFrom-Json -NoEnumerate
if ($catalog.Count -ne 1 -or $catalog[0].name -cne 'WatchCircle') {
    throw 'Expected exactly one WatchCircle entry in manifest.json.'
}
$plugin = $catalog[0]
$publishPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $PublishDirectory))
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$zipName = "jellyfin-watchcircle_$version.zip"
$zipPath = Join-Path $outputPath $zipName

# Package only declared plugin assemblies, never Jellyfin's own runtime dependencies.
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($assembly in $plugin.assemblies) {
        if ([System.IO.Path]::GetFileName($assembly) -cne $assembly -or -not $assembly.EndsWith('.dll')) {
            throw "Invalid assembly filename: $assembly"
        }
        $assemblyPath = Join-Path $publishPath $assembly
        $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($assemblyPath).Version.ToString()
        if ($assemblyVersion -cne $version) {
            throw "Assembly $assembly has version $assemblyVersion instead of $version. Rebuild before packaging."
        }
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $assemblyPath, $assembly) | Out-Null
    }
}
finally {
    $zip.Dispose()
}

$release = [ordered]@{
    version = $version
    changelog = (Get-Content -LiteralPath (Join-Path $repoRoot 'RELEASE_NOTES.md') -Raw).Trim()
    targetAbi = $targetAbi
    sourceUrl = "https://github.com/$Repository/releases/download/$Tag/$zipName"
    checksum = (Get-FileHash -LiteralPath $zipPath -Algorithm MD5).Hash.ToLowerInvariant()
    timestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
}
$plugin.versions = @($release) + @($plugin.versions | Where-Object { $_.version -ne $version })
$catalogPath = Join-Path $outputPath 'manifest.json'
[System.IO.File]::WriteAllText($catalogPath, (ConvertTo-Json -InputObject $catalog -Depth 10) + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Output "Created $zipPath and $catalogPath"
