open System

[<EntryPoint>]
let main argv =
    printfn "Starting!"
    AnkiImportFileTests.``Manual Anki import``()
    printfn "Done!"
    0 // return an integer exit code
