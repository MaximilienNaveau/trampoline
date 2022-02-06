#! /usr/env/bin python

import unicodedata
import pkg_resources
from pathlib import PurePath


class DictionaryChecker:
    def __init__(self):
        self.dictionary_fr_file_name = pkg_resources.resource_filename(
            "trampoline",
            str(
                PurePath(
                    "resources",
                    "french_dictionary",
                    "dictionary",
                    "dictionary.txt",
                )
            ),
        )
        with open(self.dictionary_fr_file_name) as f:
            self._dictionary = [
                DictionaryChecker.normalize_word(word)
                for line in f
                for word in line.split()
            ]

    @staticmethod
    def normalize_word(word):
        """
        Remove any accent in the string
        :param string: string to remove accents
        :type string: str or unicode
        :return: string without accents
        :rtype: str
        """
        try:
            local_word = unicode(word, "utf-8")
        except NameError:  # unicode is a default on python 3
            local_word = word

        local_word = (
            unicodedata.normalize("NFD", local_word)
            .encode("ascii", "ignore")
            .decode("utf-8")
        )
        return str(local_word).lower()

    def get_nth_word(self, line_index):
        for i, line in enumerate(self._dictionary):
            if i == line_index:
                return line

    def check_word_exists(self, word):
        if not word:
            return False

        if self.normalize_word(word) in self._dictionary:
            return True
        else:
            return False
