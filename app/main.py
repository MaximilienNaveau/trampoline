#! /usr/env/bin python


from trampoline.main_window import Trampoline


def start_ubuntu():
    trampoline = Trampoline()
    trampoline.on_execute(for_android=False)

def start_android():
    trampoline = Trampoline()
    trampoline.on_execute(for_android=True)


if __name__ == "__main__":
    start_android()
