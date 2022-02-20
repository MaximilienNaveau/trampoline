#! /usr/env/bin python

import numpy as np


def to_kivy_format(r,g,b,a):
    return (np.array([r, g, b, a]) / 255.0).tolist()


class Colors(object):
    """Collection of all the colors used in the game. """

    black = to_kivy_format(0, 0, 0, 255)
    white = to_kivy_format(255, 255, 255, 255)
    green = to_kivy_format(154, 205, 50, 255)
    yellow = kaki = to_kivy_format(240, 230, 140, 255)
    wood = burlywood = to_kivy_format(222, 184, 135, 255)
    blue = to_kivy_format(0, 0, 204, 255)
    valid_green = to_kivy_format(20, 100, 20, 255)
    invalid_red = to_kivy_format(255, 0, 0, 255)
