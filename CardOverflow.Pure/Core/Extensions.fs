namespace CardOverflow.Pure

open System.Collections.Generic
open System.Linq
open System.Runtime.CompilerServices
open System

[<Extension>]
module Extensions =
    type IEnumerable<'TDest> with
        member target.Merge<'TDest, 'TSource>
            (source: IEnumerable<'TSource>)
            predicate
            create
            delete
            add
            update =
            let updates =
                [ for d in target do
                  for s in source do
                  yield (d, s)
                ] |> Seq.filter predicate
            let adds = 
                source |> Seq.filter(fun m -> not <| target.Any(fun s -> predicate(s, m))) |> Seq.toList
            let deletes = 
                target |> Seq.filter(fun s -> not <| source.Any(fun m -> predicate(s, m))) |> Seq.toList

            for d, s in updates do update d s
            for d in deletes do delete d
            for item in adds do
                let o = create item
                update o item
                add o

    type IEnumerable<'T> with
        member this.ToFList () = 
            this |> List.ofSeq

    [<Extension>]
    let Apply(input, (func: Func<'TInput, 'TOutput>)) = 
        func.Invoke input

    [<Extension>]
    let Do(input, (action: Action<'TInput>)) = 
        action.Invoke input
