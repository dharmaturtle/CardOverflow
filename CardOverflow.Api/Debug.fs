module D

open CardOverflow.Debug

let d x =
    x.D()

let dA (x: obj) =
    match x with
    | :? seq<obj> as x -> x |> Seq.toList :> obj
    | x -> x
    |> printfn "%A"

let d2 label x =
    x.D label

let dI x =
    x.D() |> ignore

let df f x =
    f x |> dI
    x

let d2I label x =
    x.D label |> ignore

let c x =
    x.CDump()

let f x =
    x.FDump()

let seq s =
    s |> Seq.toList |> List.map (fun x -> x.D())

let seq2 label s =
    s |> Seq.toList |> List.map (fun x -> x.D label)
