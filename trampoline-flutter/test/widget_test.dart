// `flutter_test` is the widget-testing library: it can build widgets in
// memory (no real device/browser needed) and inspect the result, similar in
// spirit to a GoogleTest fixture that drives an object and asserts on it.
import 'package:flutter_test/flutter_test.dart';

// Import our own app so the test can build it. `package:trampoline/main.dart`
// resolves via the `name: trampoline` entry in pubspec.yaml, the same way a
// CMake target name is used to reference that target elsewhere.
import 'package:trampoline/main.dart';

// Dart tests, like a Python unittest file or a GoogleTest binary, have a
// single `main()` that registers and runs the test cases.
void main() {
  // `testWidgets` declares one test case; its body receives a `tester`
  // object used to build widgets and simulate a frame, comparable to a test
  // fixture object in GoogleTest.
  testWidgets('shows the app title', (WidgetTester tester) async {
    // Build our widget tree in memory (no window/device needed) and let
    // Flutter run one layout/paint pass, like calling render() once in a
    // unit test.
    await tester.pumpWidget(const TrampolineApp());

    // `expect` is an assertion, as in any xUnit-style framework.
    // `find.text(...)` searches the built widget tree for a Text widget
    // with that exact string; `findsOneWidget` asserts exactly one match.
    expect(find.text('Un jeu pour faire rebondir les mots'), findsOneWidget);
  });
}
