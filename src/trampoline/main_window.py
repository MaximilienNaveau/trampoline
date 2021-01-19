#! /usr/env/bin python

from os import path
import pygame
from pygame.locals import RESIZABLE
import pkg_resources
from pygame.locals import QUIT
from trampoline.cevent import TrampolineCEvent
from trampoline.grid import Grid
from trampoline.letter import AllLetters
from trampoline.colors import Colors


class Trampoline(TrampolineCEvent):
    """Un jeu pour faire rebondir les mots. """

    def __init__(self):
        self._running = True
        self._display_surface = None
        self._image_surf = None

        super().__init__()

    def on_init(self):
        pygame.init()
        pygame.display.set_caption("Trampoline")
        self._display_surface = pygame.display.set_mode((2000, 1000), RESIZABLE)
        self._running = True

        self.grid_to_be_filled = Grid(
            surface=self._display_surface, color=Colors.burlywood
        )

        self.grid_with_letter = Grid(
            surface=self._display_surface, color=Colors.burlywood
        )

        self.letters = AllLetters(self._display_surface)

        for row in range(self.grid_with_letter.rows):
            for col in range(self.grid_with_letter.cols):
                self.grid_with_letter[row, col].set_letter(self.letters[row, col])

    def on_loop(self):
        # Quit on "ctrl + q".
        all_keys = pygame.key.get_pressed()
        if all_keys[pygame.K_q] and (
            all_keys[pygame.K_LCTRL] or all_keys[pygame.K_RCTRL]
        ):
            self._running = False

        # update the grid placement
        height = self._display_surface.get_height()
        half_height = height // 2
        half_width = self._display_surface.get_width() // 2

        # Resize the grids and place them.
        grid_height = 0.9 * height
        grid_width = 0.9 * half_width
        x = (half_width - self.grid_to_be_filled.width) // 2
        y = (height - self.grid_to_be_filled.height) // 2
        self.grid_to_be_filled.update((grid_width, grid_height), (x, y))
        x += half_width
        self.grid_with_letter.update((grid_width, grid_height), (x, y))

    def on_double_lbutton(self, event):
        mouse_pos = pygame.mouse.get_pos()
        for letter in self.letters.all_letters:
            if letter.main_rectangle.collidepoint(mouse_pos):
                letter.flip()

    def on_lbutton_up(self, event):
        mouse_pos = pygame.mouse.get_pos()
        for letter in self.letters.all_letters:
            letter.selected = False
            if letter.main_rectangle.collidepoint(mouse_pos):
                letter.selected = True

        which_cell = self.grid_to_be_filled.which_cell(mouse_pos)
        print("grid_to_be_filled: ", which_cell)
        if which_cell is not None:
            pass

        which_cell = self.grid_with_letter.which_cell(mouse_pos)
        print("grid_with_letter: ", which_cell)
        if which_cell is not None:
            pass

    def on_render(self):

        # Reset the screen in white.
        self._display_surface.fill(Colors.white)

        # Drw the grids.
        self.grid_to_be_filled.draw()
        self.grid_with_letter.draw()

        # draw middle line.
        half_width = self._display_surface.get_width() // 2
        pygame.draw.line(
            self._display_surface,
            Colors.black,
            (half_width, 0),
            (
                half_width,
                self._display_surface.get_height(),
            ),
            2,
        )

        # update display
        pygame.display.flip()

    def on_exit(self):
        self._running = False

    def on_key_up(self, event):
        pass

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
