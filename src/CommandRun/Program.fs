open Gsuuon.Proc

open System

proc "echo" "boop ya snoot" host.stdin <!> Stdout
|> tap (fun s -> clog ConsoleColor.Green $"echo: {s}")
|> proc "sed" "-u -e s/snoot/toot/" <!> Stdout
|> tap (fun s -> clog ConsoleColor.Blue $"sed: {s}")
|> ignore

System.Threading.Thread.Sleep 100
