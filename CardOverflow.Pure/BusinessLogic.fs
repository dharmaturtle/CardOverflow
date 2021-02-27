namespace CardOverflow.Pure

open CardOverflow.Debug
open System.Linq
open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open CardOverflow.Debug
open System.Text.RegularExpressions
open NodaTime

module Relationship =
    type RelationshipRegex = FSharp.Text.RegexProvider.Regex< """(?<source>.+)\/(?<target>.+)""" >
    let relationshipRegex =
        Regex.compiledIgnoreCase |> RelationshipRegex
    let isDirectional = relationshipRegex.IsMatch
    let split name =
        let x = relationshipRegex.TypedMatch name
        if x.Success then
            x.source.Value, x.target.Value
        else
            name, ""
    let flipName name =
        let x = relationshipRegex.TypedMatch name
        if x.Success then
            x.target.Value + "/" + x.source.Value
        else
            name

module CardHtml =
    type CardIndex =
        | Standard
        | Cloze of int16
    let generate fieldNameValueMap questionXemplate answerXemplate css i =
        let fieldNameValueMap, questionXemplate, answerXemplate =
            match i with
            | Standard ->
                fieldNameValueMap, questionXemplate, answerXemplate
            | Cloze i ->
                let i = i + 1s |> string
                let clozeFields = Cloze.templateRegex.TypedMatches(questionXemplate).Select(fun x -> x.fieldName.Value) |> List.ofSeq
                let fieldNameValueMap, unusedFields =
                    fieldNameValueMap |> List.partition (fun (fieldName, value) ->
                        let indexMatch = Cloze.regex.TypedMatches(value).Select(fun x -> x.clozeIndex.Value).Contains i
                        (indexMatch || (not <| clozeFields.Contains fieldName))
                    )
                let fieldNameValueMap =
                    fieldNameValueMap |> List.map(fun (fieldName, value) ->
                        let value =
                            Cloze.regex
                                .TypedMatches(value)
                                .Where(fun x -> x.clozeIndex.Value <> i)
                                .Select(fun x -> x.CompleteMatch.Value, x.answer.Value)
                            |> Seq.fold (fun (state: string) (completeMatch, answer) ->
                                state.Replace(completeMatch, answer)
                            ) value
                        fieldName, value
                    )
                let qt, at =
                    ((questionXemplate, answerXemplate), (unusedFields |> List.map fst))
                    ||> Seq.fold(fun (qt, (at: string)) fieldName ->
                        let irrelevantCloze = "{{cloze:" + fieldName + "}}"
                        qt.Replace(irrelevantCloze, ""),
                        at.Replace(irrelevantCloze, "")
                    )
                fieldNameValueMap, qt, at
        let replaceFields isFront template =
            (template, fieldNameValueMap)
            ||> List.fold(fun (previous: string) (fieldName, value) -> 
                let simple =
                    previous.Replace("{{" + fieldName + "}}", value)
                let showIfHasText =
                    let regex = Regex <| "{{#" + fieldName + @"}}(.*?){{\/" + fieldName + "}}"
                    if String.IsNullOrWhiteSpace value
                    then regex.Replace(simple, "")
                    else regex.Replace(simple, "$1")
                let showIfEmpty =
                    let regex = Regex <| @"{{\^" + fieldName + @"}}(.*?){{\/" + fieldName + "}}"
                    if String.IsNullOrWhiteSpace value
                    then regex.Replace(showIfHasText, "$1")
                    else regex.Replace(showIfHasText, "")
                let stripHtml =
                    showIfEmpty.Replace("{{text:" + fieldName + "}}", MappingTools.stripHtmlTags value)
                let cloze =
                    if isFront then
                        let regexMatches = Cloze.regex.TypedMatches(value).Select(fun x -> x.hint, x.Value) |> List.ofSeq
                        (value, regexMatches) ||> List.fold(fun current (hintGroup, rawCloze) ->
                            let hint =
                                if hintGroup.Success then
                                    hintGroup.Value
                                else
                                    "..."
                            let brackets = hint |> sprintf """
        <span class="cloze-brackets-front">[</span>
        <span class="cloze-filler-front">%s</span>
        <span class="cloze-brackets-front">]</span>
        """
                            current.Replace(rawCloze, brackets)
                        ) |> fun x -> stripHtml.Replace("{{cloze:" + fieldName + "}}", x)
                    else
                        let html =
                            sprintf """
        <span class="cloze-brackets-back">[</span>
        %s
        <span class="cloze-brackets-back">]</span>
        """
                        let answer = Cloze.regex.TypedReplace(value, fun f -> html f.answer.Value)
                        stripHtml.Replace("{{cloze:" + fieldName + "}}", answer)
                cloze
            )
        let frontSide =
            replaceFields true questionXemplate
        let backSide =
            (replaceFields false answerXemplate).Replace("{{FrontSide}}", replaceFields false questionXemplate)
        let htmlBase =
            sprintf """<!DOCTYPE html>
    <head>
        <style>
            .cloze-brackets-front {
                font-size: 150%%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-filler-front {
                font-size: 150%%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-brackets-back {
                font-size: 150%%;
                font-family: monospace;
                font-weight: bolder;
                color: red;
            }
        </style>
        <style>
            %s
        </style>
    </head>
    <body>
        %s
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>"""
                css
        htmlBase frontSide,
        htmlBase backSide,
        MappingTools.stripHtmlTagsForDisplay <| frontSide,
        MappingTools.stripHtmlTagsForDisplay <| (replaceFields false answerXemplate).Replace("{{FrontSide}}", "")

