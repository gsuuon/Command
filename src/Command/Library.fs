module Gsuuon.Command

type ProcResult = {
  out : string
  err : string
  code : int
}

let exec handleOut handleErr (cmd: string) =
  let processStartInfo = new ProcessStartInfo()

  processStartInfo.FileName <- "cmd.exe"
  processStartInfo.Arguments <- sprintf "/C %s" cmd
  processStartInfo.UseShellExecute <- false
  processStartInfo.RedirectStandardOutput <- true
  processStartInfo.RedirectStandardError <- true
  processStartInfo.RedirectStandardInput <- true

  let proc = new Process()
  proc.StartInfo <- processStartInfo
  proc.Start() |> ignore

  let mutable errLines = []
  let mutable outLines = []

  async {
    let stdin = proc.StandardInput

    async {
      use errReader = proc.StandardError

      while not proc.HasExited do
        if not errReader.EndOfStream then
          let err = errReader.ReadLine()
          errLines <- errLines @ [err]
          handleErr err
    } |> Async.Start

    async {
      while not proc.HasExited do
        let line = Console.ReadLine()
        stdin.WriteLine(line)
    } |> Async.Start

    async {
      use reader = proc.StandardOutput

      while not proc.HasExited do
        if not reader.EndOfStream then
          let output = reader.ReadLine()
          outLines <- outLines @ [output]
          handleOut output
    } |> Async.Start
  } |> Async.Start

  Console.CancelKeyPress.Add(fun e ->
    e.Cancel <- true
  )

  proc.WaitForExit() |> ignore

  {
    out = String.concat "\n" outLines
    err = String.concat "\n" errLines
    code = proc.ExitCode
  }

// We're sending everything except output file to stderr so stdout is just the output file name
let show = exec (eprintfn "%s") (eprintfn "%s")
let hide = exec ignore ignore

let sleep ms = Threading.Thread.Sleep 1000


