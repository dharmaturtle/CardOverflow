module ViewLogic

open HtmlDiff
open System
open CardOverflow.Debug
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

let timestampToPretty (timestamp: DateTime) (currentTime: DateTime) =
    let delta = currentTime - timestamp
    if delta < TimeSpan.FromDays 31. then
        toString delta + " ago"
    else
        "at " + timestamp.ToString("""MMM d 'xsighx' yy""").Replace("xsighx ", "'")

let insertDiffColors (html: string) =
    let style =
        """<style>
        ins {
        	    background-color: #cfc;
        	    text-decoration: none;
        }
        
        del {
        	    color: #999;
        	    background-color:#FEC8C8;
        }
        </style>"""
    let head = "<head>"
    if html.Contains head then
        html.Replace(head, head + style)
    else
        style + html

let diff a b =
    HtmlDiff(a, b)
        //.IgnoreWhitespaceDifferences // lowTODO add an option for this
        .Build()
    |> insertDiffColors
