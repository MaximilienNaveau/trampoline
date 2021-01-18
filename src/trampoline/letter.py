#! /usr/env/bin python

import pygame
from trampoline.colors import Colors
from trampoline import pygame_text as ptext


class Letter(object):
    def __init__(
        self,
        surface,
        face1_letter,
        face2_letter,
        face1_color=Colors.yellow,
        face2_color=Colors.green,
        font_name="Arial",
    ):
        self.surface = surface
        self.face1_letter = face1_letter
        self.face2_letter = face2_letter
        self.face1_color = face1_color
        self.face2_color = face2_color
        self.font_name = font_name

        self.selected = False
        self._face1_visible = False
        self._face2_visible = True
        self._visible_face = self._face1_visible
        self.main_rectangle = pygame.Rect(0, 0, 0, 0)
        self.small_rectangle = pygame.Rect(0, 0, 0, 0)
        self.size = 40
        self.resize(self.size)

    def set_letters(self, face1_letter, face2_letter):
        self.face1_letter = face1_letter
        self.face2_letter = face2_letter

    def set_colors(self, face1_color, face2_color):
        self.face1_color = face1_color
        self.face2_color = face2_color

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

        if self._visible_face == self._face1_visible:
            main_letter = self.face1_letter
            main_color = self.face1_color
            secondary_letter = self.face2_letter
            secondary_color = self.face2_color
        else:
            main_letter = self.face2_letter
            main_color = self.face2_color
            secondary_letter = self.face1_letter
            secondary_color = Colors.yellow

        main_letter_color = Colors.black
        # if main_color == Colors.green:
        #     main_letter_color = Colors.white
        secondary_letter_color = Colors.black
        # if secondary_color == Colors.green:
        #     secondary_letter_color = Colors.white

        self.surface.fill(main_color, self.main_rectangle)
        self.surface.fill(secondary_color, self.small_rectangle)
        ptext.drawbox(
            main_letter,
            self.main_rectangle.inflate(
                int(-0.3 * self.main_rectangle.width),
                int(-0.3 * self.main_rectangle.height),
            ).move(int(-0.18 * self.main_rectangle.width), 0),
            color=main_letter_color,
        )
        ptext.drawbox(
            secondary_letter, self.small_rectangle, color=secondary_letter_color
        )
        pygame.draw.rect(self.surface, Colors.black, self.small_rectangle, width=1)
        if self.selected:
            pygame.draw.rect(self.surface, Colors.blue, self.main_rectangle, width=3)


