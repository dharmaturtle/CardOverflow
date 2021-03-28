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
open NodaTime

type NotificationTests () =
    let c = new TestContainer(memberName = nameof NotificationTests)

    //[<Generators>] [<Fact(Skip=PgSkip.reason)>]
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

[<Fact(Skip=PgSkip.reason)>]
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
    do! FacetRepositoryTests.addBasicConcept c.Db authorId [] (concept_1, example_1, revision_1, [card_1])
    let assertNotificationThenDelete expected = task {
        let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
        let n = ns.Results |> Assert.Single
        n.Created |> Assert.dateTimeEqual 60. DateTimeX.UtcNow
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
        {   ConceptId = concept_1
            ExampleId = example_1
            RevisionId = revision_1 }

    do! assertNotificationThenDelete
            {   Id = notification_1
                SenderId = authorId
                SenderDisplayName = "RoboTurtle"
                Created = Instant.MinValue
                Message = DeckAddedConcept { TheirDeck = { Id = publicDeckId; Name = "Default Deck" }
                                             MyDeck = Some { Id = newDeckId; Name = newDeckName }
                                             Collected = None
                                             New = ids
                                             NewCardCount = 1 } }
    
    // works with DeckUpdatedConcept, uncollected
    let! conceptCommand = SanitizeConceptRepository.getUpsert c.Db authorId (VUpdate_ExampleId ids.ExampleId) ((concept_1, example_1, revision_2, [card_1]) |> UpsertIds.fromTuple)
    let! _ = SanitizeConceptRepository.Update c.Db authorId [] conceptCommand
    let expectedDeckUpdatedConceptNotification nid newRevisionId collected =
            {   Id = nid
                SenderId = authorId
                SenderDisplayName = "RoboTurtle"
                Created = Instant.MinValue
                Message = DeckUpdatedConcept { TheirDeck = { Id = publicDeckId; Name = "Default Deck" }
                                               MyDeck = Some { Id = newDeckId; Name = newDeckName }
                                               Collected = collected
                                               New = { ids with RevisionId = newRevisionId }
                                               NewCardCount = 1 } }

    do! expectedDeckUpdatedConceptNotification notification_2 revision_2 None |> assertNotificationThenDelete

    // works with DeckUpdatedConcept, collected
    let collectedRevision = ids.RevisionId
    do! ConceptRepository.CollectCard c.Db followerId collectedRevision [ card_2 ]
    let! conceptCommand = SanitizeConceptRepository.getUpsert c.Db authorId (VUpdate_ExampleId ids.ExampleId) ((concept_1, example_1, revision_3, [card_1]) |> UpsertIds.fromTuple)
    let! _ = SanitizeConceptRepository.Update c.Db authorId [] conceptCommand
    let collected =
        {   ConceptId = ids.ConceptId
            ExampleId = ids.ExampleId
            RevisionId = collectedRevision
            CardIds = [ card_2 ] } |> Some
    
    do! expectedDeckUpdatedConceptNotification notification_3 revision_3 collected
        |> assertNotificationThenDelete
    
    // works with DeckDeletedConcept
    do! ConceptRepository.uncollectConcept c.Db authorId ids.ConceptId

    do! assertNotificationThenDelete
            {   Id = notification_ 4
                SenderId = authorId
                SenderDisplayName = "RoboTurtle"
                Created = Instant.MinValue
                Message = DeckDeletedConcept { TheirDeck = { Id = publicDeckId; Name = "Default Deck" }
                                               MyDeck = Some { Id = newDeckId; Name = newDeckName }
                                               Collected = collected
                                               Deleted = { ids with RevisionId = revision_3 }
                                               DeletedCardCount = 1 } }
    } |> TaskResult.getOk)