module Gsuuon.Proc

open System
open System.IO
open System.Diagnostics

type PipeName =
    | Stdout
    | Stdin
    | Stderr
    | Combine of PipeName list

type Proc =
    { cmd: string
      args: string
      stdin: StreamWriter
      stdout: StreamReader
      stderr: StreamReader
      proc: Process }

    static member (<!>)(prev: Proc, name: PipeName) =
        match name with
        | Stdin ->
            // TODO Need to actually duplicate this
            new StreamReader(prev.stdin.BaseStream)
        | Stdout -> prev.stdout
        | Stderr -> prev.stderr
        | Combine names -> failwithf "🤷 what do?"

let setupConsole () =
    // Display emojis
    Console.OutputEncoding <- Text.Encoding.UTF8

    // Don't die on ctrl-c, but passthrough ctrl-c to child processes
    Console.CancelKeyPress.Add(fun e -> e.Cancel <- true)

let echo (reader: StreamReader) =
    (task {
        let mutable hasLine = true

        while hasLine do
            let! line = reader.ReadLineAsync()

            if line = null then
                hasLine <- false
            else
                printfn "%s" line
    })
        .Wait()

let tap handleLine (input: StreamReader) =
    let mem = new MemoryStream()
    let writer = new StreamWriter(mem)

    writer.AutoFlush <- true
    let reader = new StreamReader(mem)

    task {
        while true do
            let! line = input.ReadLineAsync()
            // line immediately returns null if we're just at the end of the stream
            // You'd think this blocks?
            // how do we find out if the stream has been closed?

            if line = null then // this is not correct?
                do! Threading.Tasks.Task.Delay 16
            else
                handleLine line

                handleLine (sprintf "[Tap pos start: %i]" mem.Position)
                let originalPosition = mem.Position
                mem.Seek(0, SeekOrigin.End) |> ignore
                writer.WriteLine(line)
                handleLine (sprintf "[Tap pos write: %i]" mem.Position)
                mem.Seek(originalPosition, SeekOrigin.Begin) |> ignore
                handleLine (sprintf "[Tap pos end: %i]" mem.Position)
    } |> ignore

    reader

let read (reader: StreamReader) = reader.ReadToEnd()

let from (text: string) =
    let mem = new MemoryStream() // no ctor means expandable

    mem.Write(Text.Encoding.UTF8.GetBytes(text))
    mem.Seek(0, SeekOrigin.Begin) |> ignore

    new StreamReader(mem)

let proc (cmd: string) (args: string) (input: StreamReader) =
    let processStartInfo = new ProcessStartInfo()

    processStartInfo.FileName <- "cmd.exe"
    processStartInfo.Arguments <- $"/c {cmd} {args}"
    processStartInfo.UseShellExecute <- false
    processStartInfo.RedirectStandardOutput <- true
    processStartInfo.RedirectStandardError <- true
    processStartInfo.RedirectStandardInput <- true

    let p = new Process()
    p.StartInfo <- processStartInfo

    p.Start() |> ignore

    let stdin = p.StandardInput
    let stdout = p.StandardOutput
    let stderr = p.StandardError

    // TODO stream the stream
    // read all of input into stdin then close
    let inp = input.ReadToEnd()
    printfn "<%s> [%s]" cmd inp
    stdin.Write(inp)
    stdin.Close()
    // I tried for a long time to make this actually stream, without closing, but it just doesn't work
    // the dotnet stream api seems super thorny
    // getting .Length can block forever, checking .EndOfStream can block forever, who knows what else :/

    { cmd = cmd
      args = args
      stdin = stdin
      stdout = stdout
      stderr = stderr
      proc = p }

let wait proc =
    proc.proc.WaitForExit()
    proc

let host =
    {| stdin = new StreamReader(Console.OpenStandardInput())
       stdout = new StreamWriter(Console.OpenStandardOutput())
       stderr = new StreamWriter(Console.OpenStandardError()) |}