[<CLIMutable>]
type DateCount = {
    Date: LocalDate
    Count: int
}
type DateCountLevel = {
    Date: LocalDate
    Count: int
    Level: int
}
type Heatmap = {
    DateCountLevels: DateCountLevel list
    DailyAverageReviews: int
    DaysLearnedPercent: int
    LongestStreakDays: int
    CurrentStreakDays: int
}
module Heatmap =
    let maxConseuctive =
        let rec maxConseuctive localMax globalMax =
            function
            | h :: t ->
                if h = 0 then
                    maxConseuctive 0 globalMax t
                else
                    let localMax = localMax + 1
                    maxConseuctive localMax (max localMax globalMax) t
            | [] -> globalMax
        maxConseuctive 0 0
    let allDateCounts (startDate: LocalDate) (endDate: LocalDate) (dateCounts: DateCount list) =
        let allDatesSorted = startDate |> List.unfold (fun x -> if x <= endDate then Some(x, x.PlusDays 1) else None) // https://stackoverflow.com/a/20362003
        query { // https://stackoverflow.com/a/26008852
            for date in allDatesSorted do
            leftOuterJoin dateCount in dateCounts
                on (date = dateCount.Date) into result
            for dateCount in result do
            select {
                Date = date
                Count = dateCount |> toOption |> Option.map (fun x -> x.Count) |> Option.defaultValue 0
            }
        } |> List.ofSeq
    let addLevels (dateCounts: DateCount list) =
        let levelCount = 11. // 0 to 10 is 11
        let maxCount = dateCounts |> List.map (fun x -> x.Count) |> List.ifEmptyThen 0 |> List.max |> max 1 |> float
        dateCounts |> List.map (fun { Date = date; Count = count } ->
        {   Date = date
            Count = count
            Level = float count / maxCount * (levelCount - 1.) |> round // -1 to remove 0
        })
    let get (startDate: LocalDate) (endDate: LocalDate) (dateCounts: DateCount list) =
        let dateCounts = allDateCounts startDate endDate dateCounts
        let counts = dateCounts |> List.map (fun x -> x.Count)
        let relevantRange = counts |> List.skipWhile (fun x -> x = 0) |> List.ifEmptyThen 0
        {   DateCountLevels = addLevels dateCounts
            DailyAverageReviews = relevantRange |> List.averageBy (fun x -> float x) |> round
            DaysLearnedPercent = float (relevantRange.Count(fun x -> x <> 0)) / float relevantRange.Length * 100. |> round 
            LongestStreakDays = counts |> maxConseuctive
            CurrentStreakDays = counts |> Seq.rev |> Seq.takeWhile (fun x -> x <> 0) |> Seq.length
        }

[<CLIMutable>]
type UpsertIds = {
    ConceptId: Guid
    BranchId: Guid
    LeafId: Guid
    CardIds: Guid list // should be ordered by index
}
module UpsertIds =
    let fromTuple (conceptId, branchId, leafId, cardIds) =
        {   ConceptId = conceptId
            BranchId = branchId
            LeafId = leafId
            CardIds = cardIds
        }

