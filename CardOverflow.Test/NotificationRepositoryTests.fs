module NotificationRepositoryTests

open CardOverflow.Pure
open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Api
open ContainerExtensions
open LoadersAndCopiers
open Helpers
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Test
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open System.Linq
open Xunit
open System
open SimpleInjector
open SimpleInjector.Lifestyles
open System.Diagnostics
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation
open System.Collections
open System.Security.Cryptography
open FsToolkit.ErrorHandling
open Thoth.Json.Net
open FsCheck.Xunit

type NotificationTests () =
    let c = new TestContainer(memberName = nameof NotificationTests)

    [<Generators>]
    let ``Can insert and retrieve notifications`` (notification: NotificationEntity): unit =
        (task {
            use db = c.Db
            
            notification |> db.Notification.AddI
            do! db.SaveChangesAsyncI()
            let actual = c.Db.Notification.Single()

            Assert.equal
                <| notification.InC()
                <| actual.InC()
            
            // cleanup
            actual |> db.Remove |> ignore
            do! db.SaveChangesAsyncI()
            Assert.Empty c.Db.Notification
        }).GetAwaiter().GetResult()
    
    interface IDisposable with
        member _.Dispose() = (c :> IDisposable).Dispose()
