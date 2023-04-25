open Gsuuon.Proc

open System

// let goog = proc "curl" "-N www.google.com" host.stdin |> complete

// goog <!> Stderr |> read |> clog ConsoleColor.Red
// goog <!> Stdout |> read |> clog ConsoleColor.Green


let OPENAI_API_KEY = Environment.GetEnvironmentVariable "OPENAI_API_KEY"

let openai msg =
    let args = $"""https://api.openai.com/v1/chat/completions
      -N \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer {OPENAI_API_KEY} \
      -d '{{
        "model": "gpt-3.5-turbo",
        "messages": [{{"role": "user", "content": "{msg}"}}],
        "streaming": true
      }}'
    """
    clog ConsoleColor.Blue args
    proc "curl" args host.stdin


