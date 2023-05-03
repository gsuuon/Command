module Gsuuon.Console.Choose

open System
open System.Drawing

open Gsuuon.Console.Style
open Gsuuon.Console.Buffer
open Gsuuon.Console.Vterm

type private Direction =
    | Up
    | Down

type private Response =
    | Navigate of Direction
    | Number of int
    | Other of char
    | Cancel
    | Enter

let private getResponse () =
    let treatC = Console.TreatControlCAsInput
    Console.TreatControlCAsInput <- true

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

    response

let choose' display (description: string) startIdx (xs: 'a list) =
    let descLineCount = description.Split('\n').Length

    let showXs =
        xs
        |> List.map (fun x ->
            let content =
                match box x with
                | :? string as x -> x
                | _ -> sprintf "%A" x

            let lineCount = content.Split('\n').Length

            {| content = content
               lineCount = lineCount |})

    let xsLineCount = showXs |> List.sumBy (fun s -> s.lineCount)
    let neededRows = xsLineCount + descLineCount

    let struct (_, y) = Query.getCursorPosition display

    ensureAvailableRows display neededRows
    display Operation.cursorHide
    display (description + "\n")

    let chosen =
        let rec choose' idx =
            showXs
            |> List.iteri (fun i x ->
                if i = idx then
                    display (stext [ bg Color.Tan ] x.content)
                else
                    display x.content

                display "\n")

            display (Operation.cursorUpLines xsLineCount)

            match getResponse () with
            | Cancel -> None
            | Enter -> Some xs[idx]
            | Number n -> if n <= xs.Length then Some xs[n - 1] else choose' idx
            | Navigate Up -> choose' (Math.Max(idx - 1, 0))
            | Navigate Down -> choose' (Math.Min(idx + 1, showXs.Length - 1))
            | Other c -> choose' idx

        let result = choose' startIdx

        result

    display (Operation.linesDelete neededRows)
    display Operation.cursorShow

    chosen

let choose x = choose' Console.Error.Write x
