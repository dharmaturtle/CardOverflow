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
    let unzipCollectionToRandom zipFile destination =
        let entries = ZipFile.Open(zipFile, ZipArchiveMode.Read).Entries
        let collection =
            if entries.Any(fun x -> x.Name = "collection.anki21")
            then entries.First(fun x -> x.Name = "collection.anki21")
            else entries.First(fun x -> x.Name = "collection.anki2")
        let destination = destination +/ Random.cryptographicString 64
        destination |> collection.ExtractToFile
        destination
