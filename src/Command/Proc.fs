module Gsuuon.Proc

open System
open System.IO
open System.Diagnostics

type ProcStream =
    | Stdout
    | Stdin
    | Stderr
    | Combine of ProcStream list

type Proc =
    { cmd: string
      args: string
      stdin: StreamWriter
      stdout: StreamReader
      stderr: StreamReader
      proc: Process }

    static member (<!>) (prev: Proc, name: ProcStream) =
        match name with
        | Stdin ->
            // TODO I need to actually duplicate this
            new StreamReader(prev.stdin.BaseStream)
        | Stdout -> prev.stdout
        | Stderr -> prev.stderr
        | Combine names -> failwithf "🤷 what do?"

    static member Create (cmd: string) (args: string) (input: StreamReader) =
        let processStartInfo = new ProcessStartInfo()

        processStartInfo.FileName <- "cmd.exe"
        processStartInfo.Arguments <- $"/c {cmd} {args}"
        processStartInfo.UseShellExecute <- false
        processStartInfo.RedirectStandardOutput <- true
        processStartInfo.RedirectStandardError <- true
        processStartInfo.RedirectStandardInput <- true

        let proc = new Process()
        proc.StartInfo <- processStartInfo

        proc.Start() |> ignore

        let stdin = proc.StandardInput
        let stdout = proc.StandardOutput
        let stderr = proc.StandardError

        // read all of input into stdin then close
        let inp = input.ReadToEnd()
        printfn "<%s> [%s]" cmd inp
        stdin.Write(inp)
        stdin.Close()
            // I tried for a long time to make this actually stream, without closing, but it just doesn't work
            // the dotnet stream api seems super thorny
            // getting .Length can block forever, checking .EndOfStream can block forever, who knows what else :/

        // TODO does this do anything?
        proc.Exited.Add (fun _ ->
            printfn "proc exited"
            stdout.Close()
        )

        { cmd = cmd
          args = args
          stdin = stdin
          stdout = stdout
          stderr = stderr
          proc = proc }

let setupConsole () =

    Console.OutputEncoding <- Text.Encoding.UTF8
        // Display emojis

    Console.CancelKeyPress.Add(fun e -> e.Cancel <- true)
        // Don't die on ctrl-c, but passthrough ctrl-c to child processes

let write path input = ()
let echo (reader: StreamReader) = 
    (task {
        let mutable hasLine = true

        while hasLine do
            let! line = reader.ReadLineAsync()
            if line = null then
                hasLine <- false
                printfn "echo --end--"
            else
                printfn "echo [%s]" line
    }).Wait()
    
let proc = Proc.Create
let wait proc =
    proc.proc.WaitForExit()
    proc

// TODO next I think I can just use Streams instead of ProcStream?
// useful as an enum
let host = {|
    stdin = new StreamReader(Console.OpenStandardInput())
|}
