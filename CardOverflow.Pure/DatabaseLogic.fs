namespace CardOverflow.Pure

open CardOverflow.Debug
open System.Linq
open CardOverflow.Pure.Core
open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions
open CardOverflow.Debug
open FSharp.Text.RegexProvider

type PostgresColonRegex = Regex< """(?<WildcardWord>[A-Za-z]+\*)(?:\s|$)""" >
module FullTextSearch =
    let private regex = PostgresColonRegex(RegexOptions.Compiled)
    let parse (input: string) =
        let wildcards = regex.TypedMatches(input).Select(fun x -> x.WildcardWord.Value)
        let plain = (input, wildcards) ||> Seq.fold (fun prior x -> prior.Replace(x, ""))
        plain,
        wildcards |> String.concat " " |> fun x -> x.Replace("*", ":*")
