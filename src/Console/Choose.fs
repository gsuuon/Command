module Gsuuon.Console.Choose

open System
open System.Drawing

open Gsuuon.Console.Style
open Gsuuon.Console.Log

type private Direction =
    | Up
    | Down

type private Response =
    | Navigate of Direction
    | Number of int
    | Other of char
    | Cancel
    | Enter

let clearLine () =
    Console.Write('\r')
    Console.Write(new String(' ', Console.BufferWidth))
    Console.Write('\r')

let private getResponse () =
    let treatC = Console.TreatControlCAsInput
    let visible = Console.CursorVisible
    Console.TreatControlCAsInput <- true
    Console.CursorVisible <- false

    let response =
        let key = Console.ReadKey(true)

        match key.Key with
        | ConsoleKey.UpArrow
        | ConsoleKey.K -> Navigate Up
        | ConsoleKey.DownArrow
        | ConsoleKey.J -> Navigate Down
        | ConsoleKey.Enter -> Enter
        | ConsoleKey.Escape -> Cancel
        | ConsoleKey.C when key.Modifiers.HasFlag(ConsoleModifiers.Control) -> Cancel
        | _ ->
            let keyChar = key.KeyChar

            match Int32.TryParse(string keyChar) with
            | true, digit -> Number digit
            | x -> Other keyChar

    Console.TreatControlCAsInput <- treatC
    Console.CursorVisible <- visible

    response

let choose (description: string) startIdx (xs: 'a list) =
    let descLineCount = description.Split('\n').Length

    let showXs = xs |> List.map (fun x ->
        let content =
            match box x with
            | :? string as x -> x
            | _ -> sprintf "%A" x

        let lineCount = content.Split('\n').Length

        let content = content.Trim()

        {|
            content = content
            lineCount = lineCount
        |}
    )

    let xsLineCount = showXs |> List.sumBy (fun s -> s.lineCount)

    let (x, y) = Console.GetCursorPosition().ToTuple()

    Console.SetBufferSize(Console.BufferWidth, Console.BufferHeight + xsLineCount + descLineCount)

    Console.WriteLine description

    let chosenOne =
        let (x, y) = Console.GetCursorPosition().ToTuple()

        let rec choose' idx =
            showXs
            |> List.iteri (fun i x ->
                if i = idx then
                    slogn [ bg Color.Tan ] x.content
                else
                    logn x.content
                )

            Console.SetCursorPosition(x, y)

            match getResponse () with
            | Cancel -> None
            | Enter -> Some xs[idx]
            | Number n -> if n <= xs.Length then Some xs[n - 1] else choose' idx
            | Navigate Up -> choose' (Math.Max(idx - 1, 0))
            | Navigate Down -> choose' (Math.Min(idx + 1, showXs.Length - 1))
            | Other c -> choose' idx

        let result = choose' startIdx

        xs
        |> Seq.iter (fun _ -> Console.WriteLine(new String(' ', Console.BufferWidth)))

        result

    Console.SetCursorPosition(x, y)

    clearLine ()

    chosenOne
