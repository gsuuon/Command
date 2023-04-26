open Gsuuon.Proc

open System
open System.Text.Json
open System.Text.RegularExpressions

let OPENAI_API_KEY = Environment.GetEnvironmentVariable "OPENAI_API_KEY"

type Item =
    | Empty
    | Done
    | Reason of string
    | Partial of string

let parse =
    function
    | "" | null ->
        Empty
    | evtText ->
        match Regex.Match(evtText, "data: (.+)") with
        | m when m.Success ->
            let data = m.Groups.[1].Value

            try
                let choice = (JsonDocument.Parse data).RootElement.GetProperty("choices").[0]

                match choice.GetProperty("delta").TryGetProperty("content") with
                | true, content ->
                    Partial <| content.GetString()
                | _ ->
                    match choice.GetProperty("finish_reason").GetString() with
                    | "" | null -> Empty
                    | reason -> Reason reason

            with
            | :? JsonException ->
                match m.Value with
                | "[DONE]" -> Done
                | x -> failwithf "Unparsed response: %s" x
            | e -> reraise ()
        | _ ->
            // ""
            Empty

let openai msg =
    let args = [
        "https://api.openai.com/v1/chat/completions"
        "-N"
        "-H"; "Content-Type: application/json"
        "-H"; $"Authorization: Bearer {OPENAI_API_KEY}"
        "-d"
        JsonSerializer.Serialize {|
          model = "gpt-3.5-turbo"
          messages = [|
              {|
                role = "user"
                content = msg
              |}
          |]
          stream = true
        |}
    ]

    let req = proc "curl" args host.stdin

    { req with
        stdout = req <!> Stdout |> transform (fun write line ->
            match parse line with
            | Partial x -> write x
            | Done -> write "\n" // make sure we have at least one newline
            | Reason "limit" -> clogn ConsoleColor.Red $"[Token limit]"
            | Empty |  _ -> ()
            )
    }
    
let alternate =
    let mutable count = 0

    let lined color msg = 
        clogn color $"%3d{count}|{msg}"

    fun (msg: string) ->
        if count % 2 = 0 then
            lined ConsoleColor.Green msg
        else
            lined ConsoleColor.Blue msg

        count <- count + 1

host.stdin |> read |> openai <!> Stdout |> consume (clog ConsoleColor.Green)
