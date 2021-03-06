module MappingTools

open System
open System.Linq
open HtmlAgilityPack
open System.Globalization
open System.Web
open System.Text.RegularExpressions
open CardOverflow.Pure
open NodaTime

let delimiter = ' '

let stringOfIntsToIntList (string: string) =
    string.Split [| delimiter |] |> Seq.filter ((<>) "") |> Seq.map int |> Seq.toList

let intsListToStringOfInts (ints: int list) =
    ints |> List.map string |> fun x -> String.Join(delimiter, x)

let stringOfMinutesToTimeSpanList (string: string) =
    string.Split [| delimiter |] |> Seq.map (Double.Parse >> Duration.FromMinutes) |> Seq.toList

let timeSpanListToStringOfMinutes (timeSpans: Duration list) =
    timeSpans |> List.map (fun x -> x.TotalMinutes.ToString()) |> fun x -> String.Join(delimiter, x)

let stringIntToBool =
    function
    | "0" -> false
    | _ -> true

let boolToString =
    function
    | false -> "0"
    | true -> "1"

// https://en.wikipedia.org/wiki/C0_and_C1_control_codes

let split separator (string: string) =
    if String.IsNullOrEmpty string then
        []
    else
        string.Split [| separator |] |> Array.toList

let splitByFileSeparator =
    split '\x1c'

let splitByGroupSeparator =
    split '\x1d'

let splitByRecordSeparator =
    split '\x1e'

let splitByUnitSeparator =
    split '\x1f'

let join separator (strings: string seq) =
    String.Join(string separator, strings)

let joinByFileSeparator x =
    join '\x1c' x

let joinByGroupSeparator x =
    join '\x1d' x

let joinByRecordSeparator x =
    join '\x1e' x

let joinByUnitSeparator x =
    join '\x1f' x

let cutOffInt16 x =
    if x > float Int16.MaxValue
    then Int16.MaxValue
    else int16 x

let round (dt: Instant) (d: Duration) = // https://stackoverflow.com/a/20046261/
    let delta = float (dt.ToUnixTimeTicks()) % d.TotalTicks
    let roundUp = delta > d.TotalTicks / 2.
    let offset = (if roundUp then d.TotalTicks else 0.) |> Convert.ToInt64
    Instant.FromUnixTimeTicks(dt.ToUnixTimeTicks() + offset - (Convert.ToInt64 delta))

let standardizeWhitespace x = // lowTODO use StringBuilder like https://stackoverflow.com/a/58849324
    Regex.Replace(x, @"\s+", " ", Regex.compiled).Trim()

let stripHtmlTags html =
    let doc = HtmlDocument()
    doc.LoadHtml html
    doc.DocumentNode.InnerText
    |> HttpUtility.HtmlDecode
    |> standardizeWhitespace

let stripHtmlTagsForDisplay html =
    let doc = HtmlDocument()
    doc.LoadHtml html
    for imgNode in doc.DocumentNode.Descendants("img").ToList() do
        let imgText = HtmlNode.CreateNode " [ Image ] "
        imgNode.ParentNode.ReplaceChild(imgText, imgNode) |> ignore
    let r =
        doc.DocumentNode.InnerText
        |> HttpUtility.HtmlDecode
        |> standardizeWhitespace
    if String.IsNullOrWhiteSpace r then
        "[ Empty ]"
    else r

let private textInfo = CultureInfo("en-US", false).TextInfo;
let toTitleCase = textInfo.ToTitleCase
