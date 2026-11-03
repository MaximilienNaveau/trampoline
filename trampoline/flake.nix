{
  description = "Godot dev shell for the trampoline game";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };
        # Building for Android needs Google's Android SDK, whose licence
        # must be accepted (and which nixpkgs marks unfree): only this
        # second package set opts in, the dev shell and `nix run` don't.
        androidPkgs = import nixpkgs {
          inherit system;
          config = {
            android_sdk.accept_license = true;
            allowUnfree = true;
          };
        };
        # Just what Godot's APK export uses: `apksigner` (build-tools)
        # and `adb` (platform-tools). No NDK or Gradle: the APK is made
        # from Godot's prebuilt Android template, not compiled.
        androidSdk = (androidPkgs.androidenv.composeAndroidPackages {
          buildToolsVersions = [ "36.0.0" ];
          platformVersions = [ "36" ];
        }).androidsdk;
      in
      {
        # `nix develop` drops you into a shell with a `godot4` binary on
        # PATH (GDScript needs nothing else — no separate compiler/SDK),
        # plus `gdlint`/`gdformat` (gdtoolkit) — the closest thing GDScript
        # has to `flutter analyze`, since Godot itself ships no linter.
        devShells.default = pkgs.mkShell {
          buildInputs = [ pkgs.godot_4 pkgs.gdtoolkit_4 ];
        };

        # `nix run` launches the game. Godot writes its import cache
        # (.godot/) into the project folder, so it can't run straight from
        # the read-only Nix store: use the checkout you're in when there is
        # one (fast, reuses its cache), otherwise a writable copy of this
        # flake's source in a temporary folder (slower first start, while
        # Godot imports the assets).
        apps.default = {
          type = "app";
          meta.description = "Play Trampoline";
          program = "${pkgs.writeShellApplication {
            name = "trampoline";
            runtimeInputs = [ pkgs.godot_4 ];
            text = ''
              if [ -f project.godot ]; then
                project="$PWD"
              else
                project="$(mktemp -d)"
                cp -r --no-preserve=mode ${self}/. "$project"
              fi
              # A fresh copy has no import cache yet: build it first. (Only
              # then — an import also re-saves project.godot, which would
              # strip the comments of a checkout's copy.)
              if [ ! -d "$project/.godot" ]; then
                godot4 --headless --import --path "$project"
              fi
              # Runs the game, passing on any extra arguments, e.g.
              # `nix run . -- --resolution 480x854` for a phone-sized window.
              exec godot4 --path "$project" "$@"
            '';
          }}/bin/trampoline";
        };

        # `nix build .#android` makes result/trampoline.apk, signed with
        # the project's own debug key (keys/debug.keystore), so each build
        # installs over the previous one:
        #   adb install -r result/trampoline.apk
        # Godot runs inside the Nix build sandbox, like a compiler: a fresh
        # copy of the project, a throwaway home folder, and only the tools
        # listed here — your own editor settings play no part.
        packages.android = pkgs.stdenvNoCC.mkDerivation {
          pname = "trampoline-android";
          version = "4.0.0";
          src = ./.;
          nativeBuildInputs = [ pkgs.godot_4 pkgs.jdk17_headless ];
          buildPhase = ''
            runHook preBuild
            export HOME="$TMPDIR/home"
            export XDG_DATA_HOME="$HOME/data"
            export XDG_CONFIG_HOME="$HOME/config"
            export XDG_CACHE_HOME="$HOME/cache"
            # Godot looks for its export templates here, by version.
            mkdir -p "$XDG_DATA_HOME/godot/export_templates"
            ln -s ${pkgs.godot_4-export-templates-bin}/share/godot/export_templates/* \
              "$XDG_DATA_HOME/godot/export_templates/"
            # Read by Godot as the default Android SDK and Java paths.
            export ANDROID_HOME=${androidSdk}/libexec/android-sdk
            export JAVA_HOME=${pkgs.jdk17_headless.home}
            # A flake only sees files git tracks: an uncommitted key isn't here.
            if [ ! -f keys/debug.keystore ]; then
              echo "keys/debug.keystore is missing: is it tracked by git?" >&2
              exit 1
            fi
            export GODOT_ANDROID_KEYSTORE_DEBUG_PATH="$PWD/keys/debug.keystore"
            export GODOT_ANDROID_KEYSTORE_DEBUG_USER=androiddebugkey
            export GODOT_ANDROID_KEYSTORE_DEBUG_PASSWORD=android
            # The source has no import cache (.godot/ isn't committed).
            godot4 --headless --import --path .
            mkdir -p "$out"
            godot4 --headless --path . --export-debug Android "$out/trampoline.apk"
            runHook postBuild
          '';
          dontInstall = true;
        };
      });
}
