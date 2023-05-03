module Gsuuon.Console.Styling

open System.Drawing
open System.Numerics

type StylingCommand =
    | Foreground of Color
    | Background of Color
    | Text of string

module Escape =
    // TODO probably belongs somewhere else
    let ESC = "\x1b"

    let sgr x = ESC + "[" + x + "m"

    let osc code args =
        ESC + "]" + code + ";" + (String.concat ";" args) + "\a"

    let reset = sgr "0"

    let rgb (color: Color) = $"{color.R};{color.G};{color.B}"

    let apply =
        function
        | Foreground color -> sgr ("38;2;" + rgb (color))
        | Background color -> sgr ("48;2;" + rgb (color))
        | Text text -> text

module Math =
    let vec (color: Color) =
        Vector3(float32 color.R, float32 color.G, float32 color.B)

    let unvec (vec: Vector3) =
        Color.FromArgb(int vec.X, int vec.Y, int vec.Z)

    let lerp (colorA: Color) (colorB: Color) (strengthB: float) =
        (vec colorA, vec colorB, float32 strengthB) |> Vector3.Lerp |> unvec
