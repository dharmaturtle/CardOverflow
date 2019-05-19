module Dump

open CardOverflow.Debug

let dump x =
    x.Dump()

let dump2 label x =
    x.Dump label

let dumpI x =
    x.Dump() |> ignore

let dump2I label x =
    x.Dump label |> ignore

let c x =
    x.CDump()

let f x =
    x.FDump()

let seq s =
    s |> Seq.toList |> List.map (fun x -> x.Dump())

let seq2 label s =
    s |> Seq.toList |> List.map (fun x -> x.Dump label)
