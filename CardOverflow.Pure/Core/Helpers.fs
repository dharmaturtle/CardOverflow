module Helpers

open System.IO

// http://www.fssnip.net/1g/title/Working-with-paths
let (+/) path1 path2 = Path.Combine(path1, path2)