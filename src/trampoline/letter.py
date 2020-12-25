#! /usr/env/bin python

import pygame
from trampoline.colors import Colors
from trampoline import pygame_text as ptext


class Letter(object):
    def __init__(self, surface, yellow_letter, green_letter, font_name="Arial"):
        self.surface = surface
        self.yellow_letter = yellow_letter
        self.green_letter = green_letter
        self.font_name = font_name

        self._yellow_face = False
        self._green_face = True
        self._visible_face = self._yellow_face
        self.size = 40
        self.main_rectangle = pygame.Rect(0,0,0,0)
        self.small_rectangle = pygame.Rect(0,0,0,0)

        self.resize(self.size)

    def resize(self, size):
        self.size = size
        self.main_rectangle.width = self.size
        self.main_rectangle.height = self.size
        self.small_rectangle.width = 0.4 * self.size
        self.small_rectangle.height = 0.4 * self.size

    def flip(self):
        self._visible_face = not self._visible_face

    def draw(self, x, y):
        self.main_rectangle.x = x
        self.main_rectangle.y = y
        self.small_rectangle.x = self.main_rectangle.topright[0] - self.small_rectangle.width
        self.small_rectangle.y = self.main_rectangle.topright[1] - self.small_rectangle.height

        if self._visible_face == self._yellow_face:
            self.surface.fill(Colors.yellow, self.main_rectangle)
            self.surface.fill(Colors.green, self.main_rectangle)
            ptext.drawbox(self.yellow_letter, self.main_rectangle, color=Colors.black)
            ptext.drawbox(self.green_letter, self.small_rectangle, color=Colors.black)
        else:
            self.surface.fill(Colors.green, self.main_rectangle)
            self.surface.fill(Colors.yellow, self.main_rectangle)
            ptext.drawbox(self.yellow_letter, self.small_rectangle, color="black")
            ptext.drawbox(self.green_letter, self.main_rectangle, color="black")