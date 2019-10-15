module ViewLogic

open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions

let toString =
    function
    | x when x < TimeSpan.FromMinutes 1. -> "1 min"
    | x when x < TimeSpan.FromHours 1.   -> sprintf "%.0f min"   x.TotalMinutes
    | x when x < TimeSpan.FromDays 1.    -> sprintf "%.0f h"     x.TotalHours
    | x when x < TimeSpan.FromDays 30.   -> sprintf "%.0f d"     x.TotalDays
    | x when x < TimeSpan.FromDays 365.  -> sprintf "%.1f mo" <| x.TotalDays / 30.
    | x                                  -> sprintf "%.1f yr" <| x.TotalDays / 365.
