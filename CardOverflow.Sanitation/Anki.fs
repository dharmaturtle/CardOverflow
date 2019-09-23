namespace CardOverflow.Sanitation

open System.IO
open System.Linq
open System.IO.Compression
open Helpers
open System
open CardOverflow.Debug
open CardOverflow.Pure
open CardOverflow.Api

module SanitizeAnki = // medTODO actually sanitize, ie virus scan
    let ankiExportsDir = Directory.GetCurrentDirectory() +/ "AnkiExports"

    let unzipCollectionToRandom zipFile =
        let entries = ZipFile.Open(zipFile, ZipArchiveMode.Read).Entries
        let collection =
            if entries.Any(fun x -> x.Name = "collection.anki21")
            then entries.First(fun x -> x.Name = "collection.anki21")
            else entries.First(fun x -> x.Name = "collection.anki2")
        let destination = ankiExportsDir +/ Random.cryptographicString 64 + ".ankiTemp"
        destination |> collection.ExtractToFile
        destination
    
    let ankiDb pathToCollection =
        unzipCollectionToRandom pathToCollection
        |> AnkiDbFactory.Create
    
     // medTODO write a periodic function that will delete old files