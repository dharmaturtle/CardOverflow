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
open CardOverflow.Sanitation
open CardOverflow.Sanitation.SanitizeDeckRepository

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

[<Fact>]
let ``NotificationRepository.get populates MyDeck"``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = user_3
    let publicDeckId = deck_3
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    let followerId = user_1
    let followerDefaultDeckId = deck_1
    let newDeckId = deck_ 4
    let newDeckName = Guid.NewGuid().ToString()
    do! SanitizeDeckRepository.follow c.Db followerId publicDeckId (NewDeck (newDeckId, newDeckName)) true None
    do! FacetRepositoryTests.addBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let assertNotificationThenDelete expected = task {
        let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
        let n = ns.Results |> Assert.Single
        n.Created |> Assert.dateTimeEqual 60. DateTime.UtcNow
        n |> Assert.equal
            {   expected with
                    Id = n.Id
                    Created = n.Created // cheating, but whatever
            }
        do! NotificationRepository.remove c.Db followerId n.Id
        Assert.Empty c.Db.Notification
        Assert.Empty c.Db.ReceivedNotification
    }
    let ids =
        {   StackId = stack_1
            BranchId = branch_1
            LeafId = leaf_1 }

    do! assertNotificationThenDelete
            {   Id = notification_1
                SenderId = authorId
                SenderDisplayName = "RoboTurtle"
                Created = DateTime.MinValue
                Message = DeckAddedStack { TheirDeck = { Id = publicDeckId; Name = "Default Deck" }
                                           MyDeck = Some { Id = newDeckId; Name = newDeckName }
                                           Collected = None
                                           New = ids } }
    
    // works with DeckUpdatedStack, uncollected
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId ids.BranchId) ((stack_1, branch_1, leaf_2, [card_1]) |> UpsertIds.fromTuple)
    let! _ = SanitizeStackRepository.Update c.Db authorId [] stackCommand
    let expectedDeckUpdatedStackNotification nid newLeafId collected =
            {   Id = nid
                SenderId = authorId
                SenderDisplayName = "RoboTurtle"
                Created = DateTime.MinValue
                Message = DeckUpdatedStack { TheirDeck = { Id = publicDeckId; Name = "Default Deck" }
                                             MyDeck = Some { Id = newDeckId; Name = newDeckName }
                                             Collected = collected
                                             New = { ids with LeafId = newLeafId } } }

    do! expectedDeckUpdatedStackNotification notification_2 leaf_2 None |> assertNotificationThenDelete

    // works with DeckUpdatedStack, collected
    let collectedLeaf = ids.LeafId
    do! StackRepository.CollectCard c.Db followerId collectedLeaf [ card_2 ]
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId ids.BranchId) ((stack_1, branch_1, leaf_3, [card_1]) |> UpsertIds.fromTuple)
    let! _ = SanitizeStackRepository.Update c.Db authorId [] stackCommand
    let collected =
        {   StackId = ids.StackId
            BranchId = ids.BranchId
            LeafId = collectedLeaf
            CardIds = [ card_2 ] } |> Some
    
    do! expectedDeckUpdatedStackNotification notification_3 leaf_3 collected
        |> assertNotificationThenDelete
    
    // works with DeckDeletedStack
    do! StackRepository.uncollectStack c.Db authorId ids.StackId

    do! assertNotificationThenDelete
            {   Id = notification_ 4
                SenderId = authorId
                SenderDisplayName = "RoboTurtle"
                Created = DateTime.MinValue
                Message = DeckDeletedStack { TheirDeck = { Id = publicDeckId; Name = "Default Deck" }
                                             MyDeck = Some { Id = newDeckId; Name = newDeckName }
                                             Collected = collected
                                             Deleted = { ids with LeafId = leaf_3 } } }
    } |> TaskResult.getOk)