# Godot built from source the way F-Droid's build recipe (fdroiddata,
# metadata/com.magamajo.trampoline.yml) builds it, so the APK exported here
# is byte for byte the one F-Droid builds: F-Droid then publishes our
# signed APK (reproducible build) instead of signing its own. Keep the two
# in step: same Godot commit, same scons commands, same NDK, Gradle and JDK.
#
# - editor: the Linux editor, used only for --import and --export (the
#   modules the game doesn't use left out, as F-Droid does to save time);
# - template: Godot's Android release template (android_release.apk), its
#   native library for armeabi-v7a and arm64-v8a, packaged by Gradle.
{ pkgs, androidPkgs }:
let
  inherit (pkgs) lib;
  version = "4.7.2-stable";

  src = pkgs.fetchFromGitHub {
    owner = "godotengine";
    repo = "godot";
    tag = version;
    # Godot reads its commit hash from .git/HEAD (methods.py): keep only that.
    leaveDotGit = true;
    postFetch = ''
      hash=$(git -C "$out" rev-parse HEAD)
      rm -r "$out"/.git
      mkdir "$out"/.git
      echo "$hash" > "$out"/.git/HEAD
    '';
    hash = "sha256-RyUaOUkE6rYQcn1KKRY2GQ4vj8Gs7N24EJ+vU13meJE=";
  };

  # Godot also builds the commit's date into the engine, from
  # `git log -1 --pretty=format:%ct` (methods.py), which needs the whole
  # history: answer with that date (4.7.2-stable's, 2026-08-17T01:18:38Z),
  # as F-Droid's full clone does.
  gitCommitTime = pkgs.writeShellScriptBin "git" ''
    printf 1786929518
  '';

  scons = "scons -j $NIX_BUILD_CORES verbose=no warnings=no progress=no";

  # As in the F-Droid recipe.
  editorFlags = lib.concatStringsSep " " [
    "target=editor platform=linuxbsd wayland=no disable_xr=yes"
    "module_webrtc_enabled=no module_webxr_enabled=no module_websocket_enabled=no"
    "module_jsonrpc_enabled=no module_enet_enabled=no module_upnp_enabled=no"
    "module_theora_enabled=no module_mp3_enabled=no module_tinyexr_enabled=no"
    "module_interactive_music_enabled=no module_camera_enabled=no"
    "module_visual_shader_enabled=no module_jolt_physics_enabled=no"
    "module_godot_physics_3d_enabled=no module_navigation_3d_enabled=no"
    "module_raycast_enabled=no module_lightmapper_rd_enabled=no module_csg_enabled=no"
    "module_gridmap_enabled=no module_vhacd_enabled=no module_xatlas_unwrap_enabled=no"
    "module_gltf_enabled=no module_fbx_enabled=no module_objectdb_profiler_enabled=no"
  ];
  templateFlags = "target=template_release platform=android production=yes swappy=no";

  ndkVersion = "29.0.14206865";
  androidSdk =
    (androidPkgs.androidenv.composeAndroidPackages {
      buildToolsVersions = [ "36.1.0" ];
      platformVersions = [ "36" ];
      includeNDK = true;
      ndkVersions = [ ndkVersion ];
    }).androidsdk;
  sdkRoot = "${androidSdk}/libexec/android-sdk";

  # The Gradle version of Godot's wrapper (platform/android/java/gradle/
  # wrapper/gradle-wrapper.properties), which F-Droid's build server uses.
  gradle =
    (pkgs.gradle-packages.mkGradle {
      version = "8.11.1";
      hash = "sha256-85eyhwI6zboen2/F6nLSLdY2adWe1KKJopsadu7hUcY=";
      defaultJava = pkgs.jdk21;
    }).wrapped;

  # The aapt2 (Android resource compiler) that the Android Gradle plugin
  # 8.6.1 downloads from Google's Maven repository, as on F-Droid's build
  # server. It's a prebuilt Linux program: the Nix sandbox has no
  # /lib64/ld-linux*, so it gets Nix's loader (autoPatchelf), same code.
  aapt2 = pkgs.stdenv.mkDerivation {
    pname = "aapt2";
    version = "8.6.1-11315950";
    src = pkgs.fetchurl {
      url = "https://dl.google.com/dl/android/maven2/com/android/tools/build/aapt2/8.6.1-11315950/aapt2-8.6.1-11315950-linux.jar";
      hash = "sha256-lV8U5hysLh1wpHA7GqU5yk80hGmsZKFV3M5HvaPeUsg=";
    };
    nativeBuildInputs = [
      pkgs.unzip
      pkgs.autoPatchelfHook
    ];
    buildInputs = [ pkgs.stdenv.cc.cc.lib ];
    unpackPhase = "unzip $src aapt2";
    installPhase = "install -D -m 755 aapt2 $out/bin/aapt2";
  };
