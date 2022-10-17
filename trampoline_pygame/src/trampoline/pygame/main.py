#! /usr/env/bin python


from trampoline.pygame.trampoline import Trampoline


def start_full_screen():
    trampoline = Trampoline()
    trampoline.on_execute(full_screen=True)


def start_window_mode():
    trampoline = Trampoline()
    trampoline.on_execute(full_screen=False)


if __name__ == "__main__":
    start_window_mode()
