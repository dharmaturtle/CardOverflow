namespace CardOverflow.Api

open CardOverflow.Pure
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open System
open LoadersAndCopiers
open CardOverflow.Pure
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open System.Linq
open Helpers
open FSharp.Control.Tasks
open System.Collections.Generic
open X.PagedList
open System.Threading.Tasks
open Microsoft.FSharp.Core
open NeoSmart.Utils
open System.IO
open System
open System.Runtime.ExceptionServices
open System.Runtime.CompilerServices
open NUlid
open NodaTime
open Dapper
open Npgsql
open NodaTime.Text

module FeedbackRepository =
    let addAndSaveAsync (db: CardOverflowDb) userId title description priority =
        FeedbackEntity(
            Title = title,
            Description = description,
            UserId = userId,
            Priority = priority
        ) |> db.Feedback.AddI
        db.SaveChangesAsyncI()

module HistoryRepository =
    let getHeatmap (conn: NpgsqlConnection) userId = task {
        let oneYearishAgo = DateTimeX.UtcNow - Duration.FromDays (53. * 7. - 1.) // always show full weeks of slightly more than a year; -1 is from allDateCounts being inclusive
        let query = """
            SELECT
            	date_trunc('day', h.created AT TIME ZONE 'America/Chicago') AS date, -- highTODO support other timezones
            	COUNT(*)
            FROM history AS h
            WHERE
            	h.created >= @yearishago
            	AND h.user_id = @userid
            GROUP BY date
        """
        let! dateCounts = conn.QueryAsync<DateCount>(query, {| yearishago = oneYearishAgo; userid = userId |})
        let zone = DateTimeZoneProviders.Tzdb.["America/Chicago"] // highTODO support other timezones
        return Heatmap.get
            (oneYearishAgo.InZone(zone).Date)
            (DateTimeX.UtcNow.InZone(zone).Date)
            (dateCounts |> List.ofSeq) }

module ConceptRepository =
    let private searchExplore userId (pageNumber: int) (filteredRevisions: RevisionEntity IOrderedQueryable) =
        task {
            let! r =
                filteredRevisions.Select(fun x ->
                    x,
                    x.Cards.Any(fun x -> x.UserId = userId),
                    x.TemplateRevision, // .Include fails for some reason, so we have to manually select
                    x.Concept,
                    x.Concept.Author
                ).ToPagedListAsync(pageNumber, 15)
            let squashed =
                r |> List.ofSeq |> List.map (fun (c, isCollected, template, concept, author) ->
                    c.Concept <- concept
                    c.Concept.Author <- author
                    c.TemplateRevision <- template
                    c, isCollected
                )
            return {
                Results =
                    squashed |> List.map (fun (c, isCollected) ->
                        {   ExploreConceptSummary.Id = c.ConceptId
                            Author = c.Concept.Author.DisplayName
                            AuthorId = c.Concept.AuthorId
                            Users = c.Concept.Users
                            Revision = RevisionMeta.load isCollected true c
                        }
                    )
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }
    let search (db: CardOverflowDb) userId (pageNumber: int) order (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.LatestDefaultRevision.Search(searchTerm, plain, wildcard, order)
        |> searchExplore userId pageNumber
    let searchDeck (db: CardOverflowDb) userId (pageNumber: int) order (searchTerm: string) deckId =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.Deck
            .Where(fun x -> x.Id = deckId && (x.IsPublic || x.UserId = userId))
            .SelectMany(fun x -> x.Cards.Select(fun x -> x.Revision))
            .Search(searchTerm, plain, wildcard, order)
        |> searchExplore userId pageNumber

module NotificationRepository =
    let get (db: CardOverflowDb) userId (pageNumber: int) = task {
        let! ns =
            db.ReceivedNotification
                .Where(fun x -> x.ReceiverId = userId)
                .Select(fun x ->
                    x.Notification,
                    x.Notification.Sender.DisplayName,
                    x.Notification.Concept.Cards.Where(fun x -> x.UserId = userId).ToList(),
                    x.Notification.Deck.Name,
                    x.Notification.Deck.DerivedDecks.SingleOrDefault(fun x -> x.UserId = userId),
                    x.Notification.Revision.MaxIndexInclusive
                ).ToPagedListAsync(pageNumber, 30)
        return {
            Results = ns |> Seq.map Notification.load
            Details = {
                CurrentPage = ns.PageNumber
                PageCount = ns.PageCount
            }
        }
    }
    let remove (db: CardOverflowDb) (userId: Guid) (notificationId: Guid) =
        FormattableStringFactory.Create("""SELECT public.fn_delete_received_notification({0},{1});""", notificationId, userId)
        |> db.Database.ExecuteSqlInterpolatedAsync
        |>% ignore

[<CLIMutable>]
type DeckWithFollowMeta =
    {   Id: Guid
        Name: string
        AuthorId: Guid
        AuthorName: string
        IsFollowed: bool
        FollowCount: int
        TsvRank: double }
