#! /usr/env/bin python

import pygame
from trampoline.colors import Colors
from trampoline.dictionary_checker import DictionaryChecker


class Word(object):
    def __init__(self, dictionary_checker):
        self._dictionary_checker = dictionary_checker
        self.list_of_cells = []

    def length(self):
        return len(self.list_of_cells)

    def is_valid(self):
        word = self.to_string()
        return self._dictionary_checker.check_word_exists(word)

    def to_string(self):
        word = ""
        for cell in self.list_of_cells:
            word += cell.letter.main_letter
        return word

    def get_convex_hull(self):
        list_x = []
        list_y = []
        for cell in self.list_of_cells:
            list_x.append(cell.rectangle.left)
            list_x.append(cell.rectangle.right)
            list_y.append(cell.rectangle.top)
            list_y.append(cell.rectangle.bottom)

        x = min(list_x)
        y = min(list_y)
        width = max(list_x) - x
        height = max(list_y) - y
        return pygame.Rect(x, y, width, height)


class WordChecker(object):
    def __init__(self, grid):
        """Constructor

        Args:
            grid (trampoline.Grid): Grid to be filled.
        """
        self._grid = grid
        self._surface = self._grid.surface
        self._dictionary_checker = DictionaryChecker()
        self._words = [
            Word(self._dictionary_checker) for i in range(self._grid.rows)
        ]
        self._red_rectangles = []
        self._green_rectangles = []
        self.valid_words = []

    def check_words(self):
        self._red_rectangles = []
        self._green_rectangles = []
        self.valid_words = []

        for i in range(self._grid.rows):
            # Create the word
            word = self._words[i]
            word.list_of_cells = []
            for j in range(self._grid.cols):
                if not self._grid[i, j].is_empty():
                    word.list_of_cells.append(self._grid[i, j])
                else:
                    break

            if word.is_valid():
                self._green_rectangles.append(word.get_convex_hull())
                self.valid_words.append(word)
            else:
                if word.length() != 0:
                    self._red_rectangles.append(word.get_convex_hull())

    def draw(self):
        mouse_pose = pygame.mouse.get_pos()
        for rect in self._red_rectangles:
            if rect.collidepoint(mouse_pose):
                pygame.draw.rect(
                    self._surface, Colors.invalid_red, rect, width=6
                )
        for rect in self._green_rectangles:
            if rect.collidepoint(mouse_pose):
                pygame.draw.rect(
                    self._surface, Colors.valid_green, rect, width=6
                )
