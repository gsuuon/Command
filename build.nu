ls src
| where type == dir
| get name
| each {|d|
	cd $d
	dotnet pack
}
