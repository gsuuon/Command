module Gsuuon.Console.Buffer

open System
open Gsuuon.Console.Vterm

/// Write to stdout
let out (x: string) = Console.Write x

/// Write to stderr
let err (x: string) = Console.Error.Write x

/// Ensure buffer has additional rows available by displaying newlines
let ensureAvailableRows display desiredRows =
    let bufferHeight = Console.BufferHeight
    let struct(col, row) = Query.getCursorPosition display
    let availableRows = bufferHeight - row

    if availableRows < desiredRows then
        let additionalRows = desiredRows - availableRows
        [0..additionalRows]
        |> Seq.iter (fun _ -> display "\n")

        display (Operation.cursorPosition col (row - additionalRows))

/// Run an Async in an alternate buffer, returning the async result
let alternateBuffer (f: Async<_>) =
    err Operation.alternateBuffer
    err Operation.cursorHide

    err (Operation.cursorPosition 0 0)
    // Console.SetCursorPosition(0, 0)
    let result = f |> Async.RunSynchronously
    err Operation.mainBuffer
    result
