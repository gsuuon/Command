module Gsuuon.Console.Vterm

// https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences#text-modification
open System

module Control =
    [<Literal>]
    let charESC = '\x1b'

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
    let cursorShow = CSI + "?25h"
    let cursorHide = CSI + "?25l"
    let cursorPosition col row = CSI + $"{row};{col}H"
    let cursorUpLines n = CSI + (string n) + "F"
    let cursorDownLines n = CSI + (string n) + "E"
    let queryCursorPosition = CSI + "6n"
    let linesDelete n = CSI + string n + "M"
    let linesInsert n = CSI + string n + "L"

module Query =
    open System.Text.RegularExpressions

    /// Reads until stopChar if next char is startChar, else returns None
    let readTil startChar stopChar =
        let rec readTil' response  =
            let c = Console.ReadKey(true).KeyChar
            let res = response + string c

            if c = stopChar then res
            else readTil' res

        let c = Console.ReadKey(true).KeyChar

        if c = startChar then
            Some <| readTil' (string c)
        else
            None

    let readCursorPosition () =
        readTil Control.charESC 'R'
        |> Option.map (fun x ->
            let m = Regex.Match(x, Control.ESC + "\[(?<row>\d+);(?<col>\d+)R")
            struct(Int32.Parse m.Groups["col"].Value, Int32.Parse m.Groups["row"].Value)
        )

    /// Returns struct(col, row)
    let getCursorPosition out =
        out Operation.queryCursorPosition
        readCursorPosition ()
