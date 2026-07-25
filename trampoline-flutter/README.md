# trampoline

Un jeu pour faire rebondir les mots.

Flutter/Dart rewrite of the game (the original Unity/C# version, v3.x,
lives in `../trampoline`).

## Development

The Flutter/Dart toolchain is pinned via Nix, so no manual SDK install is
needed:

```sh
nix develop        # drops you into a shell with `flutter`/`dart` on PATH
flutter test       # run the test suite
flutter run -d linux   # or -d chrome / -d <android-device-id>
```
