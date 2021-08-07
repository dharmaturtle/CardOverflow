namespace CardOverflow.Api

open System.IO
open System.Linq
open System.IO.Compression
open Helpers
open System
open CardOverflow.Debug
open CardOverflow.Pure
open CardOverflow.Api
open CardOverflow.Entity
open FsToolkit.ErrorHandling
open CardOverflow.Entity
open System.Threading.Tasks

module SanitizeAnki = // medTODO actually sanitize, ie virus scan
    let ankiExportsDir = Directory.GetCurrentDirectory() +/ "AnkiExports"

    let unzipCollectionToRandom zipFile =
        let entries = ZipFile.Open(zipFile, ZipArchiveMode.Read).Entries
        let collection =
            if entries.Any(fun x -> x.Name = "collection.anki21")
            then entries.First(fun x -> x.Name = "collection.anki21")
            else entries.First(fun x -> x.Name = "collection.anki2")
        let zipName = Path.GetFileNameWithoutExtension(zipFile)
        let destination = ankiExportsDir +/ Guid.NewGuid().ToString() + zipName + ".sqlite.ankiTemp"
        destination |> collection.ExtractToFile
        destination
    
    let ankiDb pathToCollection =
        unzipCollectionToRandom pathToCollection
        |> AnkiDbFactory.Create

    let Import (db: CardOverflowDb) pathToCollection userId =
        pathToCollection
        |> AnkiImporter.loadFiles (fun sha256 -> db.File.FirstOrDefault(fun f -> f.Sha256 = sha256) |> Option.ofObj)
        |> Task.FromResult
        |> TaskResult.bind(
            AnkiImporter.save
                db
                (pathToCollection |> ankiDb |> AnkiImporter.getSimpleAnkiDb)
                userId
            )
        
     // medTODO write a periodic function that will delete old files
