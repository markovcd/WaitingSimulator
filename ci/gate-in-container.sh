#!/usr/bin/env bash
# The gate, run inside the gate image by gate.sh: builds and tests everything, then checks the game starts under Godot.
# It works on a copy of the read-only checkout at /src, so nothing it builds lands in the caller's tree.
set -euo pipefail

# The copy belongs to the container's user, not the checkout's owner; git refuses to read a repository owned by another.
rm -rf /work
cp -a --no-preserve=ownership /src/. /work
cd /work
find . \( -name bin -o -name obj -o -name .godot -o -name TestResults \) -type d -prune -exec rm -rf {} +

# Restores are locked to the committed lock files, as in CI (Directory.Build.props).
export CI=true

# dotnet test builds only the test projects and what they reference, so the game is built with the solution first.
dotnet build WaitingSimulator.slnx
dotnet test --solution WaitingSimulator.slnx --no-build

# Godot writes files the repo should hold (a .uid beside each new script) and rewrites a project file it disagrees
# with. Either means the checkout is missing something, so a change to what git sees fails the gate.
before=$(git status --porcelain --untracked-files=all)
godot --headless --path game --import
after=$(git status --porcelain --untracked-files=all)
if [[ "$before" != "$after" ]]; then
    echo "gate: Godot changed or added files under game/; run the import locally and commit what it writes:" >&2
    diff <(echo "$before") <(echo "$after") >&2 || true
    exit 1
fi

# The smoke run quits as soon as the client is ready. A client whose scripts cannot load keeps running instead,
# so the time limit is the failure; a normal start takes a few seconds.
timeout 120 godot --headless --path game -- --smoke
echo "gate: passed"
