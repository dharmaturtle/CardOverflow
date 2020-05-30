namespace CardOverflow.Pure

open CardOverflow.Debug
open System.Linq
open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions
open CardOverflow.Debug
open FSharp.Text.RegexProvider

module FullTextSearch =
    type PostgresColonRegex = FSharp.Text.RegexProvider.Regex< """(?<WildcardWord>[A-Za-z]+\*)(?:\s|$)""" >
    let postgresColonRegex = RegexOptions.Compiled &&& RegexOptions.IgnoreCase |> PostgresColonRegex
    let parse (input: string) =
        let wildcards = postgresColonRegex.TypedMatches(input).Select(fun x -> x.WildcardWord.Value)
        let plain = (input, wildcards) ||> Seq.fold (fun prior x -> prior.Replace(x, ""))
        plain,
        wildcards |> String.concat " " |> fun x -> x.Replace("*", ":*")
