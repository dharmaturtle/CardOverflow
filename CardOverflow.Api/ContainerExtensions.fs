module ContainerExtensions

open CardOverflow.Api
open SimpleInjector
open SimpleInjector.Lifestyles

type Container with
    member container.RegisterNonView =
        container.Options.DefaultScopedLifestyle <- new AsyncScopedLifestyle()
        container.RegisterInstance("Server=localhost;Database=CardOverflow;Trusted_Connection=True;" |> ConnectionString |> CreateCardOverflowDb.create)
        container.RegisterSingleton<IDbService, DbService>()
