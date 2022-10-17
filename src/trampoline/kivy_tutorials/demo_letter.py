
from kivy.app import App
from kivy.core.window import Window
from kivy.lang.builder import Builder
from kivy.graphics import Color, Rectangle
from trampoline.kivy.colors import Colors
from trampoline.kivy.letter import AllLetters
from kivy.uix.gridlayout import GridLayout
from kivy.uix.boxlayout import BoxLayout

KV = """
FloatLayout:
	BoxLayout:
		id: chess_board
		orientation: "vertical"
"""

class MyApp(App):
	
    def build(self):
        Window.size = [700, 700]
        self.letters = AllLetters()
        return Builder.load_string(KV)

    def on_start(self):
        board = self.root.ids.chess_board
        for i in range(self.letters.rows):
            board_row = BoxLayout(orientation="horizontal")
            for j in range(self.letters.cols):
                board_row.add_widget(self.letters[i, j])            
            board.add_widget(board_row)

if __name__ == "__main__":
    MyApp().run()