type ConceptLeafIds = {
    ConceptId: Guid
    BranchId: Guid
    LeafId: Guid
} with
    member this.ToUpsertIds cardIds =
        {   ConceptId = this.ConceptId
            BranchId = this.BranchId
            LeafId = this.LeafId
            CardIds = cardIds
        }

type ConceptLeafIndex = {
    ConceptId: Guid
    BranchId: Guid
    LeafId: Guid
    Index: int16
    DeckId: Guid
    CardId: Guid
}

module ConceptLeafIds =
    let fromTuple (conceptId, branchId, leafId) =
        {   ConceptId = conceptId
            BranchId = branchId
            LeafId = leafId
        }

module ConceptLeafIndex =
    let fromTuple (conceptId, branchId, leafId, index, deckId, cardId) =
        {   ConceptId = conceptId
            BranchId = branchId
            LeafId = leafId
            Index = index
            DeckId = deckId
            CardId = cardId
        }

type DiffState =
    | Unchanged of ConceptLeafIndex
    | LeafChanged of ConceptLeafIndex * ConceptLeafIndex // theirs, mine
    | BranchChanged of ConceptLeafIndex * ConceptLeafIndex
    | AddedConcept of ConceptLeafIndex
    | RemovedConcept of ConceptLeafIndex

[<CLIMutable>]
type DiffStateSummary = {
    Unchanged: ConceptLeafIndex list
    LeafChanged: (ConceptLeafIndex * ConceptLeafIndex) list // theirs, mine
    BranchChanged: (ConceptLeafIndex * ConceptLeafIndex) list
    AddedConcept: ConceptLeafIndex list
    RemovedConcept: ConceptLeafIndex list
    MoveToAnotherDeck: ConceptLeafIndex list
} with
    member this.DeckIds =
        let tupleToList (a, b) = [a; b]
        [   this.Unchanged
            (this.LeafChanged |> List.collect tupleToList)
            (this.BranchChanged |> List.collect tupleToList)
            this.AddedConcept
            this.RemovedConcept
        ] |> List.concat
        |> List.map (fun x -> x.DeckId)
        |> List.distinct

module Diff =
    let ids aIds bIds =
        let sort = List.sortBy (fun x -> x.ConceptId, x.BranchId, x.LeafId, x.Index)
        let aIds = aIds |> sort
        let bIds = bIds |> sort
        List.zipOn aIds bIds (fun a b -> a.ConceptId = b.ConceptId && a.Index = b.Index)
        |> List.map (
            function
            | Some a, Some b ->
                if a.LeafId = b.LeafId && a.Index = b.Index then
                    Unchanged b
                elif a.BranchId = b.BranchId then
                    LeafChanged (a, b)
                else
                    BranchChanged (a, b)
            | Some a, None   -> AddedConcept a
            | None  , Some b -> RemovedConcept b
            | None  , None   -> failwith "impossible"
        )
    let toSummary diffStates =
        let unchanged             = ResizeArray.empty
        let leafChanged = ResizeArray.empty
        let branchChanged         = ResizeArray.empty
        let addedConcept            = ResizeArray.empty
        let removedConcept          = ResizeArray.empty
        diffStates |> List.iter
            (function
            | Unchanged x ->
                unchanged.Add x
            | LeafChanged (x, y) ->
                leafChanged.Add (x, y)
            | BranchChanged (x, y) ->
                branchChanged.Add (x, y)
            | AddedConcept x ->
                addedConcept.Add x
            | RemovedConcept x ->
                removedConcept.Add x)
        let moveToAnotherDeck, removedConcept =
            removedConcept |> List.ofSeq |> List.partition (fun r ->
                unchanged
                    .Select(fun x -> (x.ConceptId, x.BranchId, x.LeafId))
                    .Contains(       (r.ConceptId, r.BranchId, r.LeafId)))
        {   Unchanged = unchanged |> Seq.toList
            LeafChanged = leafChanged |> Seq.toList
            BranchChanged = branchChanged |> Seq.toList
            AddedConcept = addedConcept |> Seq.toList
            RemovedConcept = removedConcept
            MoveToAnotherDeck = moveToAnotherDeck
        }
