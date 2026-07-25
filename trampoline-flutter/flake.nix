{
  description = "Flutter/Dart dev shell for the trampoline game";

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
        # `nix develop` drops you into a shell with `flutter` and `dart` on
        # PATH. pkgs.flutter bundles its own Dart SDK, so nothing else is
        # needed to build/run/test this project.
        devShells.default = pkgs.mkShell {
          buildInputs = [ pkgs.flutter ];
        };
      });
}
