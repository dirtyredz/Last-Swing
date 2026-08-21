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
$project  = Join-Path $modRoot 'src\LastSwing.csproj'

# Single source of truth for the version, so the archive can never disagree with the DLL.
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ }
if (-not $version) { throw "Could not read <Version> from $project" }

Write-Host "Packing Last Swing $version"

# SkipDeploy keeps a release build from overwriting the copy under test in the game folder.
dotnet build $project -c Release -p:SkipDeploy=true
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$dll = Join-Path $modRoot 'src\bin\Release\netstandard2.1\LastSwing.dll'
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

# Convenience for the author's machine only.
#
# This mod used to live inside the notes repo alongside its siblings, whose pack scripts all
# write to one shared dist/ two levels up. Splitting it into its own repo moved the archive
# inside the mod folder - correct for a standalone repo, which has no business writing outside
# itself, but it breaks the habit of finding every mod's zip in one place.
#
# So: drop a second copy there, but only when this checkout is sitting in that layout. The
# guard is that the parent folder is literally named "mods" and a dist/ already exists beside
# it. Neither is true for someone who clones this repo on its own, so they get the standalone
# behaviour and nothing is created behind their back.
$parent = Split-Path -Parent $modRoot
if ((Split-Path -Leaf $parent) -eq 'mods') {
    $sharedDist = Join-Path (Split-Path -Parent $parent) 'dist'

    if ((Test-Path $sharedDist) -and ((Resolve-Path $sharedDist).Path -ne (Resolve-Path $dist).Path)) {
        try {
            Copy-Item $archive $sharedDist -Force
            Write-Host "Also copied to $sharedDist"
        }
        catch {
            # A convenience copy failing must not fail the pack - the real archive already exists.
            Write-Warning "Could not copy to $sharedDist : $($_.Exception.Message)"
        }
    }
}

Write-Host 'Extract it over the game folder to install.'
