namespace CardOverflow.Pure

open CardOverflow.Debug
open System
open System.Linq
open System.Security.Cryptography
open System.Text
open Microsoft.FSharp.Quotations
open System.Threading.Tasks

module TaskX =
    let map continuation (t: Task<'A>) =
        t.ContinueWith(fun (antecedent: Task<'A>) -> // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/chaining-tasks-by-using-continuation-tasks
            if  antecedent.Status <> TaskStatus.Canceled &&
                antecedent.Status <> TaskStatus.Faulted then
                continuation antecedent.Result
            else
                raise antecedent.Exception // must I?
        )

[<CustomEquality; NoComparison>]
type StructurallyNull<'T> = // https://stackoverflow.com/a/20946801
    { v: 'T } 
    override __.Equals(yobj) =
        match yobj with
        | :? StructurallyNull<'T> -> true
        | _ -> false
    override __.GetHashCode() = 0

module Map =
    let overValue f =
        Seq.map (fun (KeyValue(_, v)) -> f v)

module Result =
    let isOk = function
        | Ok _ -> true
        | Error _ -> false

    let getOk = function
        | Ok ok -> ok
        | Error x -> failwithf "Error: %A" x

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
        data |> Array.iter(fun b -> sb.Append(chars.[int b % chars.Length]) |> ignore)
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
        static member Coalesce(a: 'a, b: 'a Lazy) = 
            match obj.ReferenceEquals(a, null) with
            | true -> b.Value
            | false -> a
    let inline nullCoalesceHelper< ^t, ^a, ^b, ^c when (^t or ^a) : (static member Coalesce : ^a * ^b -> ^c)> a b = 
            // calling the statically inferred member
            ((^t or ^a) : (static member Coalesce : ^a * ^b -> ^c) (a, b))
    let inline (|??) a b = nullCoalesceHelper<NullCoalesce, _, _, _> a b
    
    let rec combination elementCount targetList = // https://stackoverflow.com/a/1231711
        match elementCount, targetList with
        | 0, _ -> [[]]
        | _, [] -> []
        | k, (x::xs) -> List.map ((@) [x]) (combination (k-1) xs) @ combination k xs

    let toResizeArray (xs: 'a seq) =
        xs.ToList()

    let toOption (a: 'a) = // stackoverflow.com/a/26008852
        match obj.ReferenceEquals(a, null) with
        | true -> None
        | false -> Some a

    type WriteAnyOverloads = WriteAnyOverloads with // https://www.youtube.com/watch?v=j7wOpye8ygM see comments
        static member inline ($) (x: float, _) = System.Math.Round(x, MidpointRounding.AwayFromZero) |> int
        static member inline ($) (x: decimal, _) = System.Math.Round(x, MidpointRounding.AwayFromZero) |> int
    let inline round x = x $ WriteAnyOverloads

module List =
    let ifEmptyThen x xs =
        match xs with
        | [] -> [x]
        | _  -> xs
