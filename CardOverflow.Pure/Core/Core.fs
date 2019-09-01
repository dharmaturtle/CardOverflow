namespace CardOverflow.Pure

open System.Security.Cryptography
open System.Text
open Microsoft.FSharp.Quotations
open System

module Map =
    let overValue f =
        Seq.map (fun (KeyValue(_, v)) -> f v)

module Result =
    let isOk = function
        | Ok _ -> true
        | Error _ -> false

    let getOk = function
        | Ok ok -> ok
        | Error _ -> failwith "Not ok"

    let getError = function
        | Ok _ -> failwith "Not error"
        | Error error -> error

    let consolidate results =
        let errors = results |> Seq.filter (not << isOk)
        if Seq.isEmpty errors
        then results |> Seq.map getOk |> Ok
        else errors |> Seq.map getError |> String.concat "\r\n" |> Error


module Random =
    let cryptographicString length = // https://stackoverflow.com/a/1344255/625919 and https://gist.github.com/diegojancic/9f78750f05550fa6039d2f6092e461e5
        let chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_".ToCharArray()
        let data = Array.zeroCreate length
        use crypto = new RNGCryptoServiceProvider()
        crypto.GetBytes data
        let sb = StringBuilder length
        data |> Seq.iter(fun b -> sb.Append(chars.[int b % chars.Length]) |> ignore)
        sb.ToString()

module Core =
    let nameof (q: Expr<_>) = // https://stackoverflow.com/a/48311816
        match q with
        | Patterns.Let(_, _, DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _))) -> mi.Name
        | Patterns.PropertyGet(_, mi, _) -> mi.Name
        | DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _)) -> mi.Name
        | _ -> failwith "Unexpected format"
    let any<'R> : 'R = failwith "!"
    
    type NullCoalesce = // https://stackoverflow.com/a/21194566
        static member Coalesce(a: 'a option, b: 'a Lazy) = 
            match a with 
            | Some a -> a 
            | _ -> b.Value
        static member Coalesce(a: 'a Nullable, b: 'a Lazy) = 
            if a.HasValue then a.Value
            else b.Value
        static member Coalesce(a: 'a when 'a:null, b: 'a Lazy) = 
            match a with 
            | null -> b.Value 
            | _ -> a
    let inline nullCoalesceHelper< ^t, ^a, ^b, ^c when (^t or ^a) : (static member Coalesce : ^a * ^b -> ^c)> a b = 
            // calling the statically inferred member
            ((^t or ^a) : (static member Coalesce : ^a * ^b -> ^c) (a, b))
    let inline (|??) a b = nullCoalesceHelper<NullCoalesce, _, _, _> a b
