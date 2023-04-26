module Gsuuon.Console.Style

open System.Drawing
open System.Numerics

type StyleCommand =
    | Foreground of Color
    | Background of Color
    | Text of string

module Escape =
    let escape x = "\x1b[" + x + "m"
    let reset = escape "0"

    let rgb (color: Color) = $"{color.R};{color.G};{color.B}"

    let apply =
        function
        | Foreground color -> escape ("38;2;" + rgb (color))
        | Background color -> escape ("48;2;" + rgb (color))
        | Text text -> text

module Math =
    let vec (color: Color) =
        Vector3(float32 color.R, float32 color.G, float32 color.B)

    let unvec (vec: Vector3) =
        Color.FromArgb(int vec.X, int vec.Y, int vec.Z)

    let lerp (colorA: Color) (colorB: Color) (strengthB: float) =
        (vec colorA, vec colorB, float32 strengthB)
        |> Vector3.Lerp
        |> unvec

let rgb (r, g, b) = Color.FromArgb(r, g, b)

let lighten color amount = Math.lerp color Color.White amount
let darken color amount = Math.lerp color Color.Black amount

let fg = Foreground
let bg = Background
let text = Text

let style (cmds: StyleCommand seq) =
    let out = cmds |> Seq.map Escape.apply |> String.concat ""
    out + Escape.reset
