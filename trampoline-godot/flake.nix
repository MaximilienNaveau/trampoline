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
        # PATH (GDScript needs nothing else — no separate compiler/SDK).
        devShells.default = pkgs.mkShell {
          buildInputs = [ pkgs.godot_4 ];
        };
      });
}
