module Helpers

open System.IO

// http://www.fssnip.net/1g/title/Working-with-paths
let (+/) path1 path2 = Path.Combine(path1, path2)

// https://troykershaw.com/null-coalescing-operator-in-fsharp-but-for-options/
let inline (|?) (a: 'a option) b = if a.IsSome then a.Value else b
