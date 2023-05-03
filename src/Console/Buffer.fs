module Gsuuon.Console.Buffer

open System

let ensureAvailableRows desiredRows =
    let bufferHeight = Console.BufferHeight
    let (col, row) = Console.GetCursorPosition().ToTuple()
    let availableRows = bufferHeight - row

    if availableRows < desiredRows then
        let additionalRows = desiredRows - availableRows
        Console.BufferHeight <- bufferHeight + additionalRows
