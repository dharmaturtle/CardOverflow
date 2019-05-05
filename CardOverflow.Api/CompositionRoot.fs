namespace CardOverflow.Api

type CompositionRoot ={
    connectionString: ConnectionString
}
module CompositionRoot =
    let local = {
        connectionString = "Server=localhost;Database=CardOverflow;Trusted_Connection=True;" |> ConnectionString
    }

