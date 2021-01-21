#! /usr/env/bin python

import pygame
from trampoline.colors import Colors


class Cell(object):
    def __init__(self):
        self.size = 0
        self.x = 0
        self.y = 0
        self.rectangle = pygame.Rect(0,0,0,0)
        self.letter = None

    def update(self, x, y, size):
        self.x = x
        self.y = y
        self.rectangle.x = x
        self.rectangle.y = y
        self.rectangle.width = size
        self.rectangle.height = size
        if not self.is_empty():
            self.letter.resize(size)

    def set_letter(self, letter):
        self.letter = letter

    def unset_letter(self):
        self.letter = None

    def has_selected_letter(self):
        if not self.is_empty():
            return self.letter.selected
        else:
            return False

    def draw(self):
        if not self.is_empty():
            self.letter.draw(self.x, self.y)

    def is_empty(self):
        if self.letter is None:
            return True
        else: 
            return False

    def collidepoint(self, point):
        if not self.is_empty():
            return self.letter.main_rectangle.collidepoint(point)
        else:
            return self.rectangle.collidepoint(point)


class Grid(object):
    def __init__(
        self,
        surface,
        color=Colors.black,
        axis_labels=False,
    ):
        # Arguments:
        self.surface = surface
        self.margin = 0
        self.color = color
        self.axis_labels = axis_labels

        self.cols = 9  # max(0, (surface.get_width() - 4 * self.margin) // cell_size)
        self.rows = 13  # max(0, (surface.get_height() - 4 * self.margin) // cell_size)
        self.line_width = 5

        # order row major
        self.grid = [Cell() for _ in range(self.rows * self.cols)]

        # set cell_size.
        self.update(grid_size=(400, 300), pos=(0, 0))

    def _get_cell_index(self, key: tuple):
        row, col = key
        return row * self.cols + col

    def __getitem__(self, key: tuple):
        return self.grid[self._get_cell_index(key)]

    def which_cell_clicked(self, mouse_pos):
        for row in range(self.rows):
            for col in range(self.cols):
                if self[row, col].collidepoint(mouse_pos):
                    return self[row, col]
        return None

    def which_cell_selected(self):
        for row in range(self.rows):
            for col in range(self.cols):
                if self[row, col].has_selected_letter():
                    return self[row, col]
        return None

    def update(self, grid_size: tuple, pos: tuple):
        """[summary]

        Args:
            grid_size (tuple): (width, height)
            x ([type]): [description]
            y ([type]): [description]
            surface ([type]): [description]
        """
        self.cell_size = min(grid_size[0] // self.cols, grid_size[1] // self.rows)
        self.width = self.cols * self.cell_size
        self.height = self.rows * self.cell_size

        self.x_max_surface = self.surface.get_width() - self.margin
        self.y_max_surface = self.surface.get_height() - self.margin

        # Determin the origin of the grid using the margin.
        self.x, self.y = pos
        self.x_min = max(self.margin, self.x)
        self.y_min = max(self.margin, self.y)

        self.x_max = min(self.x_max_surface, self.x_min + self.cols * self.cell_size)
        self.y_max = min(self.y_max_surface, self.y_min + self.rows * self.cell_size)

        for row in range(self.rows):
            for col in range(self.cols):
                self[row, col].update(
                    self.x_min + col * self.cell_size + self.line_width // 2 + 1,
                    self.y_min + row * self.cell_size + self.line_width // 2 + 1,
                    self.cell_size - self.line_width,
                )

    def draw(self):
        """ Draw the grid on the given surface. """

        # Draw horizontal lines.
        for li in range(self.rows + 1):
            li_coord = self.y_min + li * self.cell_size
            # Draw the lines.
            pygame.draw.line(
                self.surface,
                self.color,
                (self.x_min, li_coord),
                (self.x_max, li_coord),
                self.line_width,
            )
        # Draw the vertical lines.
        for co in range(self.cols + 1):
            colCoord = self.x_min + co * self.cell_size
            # Draw the lines.
            pygame.draw.line(
                self.surface,
                self.color,
                (colCoord, self.y_min),
                (colCoord, self.y_max),
                self.line_width,
            )

        for row in range(self.rows):
            for col in range(self.cols):
                self[row, col].draw()
