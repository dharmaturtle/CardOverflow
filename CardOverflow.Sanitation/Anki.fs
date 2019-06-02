namespace CardOverflow.Sanitation

open System.IO
open System.Linq
open System.IO.Compression
open Helpers
open System
open CardOverflow.Debug
open CardOverflow.Pure
open CardOverflow.Api

module Anki =
    let unzipToRandom zipFile entry destination =
        let zippedEntry = ZipFile.Open(zipFile, ZipArchiveMode.Read).Entries.First(fun x -> x.Name = entry)
        let destination = destination +/ Random.cryptographicString 64
        destination |> zippedEntry.ExtractToFile
        destination
