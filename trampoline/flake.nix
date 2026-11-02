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
      });
}
