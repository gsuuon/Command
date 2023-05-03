module Gsuuon.Console.Vterm

// https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences#text-modification

module Control =
    [<Literal>]
    let ESC = "\x1b"

    [<Literal>]
    let OSC = ESC + "]"

    [<Literal>]
    let CSI = ESC + "["

    [<Literal>]
    let ST = ESC + "\\"

    let SGR x = CSI + x + "m"

module Operation =
    open Control

    let eraseLine = CSI + "2K"
    let eraseScreen = CSI + "2J"
    let alternateBuffer = CSI + "?1049h"
    let mainBuffer = CSI + "?1049l"
    let showCursor = CSI + "?25h"
    let hideCursor = CSI + "?25l"
