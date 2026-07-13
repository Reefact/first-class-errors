#!/bin/sh
# Single source of truth for producing the published NuGet packages.
#
# Both the real release (.github/workflows/release.yml) and the automatic
# rehearsal (.github/workflows/release-dryrun.yml) call this, so the dry run can
# never silently drift from the release it is meant to mirror: the set of packed
# projects, the pack flags, the embedded SBOM and the "is the SBOM actually
# there?" check all live here, once.
#
# It assumes the solution has already been built in Release (it packs with
# --no-build). It writes the .nupkg / .snupkg into ./artifacts.
#
# Usage: tools/packaging/pack.sh <version> <scope:lib|cli>
#   <version> is any valid SemVer (a real release passes the tag version; the
#             dry run passes a throwaway like 0.0.0-dryrun).
#   <scope>   selects which release train to pack, since lib and the fce CLI are
#             versioned and released independently:
#               lib -> FirstClassErrors + FirstClassErrors.Testing (lockstep)
#               cli -> FirstClassErrors.Cli (the `fce` .NET tool)

set -eu

if [ "$#" -ne 2 ] || [ -z "$1" ] || [ -z "$2" ]; then
  echo "usage: tools/packaging/pack.sh <version> <scope:lib|cli>" >&2
  exit 2
fi
version="$1"
scope="$2"

# The projects that carry NuGet identity, selected by release train. GenerateSBOM embeds an SPDX SBOM
# (_manifest/spdx_2.2/manifest.spdx.json) inside each package; it is passed here, not hardcoded in the
# csproj, so local and floor-check packs stay SBOM-free.
case "$scope" in
  lib)
    # FirstClassErrors + its testing helpers: intrinsically coupled (Testing has a ProjectReference on
    # the library and sees its internals), so they always ship together at the same version.
    projects='FirstClassErrors/FirstClassErrors.csproj FirstClassErrors.Testing/FirstClassErrors.Testing.csproj'
    ;;
  cli)
    # The `fce` .NET tool (PackAsTool; the GenDoc worker it spawns travels bundled inside the tool
    # package). Released on its own cadence and version.
    projects='FirstClassErrors.Cli/FirstClassErrors.Cli.csproj'
    ;;
  *)
    echo "error: unknown scope '$scope' (expected 'lib' or 'cli')" >&2
    exit 2
    ;;
esac

# Intentionally unquoted: $projects is a space-separated list of project paths (no spaces in paths).
for project in $projects; do
  dotnet pack "$project" -c Release --no-build -p:Version="$version" -p:GenerateSBOM=true -o artifacts
done

# Positive proof, not just a green pack: a pack that silently stopped embedding
# the manifest (a GenerateSBOM / Microsoft.Sbom.Targets regression) would
# otherwise pass unnoticed. Assert the SPDX file is present in every package.
for package in artifacts/*.nupkg; do
  if unzip -l "$package" | grep -q '_manifest/spdx_2.2/manifest.spdx.json'; then
    echo "ok: SBOM present in $package"
  else
    echo "error: SBOM manifest missing from $package" >&2
    exit 1
  fi
done