in
rec {
  inherit version src androidSdk;

  editor = pkgs.stdenv.mkDerivation {
    pname = "godot-editor-fdroid";
    inherit version src;
    nativeBuildInputs = [
      pkgs.scons
      pkgs.pkg-config
      gitCommitTime
    ];
    buildInputs = with pkgs; [
      libx11
      libxcursor
      libxinerama
      libxi
      libxrandr
      libxext
      libxrender
      libGL
      alsa-lib
      libpulseaudio
      udev
    ];
    buildPhase = ''
      runHook preBuild
      ${scons} ${editorFlags}
      runHook postBuild
    '';
    installPhase = ''
      runHook preInstall
      install -D -m 755 bin/godot.linuxbsd.editor.x86_64 $out/bin/godot
      runHook postInstall
    '';
  };

  # Godot's Android native library, release build, for each ABI: what the
  # recipe's two `scons ... platform=android arch=...` commands make
  # (in platform/android/java/lib/libs/release/<abi>/).
  nativeLibs = pkgs.stdenvNoCC.mkDerivation {
    pname = "godot-android-libs";
    inherit version src;
    nativeBuildInputs = [
      pkgs.scons
      gitCommitTime
    ];
    ANDROID_NDK_ROOT = "${sdkRoot}/ndk/${ndkVersion}";
    ANDROID_HOME = sdkRoot;
    ANDROID_SDK_ROOT = sdkRoot;
    buildPhase = ''
      runHook preBuild
      ${scons} ${templateFlags} arch=arm32
      ${scons} ${templateFlags} arch=arm64
      runHook postBuild
    '';
    installPhase = ''
      runHook preInstall
      cp -r platform/android/java/lib/libs/release $out
      runHook postInstall
    '';
  };

  # android_release.apk: the recipe's arm64 scons command, with
  # generate_android_binaries=yes, runs `./gradlew generateGodotTemplates
  # --quiet` in platform/android/java once the libraries are built; the
  # same here with Gradle's dependencies pinned (deps.json, see below).
  template = pkgs.stdenvNoCC.mkDerivation (finalAttrs: {
    pname = "godot-android-template";
    inherit version src;
    nativeBuildInputs = [
      gradle
      pkgs.jdk21
      # Looked up by lib/build.gradle when configuring, though the template
      # task skips its scons tasks (the libraries are already built).
      pkgs.scons
    ];
    # Gradle's downloads, recorded once by
    #   nix build .#godotAndroid.template.mitmCache.updateScript && ./result
    # (from the repository root), replayed offline by the build.
    mitmCache = gradle.fetchDeps {
      pkg = finalAttrs.finalPackage;
      data = ./godot-android-deps.json;
    };
    ANDROID_HOME = sdkRoot;
    ANDROID_SDK_ROOT = sdkRoot;
    JAVA_HOME = pkgs.jdk21.home;
    gradleFlags = [
      "--quiet"
      "-Pandroid.aapt2FromMavenOverride=${aapt2}/bin/aapt2"
    ];
    gradleBuildTask = "generateGodotTemplates";
    # Records the downloads of the real build (the default task only
    # resolves the declared dependencies, missing some plugin ones).
    gradleUpdateTask = "generateGodotTemplates";
    preBuild = ''
      mkdir -p platform/android/java/lib/libs
      cp -r --no-preserve=mode ${finalAttrs.passthru.nativeLibs} platform/android/java/lib/libs/release
      cd platform/android/java
    '';
    installPhase = ''
      runHook preInstall
      install -D -m 644 ../../../bin/android_release.apk $out/android_release.apk
      runHook postInstall
    '';
    passthru = { inherit nativeLibs; };
  });
}
