{
  description = "Godot dev shell, audio, tests and exports for the trampoline game";

  # CI pushes what it builds (audio, tests, exports) to this binary cache,
  # so a machine that trusts it downloads them instead of rebuilding.
  # (Nix asks before using a flake's cache; `--accept-flake-config` says yes.)
  nixConfig = {
    extra-substituters = [ "https://baxter.cachix.org" ];
    extra-trusted-public-keys = [ "baxter.cachix.org-1:BJW9FafkmcXVDJVK2xL/b0J3bQkn+wdJPNgmHDRfzoQ=" ];
  };

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };
        inherit (pkgs) lib;
        version = "4.0.0";

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

        # The tools to make the game's music and sound effects from
        # scripts, all open source:
        # - python3 with mido (writes MIDI) and numpy/scipy (synthesises
        #   sound effects sample by sample, writes .wav),
        # - lilypond: music as text, rendered to MIDI and sheet music,
        # - fluidsynth + a free General MIDI soundfont: MIDI -> .wav,
        # - sox and ffmpeg: trim, mix, normalise, encode to .ogg.
        audioTools = [
          (pkgs.python3.withPackages (ps: [ ps.mido ps.numpy ps.scipy ]))
          pkgs.lilypond
          pkgs.fluidsynth
          pkgs.sox
          pkgs.ffmpeg
        ];
        # The soundfont paths, exported so scripts needn't hunt for them.
        soundfonts = {
          SF2_FLUID = "${pkgs.soundfont-fluid}/share/soundfonts/FluidR3_GM2-2.sf2";
          SF2_GENERALUSER = "${pkgs.soundfont-generaluser-gs}/share/soundfonts/GeneralUser-GS.sf2";
        };

        # The music and sound effects the game plays, made from
        # tools/audio/ (they aren't committed: only their .import files
        # are). Its source is that folder alone, so it's only rebuilt
        # when the scripts change. Lays out like assets/audio/.
        audio = pkgs.stdenvNoCC.mkDerivation ({
          pname = "trampoline-audio";
          inherit version;
          src = ./tools/audio;
          nativeBuildInputs = audioTools;
          buildPhase = ''
            runHook preBuild
            export HOME="$TMPDIR"
            python3 make_music.py --out "$out/music" original_le_rebond original_la_grille
            python3 make_sfx.py --out "$out/sfx"
            runHook postBuild
          '';
          dontInstall = true;
        } // soundfonts);

        # The project as the exports and tests see it: the committed
        # files plus the generated audio dropped into assets/audio/.
        project = pkgs.runCommand "trampoline-project-${version}" { } ''
          cp -r --no-preserve=mode ${./.} "$out"
          cp -r --no-preserve=mode ${audio}/. "$out/assets/audio/"
        '';

        # Godot runs inside the Nix build sandbox, like a compiler: a fresh
        # copy of the project, a throwaway home folder, and only the tools
        # listed here — your own editor settings play no part.
        godotEnv = ''
          export HOME="$TMPDIR/home"
          export XDG_DATA_HOME="$HOME/data"
          export XDG_CONFIG_HOME="$HOME/config"
          export XDG_CACHE_HOME="$HOME/cache"
          # Godot looks for its export templates here, by version.
          mkdir -p "$XDG_DATA_HOME/godot/export_templates"
          ln -s ${pkgs.godot_4-export-templates-bin}/share/godot/export_templates/* \
            "$XDG_DATA_HOME/godot/export_templates/"
          # The source has no import cache (.godot/ isn't committed); this
          # also builds the class_name cache that tests and exports need.
          godot4 --headless --import --path .
        '';

        # `nix build .#<name>` exports one preset of export_presets.cfg
        # into result/<file>. `mode` is "release" or "debug". `setup` runs
        # before Godot's first start, which is when it reads the
        # environment into its editor settings (e.g. the Android SDK path).
        godotExport = { name, preset, file, mode ? "release", extraInputs ? [ ], setup ? "" }:
          pkgs.stdenvNoCC.mkDerivation {
            pname = "trampoline-${name}";
            inherit version;
            src = project;
            nativeBuildInputs = [ pkgs.godot_4 ] ++ extraInputs;
            buildPhase = ''
              runHook preBuild
              ${setup}
              ${godotEnv}
              mkdir -p "$out"
              godot4 --headless --path . --export-${mode} "${preset}" "$out/${file}"
              runHook postBuild
            '';
            dontInstall = true;
          };

        exports = {
          linux = godotExport { name = "linux"; preset = "Linux"; file = "trampoline.x86_64"; };
          windows = godotExport { name = "windows"; preset = "Windows Desktop"; file = "trampoline.exe"; };
          macos = godotExport { name = "macos"; preset = "macOS"; file = "trampoline.zip"; };
          web = godotExport { name = "web"; preset = "Web"; file = "index.html"; };
          # No iOS: Godot refuses to export even the unsigned Xcode project
          # without an App Store Team ID, which needs an Apple account.

          # result/trampoline.apk, signed with the project's own debug key
          # (keys/debug.keystore), so each build installs over the previous
          # one: adb install -r result/trampoline.apk
          # TODO: a release build signed with the F-Droid key (doc/TODO.md).
          android = godotExport {
            name = "android";
            preset = "Android";
            file = "trampoline.apk";
            mode = "debug";
            extraInputs = [ pkgs.jdk17_headless ];
            setup = ''
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
            '';
          };
        };

        # A check that runs a command over the committed sources and
        # passes if it exits 0 (its output is an empty marker file).
        sourceCheck = name: inputs: command:
          pkgs.runCommand "trampoline-${name}" { nativeBuildInputs = inputs; } ''
            # gdtoolkit caches its parser in the home folder: give it one.
            export HOME="$TMPDIR"
            cd ${./.}
            ${command}
            touch "$out"
          '';
      in
      {
        # `nix develop` drops you into a shell with a `godot4` binary on
        # PATH (GDScript needs nothing else — no separate compiler/SDK),
        # plus `gdlint`/`gdformat` (gdtoolkit) — the closest thing GDScript
        # has to `flutter analyze`, since Godot itself ships no linter.
        devShells.default = pkgs.mkShell {
          buildInputs = [ pkgs.godot_4 pkgs.gdtoolkit_4 ];
        };

        # `nix develop .#audio`: the tools to edit and rerun
        # tools/audio/*.py by hand (see tools/audio/README.md).
        devShells.audio = pkgs.mkShell ({ buildInputs = audioTools; } // soundfonts);

        packages = exports // {
          inherit audio;
          default = exports.linux;
        };

        # `nix flake check -L` runs everything CI runs: formatting, lint,
        # the audio pipeline, the unit tests on Linux, and every export.
        checks = exports // {
          inherit audio;
          format = sourceCheck "format" [ pkgs.gdtoolkit_4 ] "gdformat --check scripts/*.gd";
          lint = sourceCheck "lint" [ pkgs.gdtoolkit_4 ] "gdlint scripts/*.gd";
          # Each tests/*.gd is a standalone script Godot runs headless; all
          # of them run, then the check fails if any one did.
          tests = pkgs.stdenvNoCC.mkDerivation {
            pname = "trampoline-tests";
            inherit version;
            src = project;
            nativeBuildInputs = [ pkgs.godot_4 ];
            buildPhase = ''
              runHook preBuild
              ${godotEnv}
              status=0
              for test_file in tests/*.gd; do
                echo "=== $test_file"
                godot4 --headless --path . --script "$test_file" || status=1
              done
              [ "$status" = 0 ]
              touch "$out"
              runHook postBuild
            '';
            dontInstall = true;
          };
        };

        apps = {
          # `nix run` launches the game. Godot writes its import cache
          # (.godot/) into the project folder, so it can't run straight
          # from the read-only Nix store: use the checkout you're in when
          # there is one (fast, reuses its cache), otherwise a writable copy
          # of the project in a temporary folder (slower first start, while
          # Godot imports the assets).
          default = {
            type = "app";
            meta.description = "Play Trampoline";
            program = "${pkgs.writeShellApplication {
              name = "trampoline";
              runtimeInputs = [ pkgs.godot_4 ];
              text = ''
                if [ -f project.godot ]; then
                  project="$PWD"
                  # A fresh checkout has no audio yet: bring in the built one.
                  if [ ! -f assets/audio/sfx/bounce.wav ]; then
                    cp -r --no-preserve=mode ${audio}/. assets/audio/
                  fi
                else
                  project="$(mktemp -d)"
                  cp -r --no-preserve=mode ${project}/. "$project"
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

          # `nix run .#audio` (from trampoline/): copies the built music and
          # sound effects into assets/audio/, for the Godot editor.
          audio = {
            type = "app";
            meta.description = "Put the generated audio into assets/audio/";
            program = "${pkgs.writeShellScript "trampoline-audio" ''
              set -e
              if [ ! -f project.godot ]; then
                echo "run this from the trampoline/ folder" >&2
                exit 1
              fi
              cp -r --no-preserve=mode ${audio}/. assets/audio/
              echo "assets/audio/ updated from ${audio}"
            ''}";
          };
        };
      });
}
