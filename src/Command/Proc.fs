module Gsuuon.Proc

open System
open System.IO
open System.Diagnostics
open System.Collections.ObjectModel

// TODO I need a Stream class that keeps track of if it's open or closed
// stream class that allows multiple views on it (separate read positions)

[<AutoOpen>]
module ThreadPrint =
    // ripped from http://www.fssnip.net/7Vy/title/Supersimple-thread-safe-colored-console-output
    
    let lockObj = obj ()
    
    let private clog' printer color s =
        lock lockObj (fun _ ->
            Console.ForegroundColor <- color
            printer s
            Console.ResetColor()
        )

    let clog = clog' (printf "%s")
    let clogn = clog' (printfn "%s")

type PipeName =
    | Stdout
    | Stdin
    | Stderr
    | Combine of PipeName list

type Proc =
    { cmd: string
      args: string seq
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


/// Prepare the console. Sets UTF-8 Encoding and handles Ctrl-C.
/// TODO do I need this?
let setupConsole () =
    // Display emojis
    Console.OutputEncoding <- Text.Encoding.UTF8

    // Don't die on ctrl-c, but passthrough ctrl-c to child processes
    Console.CancelKeyPress.Add(fun e -> e.Cancel <- true)

/// Handle each line of an input
let consume handleLine (input: StreamReader) =
    task {
        while true do
            match! input.ReadLineAsync() with
            | null -> do! Threading.Tasks.Task.Delay 16
            | line -> handleLine line
    }
    |> ignore

/// Read the stream and ignore it. Some processes may block until their stdout/stderr buffer is read.
let sink (input: StreamReader) = consume ignore input

/// Handle each line of an input stream and duplicate it for an output stream
/// NOTE This accumulates in memory forever
let tap handleLine (input: StreamReader) =
    let mem = new MemoryStream()
    let writer = new StreamWriter(mem)
    let reader = new StreamReader(mem)
    writer.AutoFlush <- true

    task {
        while true do
            match! input.ReadLineAsync() with
            | null -> do! Threading.Tasks.Task.Delay 16
            | line ->
                handleLine line

                let originalPosition = mem.Position

                // write to end of memorystream
                mem.Seek(0, SeekOrigin.End) |> ignore
                writer.WriteLine(line)

                // move position back to where it was for reader
                mem.Seek(originalPosition, SeekOrigin.Begin) |> ignore
    }
    |> ignore

    reader

/// Read the stream to its current end. Returns immediately if the stream is already at the
/// end without blocking.
let read (input: StreamReader) = input.ReadToEnd()

/// Wait for the process to finish and collect the standard in and error pipes
let complete (p: Proc) =
    let pOut = tap ignore p.stdout
    let pErr = tap ignore p.stderr

    p.proc.WaitForExit()

    { p with
        stdout = pOut
        stderr = pErr
    }
    
/// Create a pipe input (StreamReader) from some text
let from (text: string) =
    let mem = new MemoryStream() // no ctor means expandable

    mem.Write(Text.Encoding.UTF8.GetBytes(text))
    mem.Seek(0, SeekOrigin.Begin) |> ignore

    new StreamReader(mem)

/// Create a new Proc
let proc (cmd: string) (args: string seq) (input: StreamReader) =
    let processStartInfo = new ProcessStartInfo()

    let cmdText = "cmd.exe"

    processStartInfo.FileName <- cmdText 
    processStartInfo.ArgumentList.Add "/c"
    processStartInfo.ArgumentList.Add cmd
    args |> Seq.iter (processStartInfo.ArgumentList.Add)


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

    task {
        while true do
            match! input.ReadLineAsync() with
            | null ->
                // readline can return immediately if we're at end of stream but stream is not closed
                // how do I figure out if the stream has closed?
                // also maybe the stream never closes?
                do! Threading.Tasks.Task.Delay 100
            | line ->
                clogn ConsoleColor.Red $"<{cmd}> [{line}]"
                do! stdin.WriteLineAsync(line)
                do! stdin.FlushAsync()
    }
    |> ignore

    // I tried for a long time to make this actually stream, without closing, but it just doesn't work
    // the dotnet stream api seems super thorny
    // getting .Length can block forever, checking .EndOfStream can block forever, who knows what else :/

    { cmd = cmd
      args = args
      stdin = stdin
      stdout = stdout
      stderr = stderr
      proc = p }

/// Wait for a proc to exit
let wait proc =
    proc.proc.WaitForExit()
    proc

let host =
    {| stdin = new StreamReader(Console.OpenStandardInput())
       stdout = new StreamWriter(Console.OpenStandardOutput())
       stderr = new StreamWriter(Console.OpenStandardError()) |}
