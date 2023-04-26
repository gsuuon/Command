namespace Gsuuon.Console

module Style =
    open System.Drawing
    open Gsuuon.Console.Styling

    let rgb (r, g, b) = Color.FromArgb(r, g, b)

    let lighten color amount = Math.lerp color Color.White amount
    let darken color amount = Math.lerp color Color.Black amount

    let fg = Foreground
    let bg = Background
    let text = Text

    /// Apply StyleCommands and reset
    let style (cmds: StyleCommand seq) =
        let out = cmds |> Seq.map Escape.apply |> String.concat ""
        out + Escape.reset

module Utility =
    /// Set shell title
    let title text = Escape.osc "2" [ text ]

    /// Send desktop notification
    let notify text = Escape.osc "9" [ text ]

    /// Show inline image via iTerm2 protocol
    let imgIterm2 base64Image =
        Escape.osc
            "1337"
            [ "File="
              // "size=" + string bytes.Length
              "preserveAspectRatio=1"
              // "height=512px"
              // "width=512px"
              "doNotMoveCursor=0"
              "inline=1:" + base64Image ]

module Log =
    open Gsuuon.Console.Threads

    let log = printf "%s" |> log'
    let logn = printfn "%s" |> log'

    let slog styles msg =
        Style.style (styles @ [ Style.text msg ]) |> log

    let slogn styles msg =
        Style.style (styles @ [ Style.text msg ]) |> logn
