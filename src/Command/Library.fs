module Gsuuon.Command

open System
open System.IO
open System.Threading
open System.Diagnostics

// TODO I need a Stream class that keeps track of if it's open or closed
// stream class that allows multiple views on it (separate read positions)

module private Task = 
    let Wait (x: Tasks.Task<_>) =
        x.Wait()
        x.Result

module private Cells =
    /// cells.
    let mutable procCount = 0
    /// cells. interlinked.
    let incr () = Threading.Interlocked.Increment(&procCount) |> ignore
    /// interlinked.
    let decr () = Threading.Interlocked.Decrement(&procCount) |> ignore
    /// within cells interlinked.
    let hold () =
        task {
            while procCount > 0 do
                do! Threading.Tasks.Task.Delay 100
        } |> Task.Wait

    AppDomain.CurrentDomain.ProcessExit.Add(fun _ -> hold())

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


// TODO dry with tap
/// Transforms a stream (you probably want to write a newline at some point)
/// transformLine is a fn that takes a writer and a line, and calls writer with
/// the transformed line. If you need to do setup, you can -- e.g.:
/// <example>
/// inStream |> transform (fun writer ->
///     let write = expensiveSetup(writer)
///     fun line -> write line)
/// </example>
let transform transformLine (input: StreamReader) =
    let mem = new MemoryStream()
    let writer = new StreamWriter(mem)
    let reader = new StreamReader(mem)
    writer.AutoFlush <- true

    let write (line: string) =
        let originalPosition = mem.Position
        mem.Seek(0, SeekOrigin.End) |> ignore
        writer.Write(line)
        mem.Seek(originalPosition, SeekOrigin.Begin) |> ignore

    let transformedWrite = transformLine write

    task {
        while true do
            match! input.ReadLineAsync() with
            | null -> do! Threading.Tasks.Task.Delay 16
            | line -> transformedWrite line
    }
    |> ignore

    reader
    
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


/// Read the stream to its current end. Does not read until the stream closes.
/// That means it returns immediately if it's already at the end.
/// It blocks until it reaches the end of the stream - so if there's nothing to
/// read at all it will block forever.
let readBlock (input: StreamReader) = input.ReadToEnd()

/// Read the stream to its current end. Returns immediately if the 
/// stream is already at the end, or times out in 100ms if no line is read.
/// Useful for the initial input stream to an application.
let readNow (input: StreamReader) =
    let timeout = 100

    let mutable hasRead = false

    task {
        let lines = task {
            let! line = input.ReadLineAsync()
            hasRead <- true

            let! lines = input.ReadToEndAsync()

            return line + lines
        }

        do! Tasks.Task.Delay timeout

        if hasRead then return! lines
        else
            input.Close()
            return ""
    } |> Task.Wait

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

    processStartInfo.FileName <- cmd

    args |> Seq.iter (processStartInfo.ArgumentList.Add)

    processStartInfo.UseShellExecute <- false
    processStartInfo.RedirectStandardOutput <- true
    processStartInfo.RedirectStandardError <- true
    processStartInfo.RedirectStandardInput <- true

    let p = new Process()
    p.StartInfo <- processStartInfo
    p.EnableRaisingEvents <- true
    p.Exited.Add(fun _ ->
        if p.ExitCode <> 0 then
            eprintfn "<%s> exited %i" cmd p.ExitCode

        Cells.decr()
    )
    p.Start() |> ignore

    Cells.incr()

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
                do! stdin.WriteLineAsync(line)
                do! stdin.FlushAsync()
    }
    |> ignore

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
