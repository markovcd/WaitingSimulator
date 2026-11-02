# The gate's image: the .NET SDK global.json names and the Godot .NET build the game's project file names.
# A local run, CI and a release all run the gate in it (docs/agents/rules/pipeline.md); gate.sh builds and runs it.
# The base is pinned by digest and Godot by checksum. PipelineTests holds both versions to the repo's.
# The android stage builds on the gate stage and adds what an Android export needs; export.sh builds it.
ARG GODOT_VERSION=4.7.2

FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS gate

ARG GODOT_VERSION

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

FROM gate AS android

ARG GODOT_VERSION

# Android exports need a JDK (the Godot docs name 17) and the SDK's platform tools, build tools and platform.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends openjdk-17-jdk-headless unzip \
    && rm -rf /var/lib/apt/lists/*

# sha256 of commandlinetools-linux-16111833_latest.zip; Google's repository lists its sha1
# (e025545c62a8e64c7559119566a569fb1dec5f60), which the download was checked against first.
ADD --checksum=sha256:0877a1d048fe4a24efe2eff536ca4223f7adeb58648bb81909d33c446918cfa8 \
    https://dl.google.com/android/repository/commandlinetools-linux-16111833_latest.zip \
    /tmp/cmdline-tools.zip

ENV ANDROID_HOME=/opt/android-sdk \
    JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64

# sdkmanager checks each package against the checksum its repository lists.
RUN mkdir -p $ANDROID_HOME/cmdline-tools \
    && unzip -q /tmp/cmdline-tools.zip -d $ANDROID_HOME/cmdline-tools \
    && mv $ANDROID_HOME/cmdline-tools/cmdline-tools $ANDROID_HOME/cmdline-tools/latest \
    && rm /tmp/cmdline-tools.zip \
    && yes | $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses > /dev/null \
    && $ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager "platform-tools" "build-tools;35.0.1" "platforms;android-35" > /dev/null

# The export templates are 1.2 GB, so they are fetched, checked and trimmed in one layer to what the exports use: the
# Linux debug template and the Android build source (Godot's prebuilt Android template only carries .NET 9, so the
# game is built for Android with Gradle). Godot publishes SHA-512 only; this is the one SHA512-SUMS.txt lists for the archive.
RUN curl --fail --silent --show-error --location --output /tmp/templates.tpz \
        https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz \
    && echo "bb5c41d72370ed743660361f6228006f808ab04ca33abdc545d740b044f3fe057f32ae8cb7873a1bc86ddcd82ae683b9f6dfdfe4179852f2c0f1acde2ff6bd5a  /tmp/templates.tpz" | sha512sum --check --quiet \
    && mkdir -p /root/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono \
    && unzip -q -j /tmp/templates.tpz templates/android_source.zip templates/linux_debug.x86_64 templates/version.txt \
        -d /root/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono \
    && rm /tmp/templates.tpz
