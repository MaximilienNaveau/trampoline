#! /usr/env/bin python

import sys
import pygame
from pygame.locals import (
    QUIT,
    USEREVENT,
    VIDEOEXPOSE,
    VIDEORESIZE,
    KEYUP,
    KEYDOWN,
    MOUSEMOTION,
    MOUSEWHEEL,
    MOUSEBUTTONUP,
    MOUSEBUTTONDOWN,
    ACTIVEEVENT,
)


class TrampolineCEvent:
    def __init__(self):
        # print("triggered event: __init__")
        pass

    def on_input_focus(self):
        # print("triggered event: on_input_focus")
        pass

    def on_input_blur(self):
        # print("triggered event: on_input_blur")
        pass

    def on_key_down(self, event):
        # print("triggered event: on_key_down")
        pass

    def on_key_up(self, event):
        # print("triggered event: on_key_up")
        pass

    def on_mouse_focus(self):
        """ alt tab selecting the app. """
        # print("triggered event: on_mouse_focus")
        pass

    def on_mouse_blur(self):
        """ alt tab while focus is on the app. """
        # print("triggered event: on_mouse_blur")
        pass

    def on_mouse_move(self, event):
        # print("triggered event: on_mouse_move")
        pass

    def on_mouse_wheel(self, event):
        # print("triggered event: on_mouse_wheel")
        pass

    def on_lbutton_up(self, event):
        # print("triggered event: on_lbutton_up")
        pass

    def on_lbutton_down(self, event):
        # print("triggered event: on_lbutton_down")
        pass

    def on_rbutton_up(self, event):
        # print("triggered event: on_rbutton_up")
        pass

    def on_rbutton_down(self, event):
        # print("triggered event: on_rbutton_down")
        pass

    def on_mbutton_up(self, event):
        # print("triggered event: on_mbutton_up")
        pass

    def on_mbutton_down(self, event):
        # print("triggered event: on_mbutton_down")
        pass

    def on_minimize(self):
        # print("triggered event: on_minimize")
        pass

    def on_restore(self):
        # print("triggered event: on_restore")
        pass

    def on_resize(self, event):
        # print("triggered event: on_resize")
        pass

    def on_expose(self):
        # print("triggered event: on_expose")
        pass

    def on_exit(self):
        self._running = False

    def on_user(self, event):
        # print("triggered event: on_user")
        pass

    def on_joy_axis(self, event):
        # print("triggered event: on_joy_axis")
        pass

    def on_joybutton_up(self, event):
        # print("triggered event: on_joybutton_up")
        pass

    def on_joybutton_down(self, event):
        # print("triggered event: on_joybutton_down")
        pass

    def on_joy_hat(self, event):
        # print("triggered event: on_joy_hat")
        pass

    def on_joy_ball(self, event):
        # print("triggered event: on_joy_ball")
        pass

    def on_event(self, event):
        if event.type == QUIT:
            self.on_exit()

        elif event.type >= USEREVENT:
            self.on_user(event)

        elif event.type == VIDEOEXPOSE:
            self.on_expose()

        elif event.type == VIDEORESIZE:
            self.on_resize(event)

        elif event.type == KEYUP:
            self.on_key_up(event)

        elif event.type == KEYDOWN:
            self.on_key_down(event)

        elif event.type == MOUSEMOTION:
            self.on_mouse_move(event)

        elif event.type == MOUSEWHEEL:
            self.on_mouse_wheel(event)

        elif event.type == MOUSEBUTTONUP:
            if event.button == 1:
                self.on_lbutton_up(event)
            elif event.button == 2:
                self.on_mbutton_up(event)
            elif event.button == 3:
                self.on_rbutton_up(event)

        elif event.type == MOUSEBUTTONDOWN:
            if event.button == 1:
                self.on_lbutton_down(event)
            elif event.button == 2:
                self.on_mbutton_down(event)
            elif event.button == 3:
                self.on_rbutton_down(event)

        elif event.type == ACTIVEEVENT:
            if event.state == 1:
                if event.gain:
                    self.on_mouse_focus()
                else:
                    self.on_mouse_blur()
            elif event.state == 2:
                if event.gain:
                    self.on_input_focus()
                else:
                    self.on_input_blur()
            elif event.state == 4:
                if event.gain:
                    self.on_restore()
                else:
                    self.on_minimize()


if __name__ == "__main__":
    event = CEvent()