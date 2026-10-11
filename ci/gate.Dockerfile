# The gate's image: the .NET SDK global.json names and the Godot .NET build the game's project file names.
# A local run, CI and a release all run the gate in it (docs/agents/rules/pipeline.md); gate.sh builds and runs it.
# The base is pinned by digest and Godot by checksum. PipelineTests holds both versions to the repo's.
FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317

ARG GODOT_VERSION=4.7.2

# sha256 of Godot_v4.7.2-stable_mono_linux_x86_64.zip. Godot publishes SHA-512 only
# (1855960b27ee3ef5e66e5e228cced69d55637b24334a7411162687dcd077d8f9f645348cdb8eae984bec8135d49ed855a1e3a16476786b8bce60774fd8402d13);
# the download was checked against it before this sha256 was taken.
ADD --checksum=sha256:129f82db7bafd54ae14bb5bb284041c73860e8c7a009a3a026ca5e946cbff247 \
    https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip \
    /tmp/godot.zip

# The image has no unzip; PowerShell ships with the SDK image. Unzipping drops the executable bit, hence the chmod.
RUN pwsh -NoProfile -Command "Expand-Archive -Path /tmp/godot.zip -DestinationPath /opt" \
    && mv /opt/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64 /opt/godot \
    && chmod +x /opt/godot/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64 \
    && ln -s /opt/godot/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64 /usr/local/bin/godot \
    && rm /tmp/godot.zip

ENV DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1
