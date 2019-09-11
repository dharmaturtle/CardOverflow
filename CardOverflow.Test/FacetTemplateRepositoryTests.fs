module CardTemplateRepositoryTests

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Test
open System
open System.Linq
open Xunit
open CardOverflow.Pure
open System.Collections.Generic
open FSharp.Control.Tasks

[<Fact>]
let ``CardTemplateRepository.GetFromInstance isn't empty``() =
    let templateId = 1
    task {
        use c = new TestContainer()
        let! cardTemplate = CardTemplateRepository.GetFromInstance c.Db templateId
        Assert.NotEmpty(cardTemplate.Instances)
        Assert.NotEmpty(cardTemplate.Instances.First().Fields)
    }
