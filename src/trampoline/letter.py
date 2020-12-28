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
        self.main_rectangle = pygame.Rect(0, 0, 0, 0)
        self.small_rectangle = pygame.Rect(0, 0, 0, 0)

        self.resize(self.size)

    def resize(self, size):
        self.size = size
        self.main_rectangle.width = self.size
        self.main_rectangle.height = self.size
        self.small_rectangle.width = int(0.35 * self.size)
        self.small_rectangle.height = int(0.35 * self.size)

    def flip(self):
        self._visible_face = not self._visible_face

    def draw(self, x, y):
        self.main_rectangle.x = x
        self.main_rectangle.y = y
        self.small_rectangle.x = (
            self.main_rectangle.topright[0] - self.small_rectangle.width
        )
        self.small_rectangle.y = self.main_rectangle.topright[1]

        if self._visible_face == self._yellow_face:
            main_letter = self.yellow_letter
            main_letter_color = Colors.black
            main_color = Colors.yellow
            secondary_letter = self.green_letter
            secondary_letter_color = Colors.white
            secondary_color = Colors.green
        else:
            main_letter = self.green_letter
            main_letter_color = Colors.white
            main_color = Colors.green
            secondary_letter = self.yellow_letter
            secondary_letter_color = Colors.black
            secondary_color = Colors.yellow

        self.surface.fill(main_color, self.main_rectangle)
        self.surface.fill(secondary_color, self.small_rectangle)
        ptext.drawbox(
            main_letter,
            self.main_rectangle.inflate(
                int(-0.2 * self.main_rectangle.width),
                int(-0.2 * self.main_rectangle.height),
            ).move(int(-0.15 * self.main_rectangle.width), 0),
            color=main_letter_color,
        )
        ptext.drawbox(
            secondary_letter, self.small_rectangle, color=secondary_letter_color
        )
