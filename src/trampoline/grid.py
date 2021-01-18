#! /usr/env/bin python

import pygame
from trampoline.colors import Colors


class Cell(object):
    def __init__(self):
        self.size = 0
        self.rect = pygame.Rect(0, 0, 0, 0)
        self.letter = None

    def update(self, x, y, size):
        self.rect.x = x
        self.rect.y = y
        self.rect.width = size
        self.rect.height = size
        if not self.is_empty():
            self.letter.resize(size)

    def set_letter(self, letter):
        self.letter = letter

    def unset_letter(self):
        self.letter = None

    def draw(self, x, y):
        if not self.is_empty():
            self.letter.draw(x, y)

    def is_empty():
        return True if self.letter is None else False

    def collidepoint(self, point):
        return self.rect.collidepoint(point)


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

        # set cell_size.
        self.update((400, 300), (0, 0))

        # order row major
        self.grid = [Cell() for _ in range(self.rows * self.cols)]

        self.x_min = self.margin
        self.x_max_surface = self.surface.get_width() - self.margin
        self.x_max = 0  # updated online

        self.y_min = self.margin
        self.y_max_surface = self.surface.get_height() - self.margin
        self.y_max = 0  # updated online

    def __getitem__(self, key: tuple):
        row, col = key
        pose = col * self.cols + row
        return self.grid[pose]

    def which_cell(self, mouse_pos):
        for col in range(self.cols):
            for row in range(self.rows):
                if self[row, col].collidepoint(mouse_pos):
                    return (row, col)
                else:
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
                self.grid[row, col].draw()
                    self.grid[row][col].resize(self.cell_size - self.line_width)
                    self.grid[row][col].draw(
                        self.x_min + col * self.cell_size + self.line_width // 2 + 1,
                        self.y_min + row * self.cell_size + self.line_width // 2 + 1,
                    )
