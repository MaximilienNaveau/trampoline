// `import` is like a C++ `#include` or Python `import`: it pulls in the
// Flutter "material" library, which provides ready-made Android/Google
// style widgets (buttons, app bars, text styles, ...).
import 'package:flutter/material.dart';

// `main()` is the program entry point, exactly like in C/C++. Flutter apps
// don't return an exit code; instead they call `runApp` to start the UI
// event loop and keep running until the window/app is closed.
void main() {
  runApp(const TrampolineApp());
}

// In Flutter, the whole UI is a tree of immutable "widget" objects, each
// describing one node of that tree (roughly: a widget is to the UI what a
// struct is to data — it just holds the configuration, not behaviour).
// `StatelessWidget` is the simplest kind: a widget with no internal state,
// comparable to a pure function that always renders the same way given the
// same inputs.
class TrampolineApp extends StatelessWidget {
  // `const` constructor: like a C++ `constexpr` constructor, it lets Flutter
  // build this widget at compile time when possible, which is cheaper.
  // `super.key` forwards an optional identity token to the base class; it
  // helps Flutter tell widgets apart when the tree is rebuilt (not needed
  // here, but idiomatic to keep).
  const TrampolineApp({super.key});

  // `@override` works like C++'s `override` keyword: this method replaces
  // the base class's `build`. Flutter calls `build` whenever it needs to
  // (re)compute what this widget looks like, and expects a `Widget` tree in
  // return — there is no manual "draw" call to make.
  @override
  Widget build(BuildContext context) {
    // `MaterialApp` is the top-level widget that wires up navigation,
    // theming and the app title; think of it as the root window/main loop
    // configuration object.
    return MaterialApp(
      title: 'Trampoline',
      // `Scaffold` gives the standard "app bar + body" page layout, similar
      // to a basic window template with a title bar and a content area.
      home: Scaffold(
        appBar: AppBar(title: const Text('Trampoline')),
        // `Center` positions its single child in the middle of the
        // available space (a layout widget, it draws nothing itself).
        body: const Center(
          child: Text('Un jeu pour faire rebondir les mots'),
        ),
      ),
    );
  }
}
