open System

[<EntryPoint>]
let main argv =
    printfn "Starting!"
    AnkiImportFileTests.``Manual Anki import``().GetAwaiter().GetResult() // lowTODO fix
    printfn "Done!"
    0 // return an integer exit code
