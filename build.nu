rm packages/*

[ Command
  Console
  Command.Utility ]
  | each {|d|
      cd src
      cd $d
      dotnet pack
  }
