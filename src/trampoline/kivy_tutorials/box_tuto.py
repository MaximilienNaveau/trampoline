from kivy.uix.gridlayout import Widget
from kivy.app import App
from kivy.lang import Builder

Builder.load_string('''
<Demo>:
    Letter:
        
''')


class Demo(Widget):
    pass


class DemoApp(App):
    def build(self):
        return Demo()


if __name__ == '__main__':
    DemoApp().run()