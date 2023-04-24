open Gsuuon.Proc

// host.stdin |> echo
// proc "echo" "boop" host.stdin <!> Stdout |> echo
// proc "findstr" "h" host.stdin <!> Stdout  |> echo
// proc "sed" "-e s/hi/hello/" host.stdin <!> Stdout |> echo

proc "echo" "feafs" host.stdin <!> Stdout |> proc "sed" "-e s/hi/hello/" <!> Stdout |> echo