class AllLetters(object):
    def __init__(self, surface):
        self.rows = 13
        self.cols = 9
        self.all_letters = [
            Letter(surface, "A", "B") for _ in range(self.rows * self.cols)
        ]

        # row 0
        self.all_letters[0].set_letters("A", "E")
        self.all_letters[1].set_letters("A", "I")
        self.all_letters[2].set_letters("A", "M")
        self.all_letters[3].set_letters("A", "O")
        self.all_letters[4].set_letters("A", "R")
        self.all_letters[5].set_letters("A", "S")
        self.all_letters[6].set_letters("A", "T")
        self.all_letters[7].set_letters("A", "U")
        self.all_letters[8].set_letters("B", "E")

        # row 0
        self.all_letters[self.cols + 0].set_letters("B", "N")
        self.all_letters[self.cols + 1].set_letters("B", "T")
        self.all_letters[self.cols + 2].set_letters("C", "A")
        self.all_letters[self.cols + 3].set_letters("C", "I")
        self.all_letters[self.cols + 4].set_letters("C", "N")
        self.all_letters[self.cols + 5].set_letters("C", "T")
        self.all_letters[self.cols + 6].set_letters("D", "C")
        self.all_letters[self.cols + 7].set_letters("D", "N")
        self.all_letters[self.cols + 8].set_letters("D", "R")

        # row 2
        self.all_letters[2 * self.cols + 0].set_letters("E", "-")
        self.all_letters[2 * self.cols + 1].set_letters("E", "-")
        self.all_letters[2 * self.cols + 2].set_letters("E", "-")
        self.all_letters[2 * self.cols + 3].set_letters("E", "-")
        self.all_letters[2 * self.cols + 4].set_letters("E", "-")
        self.all_letters[2 * self.cols + 5].set_letters("E", "-")
        self.all_letters[2 * self.cols + 6].set_letters("E", "-")
        self.all_letters[2 * self.cols + 7].set_letters("E", "C")
        self.all_letters[2 * self.cols + 8].set_letters("E", "D")

        # row 3
        self.all_letters[3 * self.cols + 0].set_letters("E", "G")
        self.all_letters[3 * self.cols + 1].set_letters("E", "H")
        self.all_letters[3 * self.cols + 2].set_letters("E", "L")
        self.all_letters[3 * self.cols + 3].set_letters("E", "N")
        self.all_letters[3 * self.cols + 4].set_letters("E", "O")
        self.all_letters[3 * self.cols + 5].set_letters("E", "S")
        self.all_letters[3 * self.cols + 6].set_letters("E", "T")
        self.all_letters[3 * self.cols + 7].set_letters("E", "Y")
        self.all_letters[3 * self.cols + 8].set_letters("E", "Z")

        # row 4
        self.all_letters[4 * self.cols + 0].set_letters("F", "A")
        self.all_letters[4 * self.cols + 1].set_letters("F", "E")
        self.all_letters[4 * self.cols + 2].set_letters("F", "T")
        self.all_letters[4 * self.cols + 3].set_letters("G", "A")
        self.all_letters[4 * self.cols + 4].set_letters("G", "I")
        self.all_letters[4 * self.cols + 5].set_letters("H", "A")
        self.all_letters[4 * self.cols + 6].set_letters("H", "T")
        self.all_letters[4 * self.cols + 7].set_letters("I", "B")
        self.all_letters[4 * self.cols + 8].set_letters("I", "D")

        # row 5
        self.all_letters[5 * self.cols + 0].set_letters("I", "E")
        self.all_letters[5 * self.cols + 1].set_letters("I", "F")
        self.all_letters[5 * self.cols + 2].set_letters("I", "M")
        self.all_letters[5 * self.cols + 3].set_letters("I", "O")
        self.all_letters[5 * self.cols + 4].set_letters("I", "R")
        self.all_letters[5 * self.cols + 5].set_letters("I", "S")
        self.all_letters[5 * self.cols + 6].set_letters("I", "U")
        self.all_letters[5 * self.cols + 7].set_letters("J", "E")
        self.all_letters[5 * self.cols + 8].set_letters("K", "U")
        self.all_letters[5 * self.cols + 8].set_colors(Colors.yellow, Colors.yellow)

        # row 6
        self.all_letters[6 * self.cols + 0].set_letters("L", "A")
        self.all_letters[6 * self.cols + 1].set_letters("L", "D")
        self.all_letters[6 * self.cols + 2].set_letters("L", "I")
        self.all_letters[6 * self.cols + 3].set_letters("L", "S")
        self.all_letters[6 * self.cols + 4].set_letters("M", "E")
        self.all_letters[6 * self.cols + 5].set_letters("M", "S")
        self.all_letters[6 * self.cols + 6].set_letters("M", "U")
        self.all_letters[6 * self.cols + 7].set_letters("N", "-")
        self.all_letters[6 * self.cols + 8].set_letters("N", "A")

        # row 7
        self.all_letters[7 * self.cols + 0].set_letters("N", "F")
        self.all_letters[7 * self.cols + 1].set_letters("N", "I")
        self.all_letters[7 * self.cols + 2].set_letters("N", "L")
        self.all_letters[7 * self.cols + 3].set_letters("N", "P")
        self.all_letters[7 * self.cols + 4].set_letters("N", "Q")
        self.all_letters[7 * self.cols + 5].set_letters("N", "T")
        self.all_letters[7 * self.cols + 6].set_letters("O", "-")
        self.all_letters[7 * self.cols + 7].set_letters("O", "C")
        self.all_letters[7 * self.cols + 8].set_letters("O", "F")

        # row 8
        self.all_letters[8 * self.cols + 0].set_letters("O", "J")
        self.all_letters[8 * self.cols + 1].set_letters("O", "R")
        self.all_letters[8 * self.cols + 2].set_letters("O", "U")
        self.all_letters[8 * self.cols + 3].set_letters("P", "A")
        self.all_letters[8 * self.cols + 4].set_letters("P", "E")
        self.all_letters[8 * self.cols + 5].set_letters("P", "I")
        self.all_letters[8 * self.cols + 6].set_letters("Q", "E")
        self.all_letters[8 * self.cols + 7].set_letters("Q", "I")
        self.all_letters[8 * self.cols + 8].set_letters("R", "B")

        # row 9
        self.all_letters[9 * self.cols + 0].set_letters("R", "C")
        self.all_letters[9 * self.cols + 1].set_letters("R", "E")
        self.all_letters[9 * self.cols + 2].set_letters("R", "G")
        self.all_letters[9 * self.cols + 3].set_letters("R", "H")
        self.all_letters[9 * self.cols + 4].set_letters("R", "M")
        self.all_letters[9 * self.cols + 5].set_letters("R", "N")
        self.all_letters[9 * self.cols + 6].set_letters("R", "P")
        self.all_letters[9 * self.cols + 7].set_letters("R", "V")
        self.all_letters[9 * self.cols + 8].set_letters("S", "-")

        # row 10
        self.all_letters[10 * self.cols + 0].set_letters("S", "-")
        self.all_letters[10 * self.cols + 1].set_letters("S", "B")
        self.all_letters[10 * self.cols + 2].set_letters("S", "N")
        self.all_letters[10 * self.cols + 3].set_letters("S", "O")
        self.all_letters[10 * self.cols + 4].set_letters("S", "R")
        self.all_letters[10 * self.cols + 5].set_letters("S", "U")
        self.all_letters[10 * self.cols + 6].set_letters("S", "X")
        self.all_letters[10 * self.cols + 7].set_letters("T", "-")
        self.all_letters[10 * self.cols + 8].set_letters("T", "I")

        # row 11
        self.all_letters[11 * self.cols + 0].set_letters("T", "L")
        self.all_letters[11 * self.cols + 1].set_letters("T", "O")
        self.all_letters[11 * self.cols + 2].set_letters("T", "R")
        self.all_letters[11 * self.cols + 3].set_letters("T", "S")
        self.all_letters[11 * self.cols + 4].set_letters("T", "U")
        self.all_letters[11 * self.cols + 5].set_letters("T", "V")
        self.all_letters[11 * self.cols + 6].set_letters("U", "E")
        self.all_letters[11 * self.cols + 7].set_letters("U", "L")
        self.all_letters[11 * self.cols + 8].set_letters("U", "N")

        # row 12
        self.all_letters[12 * self.cols + 0].set_letters("U", "P")
        self.all_letters[12 * self.cols + 1].set_letters("U", "Q")
        self.all_letters[12 * self.cols + 2].set_letters("U", "R")
        self.all_letters[12 * self.cols + 3].set_letters("V", "E")
        self.all_letters[12 * self.cols + 4].set_letters("V", "S")
        self.all_letters[12 * self.cols + 5].set_letters("W", "S")
        self.all_letters[12 * self.cols + 5].set_colors(Colors.yellow, Colors.yellow)
        self.all_letters[12 * self.cols + 6].set_letters("X", "E")
        self.all_letters[12 * self.cols + 7].set_letters("Y", "O")
        self.all_letters[12 * self.cols + 8].set_letters("Z", "R")

    def __getitem__(self, key: tuple):
        row, col = key
        pose = col * self.cols + row
        return self.all_letters[pose]