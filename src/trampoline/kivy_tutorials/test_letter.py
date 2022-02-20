from tkinter import Label
from kivy.uix.widget import Widget
from kivy.app import App
from kivy.lang import Builder
from kivy.graphics import Color, Rectangle, Line


from trampoline.kivy.letter import Letter


# Builder.load_string('''

# <Letter>:
#         face1_letter: "A"
#         face1_color: [0.94117647, 0.90196078, 0.54901961, 1.]
#         face2_letter: "B"
#         face2_color: [154, 205, 50, 255]
#         face1_up: True

# <Demo>:
#         Letter:
#                 id: letter1
#                 face1_letter: "2"
#                 face2_letter: "1"
#                 pos: self.parent.center
#                 canvas:
#                         Color:
#                                 rgba: 1, 1, 1, 1
#                         Rectangle:
#                                 pos: self.parent.pos
#                                 size: self.size
    
# ''')


class Letter(Widget):
    _face1_color = [0.94117647, 0.90196078, 0.54901961, 1.]
    white = [1,1,1,1]
    black = [0,0,0,1]

    def __init__(self, **kwargs):
        super(Letter, self).__init__(**kwargs)
        self.bind(pos=self.update_canvas)
        self.bind(size=self.update_canvas)
        self.update_canvas()

    def update_canvas(self, *args):
        self.canvas.clear()
        with self.canvas:
            # Color(self.white)
            # Rectangle(pos=self.pos, size=self.size)
            Color(*self.white)
            Rectangle(pos=self.pos, size=(self.size[0]*0.35, self.size[1] * 0.35))

class DemoApp(App):
    def build(self):
        return Letter()


if __name__ == '__main__':
    DemoApp().run()