#!/usr/bin/env bash
# The exports, run inside the gate image's android stage by export.sh: a Linux build that must pass the smoke runs,
# which proves an exported build finds its data, and the Android APK for playtesting on a phone. Both land in /out.
# It works on a copy of the read-only checkout at /src, like the gate.
set -euo pipefail

rm -rf /work
cp -a --no-preserve=ownership /src/. /work
cd /work
find . \( -name bin -o -name obj -o -name .godot -o -name TestResults \) -type d -prune -exec rm -rf {} +

# An export restores for a runtime and a configuration the committed lock files do not describe, so it restores
# unlocked. The gate, which runs first, holds every restore to the lock files.
unset CI

# The presets pack data/ from inside the project (ShippedData unpacks it).
cp -r data game/data

# Godot's export builds through a solution named for the game's assembly, beside the project.
dotnet new sln --format sln --name WaitingSimulator.Game --output game
dotnet sln game/WaitingSimulator.Game.sln add game/WaitingSimulator.Game.csproj

# A debug key made for this build: a phone that has an older build installs a newer one only after uninstalling it.
rm -f /tmp/debug.keystore
keytool -genkeypair -keystore /tmp/debug.keystore -alias androiddebugkey -storepass android -keypass android \
    -keyalg RSA -validity 365 -dname "CN=Waiting Simulator debug" 2> /dev/null
export GODOT_ANDROID_KEYSTORE_DEBUG_PATH=/tmp/debug.keystore
export GODOT_ANDROID_KEYSTORE_DEBUG_USER=androiddebugkey
export GODOT_ANDROID_KEYSTORE_DEBUG_PASSWORD=android

godot --headless --path game --import

# Godot reports a failed export in its log and still exits 0, so each export is checked by the file it should write.
mkdir -p /out/linux
godot --headless --path game --export-debug Linux /out/linux/WaitingSimulator.x86_64
test -f /out/linux/WaitingSimulator.pck
timeout 120 /out/linux/WaitingSimulator.x86_64 --headless -- --smoke
timeout 120 /out/linux/WaitingSimulator.x86_64 --headless -- --smoke --level deli-counter

godot --headless --path game --install-android-build-template --export-debug Android /out/WaitingSimulator.apk
test -f /out/WaitingSimulator.apk
echo "export: wrote /out/WaitingSimulator.apk"
