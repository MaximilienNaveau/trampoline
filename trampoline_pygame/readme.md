# trampoline

"Un jeu pour faire rebondir les mots" par Michel Cheenne.
Implementation par Maximilien Naveau

# Getting started on linux

In order to install this package one can use "pip".

```
git clone https://github.com/MaximilienNaveau/trampoline
cd trampoline
python3 -m pip install .
```

## Start the app

In order to start the game one can then do:

```
trampoline
```

and enjoy ;)

# Getting started on windows

To be written sorry...

# Create the android apk on linux

In order to create an android apk on Ubuntu18.04 we use
[buildozer](https://buildozer.readthedocs.io/en/latest/installation.html).

```
cd trampoline
buildozer -v android debug # or buildozer -v android release
```

# Technology used

This started as a pygame application. Though one unsaid requirement for this
video game is to be able to run as well on Linux (Ubuntu latest, targeted 20.04)
as on Android.

The Python language is very nice but apparently limited when it comes to
video games on android.

Hence the pipeline is changing from Pygame to Kivy. And we use Buildozer in oder
to cross-compile the code into APK (Android applications).
