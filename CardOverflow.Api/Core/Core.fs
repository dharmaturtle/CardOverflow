namespace CardOverflow.Api

open System

module Map =
    let overValue f =
        Seq.map (fun (KeyValue(_, v)) -> f v)
