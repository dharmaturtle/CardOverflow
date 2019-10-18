module ViewLogic

open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions

let toString (timeSpan: TimeSpan) =
    match timeSpan.Duration() with
    | duration when duration < TimeSpan.FromMinutes 1. -> if timeSpan >= TimeSpan.Zero then "1 min" else "-1 min"
    | duration when duration < TimeSpan.FromHours 1.   -> sprintf "%.0f min"   timeSpan.TotalMinutes
    | duration when duration < TimeSpan.FromDays 1.    -> sprintf "%.0f h"     timeSpan.TotalHours
    | duration when duration < TimeSpan.FromDays 30.   -> sprintf "%.0f d"     timeSpan.TotalDays
    | duration when duration < TimeSpan.FromDays 365.  -> sprintf "%.1f mo" <| timeSpan.TotalDays / 30.
    | _                                                -> sprintf "%.1f yr" <| timeSpan.TotalDays / 365.
