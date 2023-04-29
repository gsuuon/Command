module Gsuuon.Command.Utility

open System

/// Prints using alternating printers and includes line number
/// Useful for debugging per-read output
let lineNumAlternating printA printB =
    let mutable count = 0

    fun (msg: string) ->
        let printer =
            if count % 2 = 0 then printA
            else printB

        printer $"%3d{count}|{msg}"

        count <- count + 1

/// Blocking sleep for ms
let sleep ms = Threading.Thread.Sleep 1000
