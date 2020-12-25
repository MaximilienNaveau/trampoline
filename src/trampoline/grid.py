#! /usr/env/bin python

import pygame
from trampoline.colors import Colors


class GridParams(object):
    pass


class Grid(object):
    def __init__(
        self, surface, cell_size=30, margin=0, color=Colors.black, axis_labels=False
    ):
        # Arguments:
        self.surface = surface
        self.cell_size = cell_size
        self.margin = margin
        self.color = color
        self.axis_labels = axis_labels

        self.cols = 9  # max(0, (surface.get_width() - 4 * self.margin) // cell_size)
        self.rows = 13  # max(0, (surface.get_height() - 4 * self.margin) // cell_size)
        self.line_width = 3

        self.grid = [[None for i in range(self.cols)] for j in range(self.rows)]
        self.font = pygame.font.SysFont("arial", 12, False)
        self.color = color

        self.x_min = self.margin
        self.x_max_surface = self.surface.get_width() - self.margin
        self.x_max = 0  # updated online

        self.y_min = self.margin
        self.y_max_surface = self.surface.get_height() - self.margin
        self.y_max = 0  # updated online

        print("cols = ", self.cols)
        print("rows = ", self.rows)

    def set_letter(self, letter, x, y):
        """  """
        self.grid[x][y] = letter

    def draw(self, x, y):
        """ Draw the grid on the given surface. """

        # Determin the origin of the grid using the margin.
        self.x_min = max(self.margin, x)
        self.y_min = max(self.margin, y)

        self.x_max = min(self.x_max_surface, self.x_min + self.cols * self.cell_size)
        self.y_max = min(self.y_max_surface, self.y_min + self.rows * self.cell_size)

        # Draw horizontal lines.
        for li in range(self.rows + 1):
            li_coord = self.y_min + li * self.cell_size
            # Add the labels at every line drawn.
            if self.axis_labels:
                if li < 10:
                    indent = "   "
                else:
                    indent = "  "
                text = self.font.render(indent + str(li), 1, (0, 0, 0))
                self.surface.blit(text, (0, li_coord))
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
            # Add the labels at every line drawn.# Draw the lines.
            if self.axis_labels:
                if co < 10:
                    ident = "  "
                else:
                    ident = " "
                text = self.font.render(ident + str(co), 1, (0, 0, 0))
                self.surface.blit(text, (colCoord, 1))
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
                if self.grid[row][col] is not None:
                    self.grid[row][col].resize(self.cell_size)
                    self.grid[row][col].draw(
                        self.x_min + col * self.cell_size,
                        self.y_min + row * self.cell_size,
                    )
