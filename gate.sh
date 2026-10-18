#!/usr/bin/env bash
# The gate: builds the gate image (ci/gate.Dockerfile) and runs ci/gate-in-container.sh in it against this checkout.
# Needs Docker. On Windows, run it from Git Bash.
set -euo pipefail
cd "$(dirname "$0")"

# Git Bash would rewrite /src paths into Windows ones; Docker wants the host path in Windows form and the rest as is.
export MSYS_NO_PATHCONV=1
source=$(pwd -W 2>/dev/null || pwd)

docker build --file ci/gate.Dockerfile --target gate --tag waitingsim-gate ci
# The NuGet cache is a named volume so repeated local runs restore from it; the checkout is mounted read-only.
docker run --rm \
    --volume "$source:/src:ro" \
    --volume waitingsim-gate-nuget:/root/.nuget/packages \
    waitingsim-gate /src/ci/gate-in-container.sh
