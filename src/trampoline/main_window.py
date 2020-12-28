#! /usr/env/bin python

from os import path
import pygame
from pygame.locals import RESIZABLE
import pkg_resources
from pygame.locals import QUIT
from trampoline.cevent import TrampolineCEvent
from trampoline.grid import Grid
from trampoline.letter import Letter
from trampoline.colors import Colors


class Trampoline(TrampolineCEvent):
    """Un jeu pour faire rebondir les mots. """

    def __init__(self):
        self._running = True
        self._display_surface = None
        self._image_surf = None

    def on_init(self):
        pygame.init()
        pygame.display.set_caption("Trampoline")
        self._display_surface = pygame.display.set_mode((1000, 500), RESIZABLE)
        self._running = True
        image_path = pkg_resources.resource_filename(
            "trampoline", path.join("resources", "images", "thumbnail.jpeg")
        )

        # self._image_surf = pygame.image.load(image_path).convert()
        self.grid1 = Grid(
            surface=self._display_surface,
            size=(30 * 13, 30 * 9),
            color=Colors.burlywood,
        )

        self.grid2 = Grid(surface=self._display_surface, color=Colors.burlywood)

        self.row_numbers = [
            Letter(self._display_surface, str(i), "R") for i in range(self.grid1.rows)
        ]
        self.col_numbers = [
            Letter(self._display_surface, str(i), "R") for i in range(self.grid1.cols)
        ]

        self.letter = Letter(self._display_surface, "A", "R")
        self.letter_flipped = Letter(self._display_surface, "A", "R")
        self.letter_flipped.flip()

    def on_loop(self):
        # self.grid1.set_letter(self.letter, 5, 8)
        # self.grid2.set_letter(self.letter, 2, 3)
        # self.grid1.set_letter(self.letter_flipped, 3, 6)
        for i in range(self.grid1.rows):
            self.grid1.set_letter(self.row_numbers[i], i, 0)
            self.grid2.set_letter(self.row_numbers[i], i, 0)

        for i in range(self.grid1.cols):
            self.grid1.set_letter(self.col_numbers[i], 0, i)
            self.grid2.set_letter(self.col_numbers[i], 0, i)

    def on_render(self):
        self._display_surface.fill(Colors.white)
        self.grid1.draw(20, 10)
        self.grid2.draw(500, 10)
        # self.letter.draw(0, 0)
        # self.letter.draw(10, 10)

        # display
        pygame.display.flip()

    def on_cleanup(self):
        pygame.quit()

    def on_execute(self):
        if self.on_init() == False:
            self._running = False

        while self._running:
            for event in pygame.event.get():
                self.on_event(event)
            self.on_loop()
            self.on_render()
        self.on_cleanup()
