#! /usr/env/bin python

import numpy as np
from kivy.core.text import Label as CoreLabel
from kivy.graphics import Color, Rectangle
from trampoline.kivy.colors import Colors
from kivy.uix.widget import Widget


class FaceSettings(object):
    def __init__(self, letter, background_color, letter_color):
        self.letter = letter
        self.background_color = background_color
        self.letter_color = letter_color


class Letter(Widget):
    def __init__(
        self,
        letter1="A",
        letter1_bg=Colors.yellow,
        letter1_color=Colors.black,
        letter2="A",
        letter2_bg=Colors.green,
        letter2_color=Colors.black,
        **kwargs
    ):
        super(Letter, self).__init__(**kwargs)
        self._face1 = FaceSettings(letter1, letter1_bg, letter1_color)
        self._face2 = FaceSettings(letter2, letter2_bg, letter2_color)
        self._face1_up = True
        self._face_up = self._face1
        self._face_down = self._face2
        self.pos = (100, 100)
        self.size = (10, 10)
        self.bind(pos=self.update_canvas)
        self.bind(size=self.update_canvas)
        self.update_canvas()

    def set_letters(self, face1_letter, face2_letter):
        self._face1.letter = face1_letter
        self._face2.letter = face2_letter

    def set_colors(self, face1_color, face2_color):
        self._face1.background_color = face1_color
        self._face2.background_color = face2_color

    def update_canvas(self, *args):
        self.canvas.clear()
        with self.canvas:
            if self._face1_up:
                self._face_up = self._face1
                self._face_down = self._face2
            else:
                self._face_down = self._face1
                self._face_up = self._face2

            # get the current location size.
            current_size = (
                self.width,
                self.height,
            )  # min(self.width, self.height)
            # Black surrounding of the token
            Color(*Colors.black)
            Rectangle(pos=self.pos, size=current_size)
            # Primary color square.
            psize = (0.95 * np.array(current_size)).tolist()
            ppose = (
                np.array(self.pos)
                + 0.5 * (np.array(current_size) - np.array(psize))
            ).tolist()
            Color(*self._face_up.background_color)
            rect = Rectangle(pos=ppose, size=psize)
            # Main letter drawing.
            self._draw_a_letter(
                rect,
                self._face_up.letter,
                self._face_up.letter_color,
                pos_offset=(-0.1 * np.array(psize)).tolist(),
            )
            # Secondary color square.
            ssize = (np.array(psize) * 0.35).tolist()
            spos = (np.array(ppose) + (1 - 0.35) * np.array(psize)).tolist()
            Color(*self._face_down.background_color)
            rect = Rectangle(pos=spos, size=ssize)
            # Secondary letter
            self._draw_a_letter(
                rect,
                self._face_down.letter,
                self._face_down.letter_color,
            )

    def _draw_a_letter(self, rect, letter, color, pos_offset=(0, 0)):
        with self.canvas:
            label = CoreLabel(
                text=letter,
                font_size=0.8 * rect.size[0],
                color=color,
            )
            label.refresh()
            texture = label.texture
            texture_size = list(texture.size)
            label_pos = list(
                rect.pos[i] + (rect.size[i] - texture.size[i]) / 2
                for i in range(2)
            )
            label_pos[0] += pos_offset[0]
            label_pos[1] += pos_offset[1]
            Rectangle(texture=texture, size=texture_size, pos=label_pos)


class AllLetters(object):
    def __init__(self):
        self.rows = 13
        self.cols = 9
        self.all_letters = [
            Letter() for _ in range(self.rows * self.cols)
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

        # row 1
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
        self.all_letters[5 * self.cols + 8].set_colors(
            Colors.yellow, Colors.yellow
        )

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
        self.all_letters[12 * self.cols + 5].set_colors(
            Colors.yellow, Colors.yellow
        )
        self.all_letters[12 * self.cols + 6].set_letters("X", "E")
        self.all_letters[12 * self.cols + 7].set_letters("Y", "O")
        self.all_letters[12 * self.cols + 8].set_letters("Z", "R")

    def __getitem__(self, key: tuple):
        row, col = key
        pose = row * self.cols + col
        return self.all_letters[pose]
