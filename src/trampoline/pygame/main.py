#! /usr/env/bin python


from trampoline.pygame.main_window import Trampoline


def start_full_screen():
    trampoline = Trampoline()
    trampoline.on_execute(full_screen=True)

def start_window_mode():
    trampoline = Trampoline()
    trampoline.on_execute(full_screen=False)

start_android=start_full_screen
start_ubuntu=start_window_mode


if __name__ == "__main__":
    start_android()
