namespace CardOverflow.Pure

open System.Security.Cryptography
open System.Text

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
