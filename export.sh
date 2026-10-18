#!/usr/bin/env bash
# The exports: builds the gate image's android stage (ci/gate.Dockerfile) and runs ci/export-in-container.sh in it
# against this checkout. Writes exports/WaitingSimulator.apk, and the Linux build it smoke-tests to exports/linux/.
# Needs Docker. On Windows, run it from Git Bash.
set -euo pipefail
cd "$(dirname "$0")"

# Git Bash would rewrite /src paths into Windows ones; Docker wants the host path in Windows form and the rest as is.
export MSYS_NO_PATHCONV=1
source=$(pwd -W 2>/dev/null || pwd)

mkdir -p exports
docker build --file ci/gate.Dockerfile --target android --tag waitingsim-android ci
# The NuGet and Gradle caches are named volumes so repeated local runs restore from them; the checkout is read-only.
docker run --rm \
    --volume "$source:/src:ro" \
    --volume "$source/exports:/out" \
    --volume waitingsim-gate-nuget:/root/.nuget/packages \
    --volume waitingsim-android-gradle:/root/.gradle \
    waitingsim-android /src/ci/export-in-container.sh
