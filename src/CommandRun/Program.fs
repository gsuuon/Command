open Gsuuon.Proc

open System.IO

// host.stdin |> echo
// proc "echo" "boop" host.stdin <!> Stdout |> echo
// proc "findstr" "h" host.stdin <!> Stdout  |> echo
// proc "sed" "-e s/hi/hello/" host.stdin <!> Stdout |> echo

// let out = proc "echo" "feafs" host.stdin <!> Stdout |> proc "sed" "-e s/hi/hello/" <!> Stdout |> read

// proc "sed" "-e s/hi/hello/" (from out) <!> Stdout |> echo

// proc ("echo", "hi") <!> Stdout |> echo

// proc "echo" "hey" host.stdin <!> Stdout |> tap (printfn "x: %s") |> tap (printfn "y: %s") |> ignore

//from "boop"
proc "echo" "boop" host.stdin <!> Stdout
|> tap (printfn "x: %s")
|> tap (printfn "y: %s") 
|> ignore

System.Threading.Thread.Sleep 100
