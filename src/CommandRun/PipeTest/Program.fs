open System
open System.Diagnostics
open System.IO

let findstr =
    let proc = new Process()
    proc.StartInfo.FileName <- "findstr"
    proc.StartInfo.Arguments <- "x*"

    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardInput <- true
    proc.StartInfo.RedirectStandardOutput <- true

    proc.Start() |> ignore

    proc

let stdin = Console.OpenStandardInput()

let reader = new StreamReader(stdin)
let line = reader.ReadToEnd()
printfn "read %s" line

// findstr.OutputDataReceived.Add (fun x ->
//     printfn "findstr output: %s" x.Data
// )


// printfn "before begin"
// findstr.BeginOutputReadLine()
// printfn "after begin"

let readTask =(task {
    let reader = findstr.StandardOutput
    printfn "task"
    while reader.BaseStream.CanRead do
        // this is always true
        // no nice way to figure out if the output closed?
        printfn "loop"
        let! line = reader.ReadLineAsync()
        printfn "findstr: %s" line
    printfn "reader ended"
})

findstr.StandardInput.WriteLine line
printfn "wrote: %s" line

// How do I get findstr to process the line above without closing StandardInput?
findstr.StandardInput.Close()

// Flush doesn't seem to work
// findstr.StandardInput.Flush()

// maybe this is the problem?


// findstr.StandardOutput.ReadToEnd() |> printfn "output: %s"

findstr.WaitForExit()

readTask.Wait()
