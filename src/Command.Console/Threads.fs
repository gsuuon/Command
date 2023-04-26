module Gsuuon.Command.Console.Threads

let lockObj = obj ()

let log' printer txt = lock lockObj (fun _ -> printer txt)
