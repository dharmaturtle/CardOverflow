module D

open System
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

let d2f label f x =
    f x |> d2 label |> ignore
    x

let d2I label x =
    x.D label |> ignore

let seq s =
    s |> Seq.toList |> List.map (fun x -> x.D())

let seq2 label s =
    s |> Seq.toList |> List.map (fun x -> x.D label)

let f x =
    match box x with
    | :? seq<_> as x -> List.ofSeq x :> Object
    | x -> x
    |> printfn "%A"
    x

let f2 label x =
    match box x with
    | :? seq<_> as x -> List.ofSeq x :> Object
    | x -> x
    |> printfn "%s: %A" label
    x
