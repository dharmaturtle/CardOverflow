namespace CardOverflow.Pure

open System.Collections.Generic
open System.Linq
open System.Runtime.CompilerServices
open System
open System.Runtime.InteropServices

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
    let ToFList input =
        input |> List.ofSeq

    [<Extension>]
    let None (input: _ seq) =
        input.Any()

    [<Extension>]
    let Apply(input, (func: Func<'TInput, 'TOutput>)) = 
        func.Invoke input

    [<Extension>]
    let Pipe(input, (func: Func<'TInput, 'TOutput>)) = 
        func.Invoke input

    [<Extension>]
    let Do(input, (action: Action<'TInput>)) = 
        action.Invoke input

    [<Extension>]
    let TryOk(this, [<Out>] out: _ byref) =
        match this with
        | Ok x -> out <- x; true
        | _ -> false
    
    [<Extension>]
    let TryError(this, [<Out>] out: _ byref) =
        match this with
        | Error x -> out <- x; true
        | _ -> false
    
    [<Extension>]
    let Cata(this, (fok: Func<'ok, 'unified>), (ferror: Func<'error, 'unified>)) =
        match this with
        | Ok ok -> fok.Invoke ok
        | Error error -> ferror.Invoke error
    
    [<Extension>]
    let Match(this, (fok: 'ok Action), (ferror: 'error Action)) =
        match this with
        | Ok ok -> fok.Invoke ok
        | Error error -> ferror.Invoke error
