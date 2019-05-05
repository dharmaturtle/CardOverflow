namespace CardOverflow.Api

type CompositionRoot ={
    dbService: IDbService
}
module CompositionRoot =
    let local = {
        dbService =
            "Server=localhost;Database=CardOverflow;Trusted_Connection=True;"
            |> ConnectionString
            |> CreateCardOverflowDb.create
            |> DbService
    }

