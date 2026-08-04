<#
    Builds a release archive laid out the way Nexus and Vortex expect:

        BepInEx/plugins/LastSwing/LastSwing.dll

    Deliberately not the dev deploy path (plugins/MoonlightPeaksMods/LastSwing), which only
    exists to keep hand-built DLLs clear of Vortex during development.

    There is no test project to run. Like Plant Peek, every code path here reads Unity and game
    types - SwingToolView, the four damage components, a Canvas - so a console runner could not
    exercise anything meaningful. Verification is in TESTING.md instead.
#>

$ErrorActionPreference = 'Stop'

# This mod is its own repo, so the mod root and the repo root are the same directory. In the
# sibling mods that live inside the notes repo these differ by two levels - don't copy that
# form back in here.
$modRoot  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = $modRoot
$project  = Join-Path $modRoot 'src\LastSwing\LastSwing.csproj'

# Single source of truth for the version, so the archive can never disagree with the DLL.
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ }
if (-not $version) { throw "Could not read <Version> from $project" }

# The version is reported to players twice; a mismatch means an archive that lies about what
# is inside it.
$pluginSource = Get-Content (Join-Path $modRoot 'src\LastSwing\Plugin.cs') -Raw
if ($pluginSource -notmatch 'PluginVersion\s*=\s*"([^"]+)"') { throw 'Could not read PluginVersion from Plugin.cs' }
$pluginVersion = $Matches[1]
if ($pluginVersion -ne $version) {
    throw "Version mismatch: csproj says $version, Plugin.cs says $pluginVersion"
}

Write-Host "Packing Last Swing $version"

# SkipDeploy keeps a release build from overwriting the copy under test in the game folder.
dotnet build $project -c Release -p:SkipDeploy=true
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$dll = Join-Path $modRoot 'src\LastSwing\bin\Release\netstandard2.1\LastSwing.dll'
if (-not (Test-Path $dll)) { throw "Built DLL not found at $dll" }

$staging = Join-Path $env:TEMP "LastSwing-pack-$([guid]::NewGuid().ToString('N'))"
$target  = Join-Path $staging 'BepInEx\plugins\LastSwing'
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item $dll $target

$dist = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$archive = Join-Path $dist "LastSwing-$version.zip"
if (Test-Path $archive) { Remove-Item $archive }

Compress-Archive -Path (Join-Path $staging 'BepInEx') -DestinationPath $archive
Remove-Item $staging -Recurse -Force

Write-Host "Created $archive"
Write-Host 'Extract it over the game folder to install.'
