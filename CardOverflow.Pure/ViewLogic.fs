module ViewLogic

open HtmlDiff
open System
open CardOverflow.Debug
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions
open NodaTime
open System.Globalization

let toString (timeSpan: Duration) =
    let abs = if timeSpan >= Duration.Zero then timeSpan else -timeSpan
    match abs with
    | abs when abs < Duration.FromMinutes 1L -> if timeSpan >= Duration.Zero then "1 min" else "-1 min"
    | abs when abs < Duration.FromHours 1.   -> sprintf "%.0f min"   timeSpan.TotalMinutes
    | abs when abs < Duration.FromDays 1.    -> sprintf "%.0f h"     timeSpan.TotalHours
    | abs when abs < Duration.FromDays 30.   -> sprintf "%.0f d"     timeSpan.TotalDays
    | abs when abs < Duration.FromDays 365.  -> sprintf "%.1f mo" <| timeSpan.TotalDays / 30.
    | _                                      -> sprintf "%.1f yr" <| timeSpan.TotalDays / 365.

let timestampToPretty (timestamp: Instant) (currentTime: Instant) =
    let delta = currentTime - timestamp
    if delta < Duration.FromDays 31. then
        toString delta + " ago"
    else
        "on " + timestamp.ToString("""MMM d 'xsighx' yy""", CultureInfo.InvariantCulture).Replace("xsighx ", "'")

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
