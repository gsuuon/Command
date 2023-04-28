[ Command
	Console
	Command.Utility ]
	| each {|d|
			cd $"src/($d)"
			dotnet pack
	}
