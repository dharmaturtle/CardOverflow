namespace CardOverflow.Test

open LoadersAndCopiers
open CardOverflow.Api
open CardOverflow.Test
open CardOverflow.Debug
open CardOverflow.Pure
open Xunit
open System
open System.Linq
open System.ComponentModel.DataAnnotations
open FsCheck
open FsCheck.Xunit
open CardOverflow.Entity
open NodaTime

module internal Gen =
    let gen<'a> = // uses the reflection based default generator instead of the registered generator
        Arb.Default.Derive<'a>()
        |> Arb.toGen
    let genMap<'a> f: Gen<'a> =
        gen<'a>
        |> Gen.map f
    let sample1 x =
        x |> Gen.elements |> Gen.sample 0 1 |> List.head
    let sample1Gen x =
        x |> Gen.sample 10 1 |> List.head

type NodaGen =
    static member instant () =
        Arb.generate<System.DateTime>
        |> Gen.map (fun dt -> dt.ToUniversalTime())
        |> Gen.map (fun dt -> Instant.FromDateTimeUtc dt)
        |> Arb.fromGen

module Generators =
    let alphanumericChar = ['a'..'z'] @ ['A'..'Z'] @ ['0'..'9'] |> Gen.elements
    let alphanumericString = alphanumericChar |> Gen.nonEmptyListOf |> Gen.map (List.toArray >> String)
    let seqOfLength lengthInterval generator =
        gen {
            let! length = Gen.choose lengthInterval
            let! items = generator |> Gen.arrayOfLength length
            return items |> Seq.ofArray
        }
    let stringOfLength lengthInterval =
        Arb.generate<char>
        |> seqOfLength lengthInterval
        |> Gen.map String.Concat
    let differentPositives n =
        Arb.Default.PositiveInt().Generator
        |> Gen.map (fun x -> x.Get)
        |> Gen.listOfLength n
        |> Gen.filter (fun x -> x.Distinct().Count() = n)
    let standardTemplate fields =
        gen {
            let templateGen =
                gen {
                    let! name = Arb.generate<string>
                    let! front = Gen.elements fields
                    let! back = Gen.elements fields
                    return
                        {   Name = name
                            Front = "{{" + front + "}}"
                            Back = "{{FrontSide}}<hr id=answer>{{" + back + "}}"
                            ShortFront = ""
                            ShortBack = ""
                        }
                }
            let! templates = Gen.nonEmptyListOf templateGen
            return Standard templates
        }
    let clozeTemplate fields =
        gen {
            let! name = Arb.generate<string>
            let! text = Gen.elements fields
            let! extra = Gen.elements fields
            return
                {   Name = name
                    Front = "{{cloze:" + text + "}}"
                    Back = "{{cloze:" + text + "}}<br>{{" + extra + "}}"
                    ShortFront = ""
                    ShortBack = ""
                } |> Cloze
        }
    let gromplateType fields =
        Gen.oneof [
            standardTemplate fields
            clozeTemplate fields
        ]
    let fields =
        List.map (fun fieldName -> Gen.genMap<Field> (fun field -> { field with Name = fieldName }))
        >> Gen.sequence
    let grompleaf gromplateType fieldNames =
        gen {
            let! fields = fields fieldNames
            return!
                Gen.genMap<Grompleaf> (fun grompleaf -> {
                    grompleaf with
                        Fields = fields
                        Templates = gromplateType
                })
        }
    type ClozeText = ClozeText of string
    let clozeText =
        gen { // medTODO make more realistic
            let! a = alphanumericString
            let! b = alphanumericString
            let! c = alphanumericString
            return sprintf "%s{{c1::%s}}%s" a b c
        }
    let editStackCommand =
        Arb.register<NodaGen>() |> ignore
        gen {
            let! fieldNames = alphanumericString |> Gen.nonEmptyListOf
            let! gromplateType = gromplateType fieldNames
            let! grompleaf = grompleaf gromplateType fieldNames
            let values =
                match gromplateType with
                | Standard _ -> alphanumericString
                | Cloze _ -> clozeText
            let! fields = fields fieldNames
            let! fields =
                fields
                |> List.map (fun f -> values |> Gen.map (fun value -> { EditField = f; Value = value }))
                |> Gen.sequence
            let! kind = Gen.genMap<UpsertKind> (fun x ->
                match x with
                | NewOriginal_TagIds tags -> tags |> List.filter (fun t -> t <> null) |> NewOriginal_TagIds
                | NewCopy_SourceLeafId_TagIds (x, tags) ->
                    let tags = tags |> List.filter (fun t -> t <> null)
                    NewCopy_SourceLeafId_TagIds (x, tags)
                | _ -> x
            )
            return!
                Gen.genMap<EditStackCommand> (fun c ->
                    {   c with
                            FieldValues = fields |> toResizeArray
                            Grompleaf = grompleaf
                            Kind = kind
                    })
        }
    let notificationEntity = gen {
        let! timestamp = NodaGen.instant() |> Arb.toGen
        let! message = Arb.generate<string>
        let! notificationType = Gen.gen<NotificationType>
        return
            NotificationEntity(
                Id = Ulid.create,
                SenderId = Gen.sample1 [ user_1; user_2; user_3 ],
                Created = timestamp,
                Type = notificationType,
                Message = message
            )}
    let genString15 = // unused; here as an example
        Gen.sized (fun s -> Gen.resize (min s 15) Arb.generate<NonNull<string>>) |> Gen.map (fun (NonNull str) -> str)
    let uniqueInts length = gen {
        let initialValue = Int32.MinValue
        let array = Array.create length initialValue
        while array.Contains initialValue do
            let! id = Arb.generate<int>
            if not <| array.Contains id then
                array.[Array.IndexOf(array, initialValue)] <- id
        return array }
    let uniqueGuids length =
        Arb.generate<Guid> |> Gen.listOfLength length
    let StackLeafIds length = gen {
        let! stacks = uniqueGuids length
        let! branches = uniqueGuids length
        let! leafs = uniqueGuids length
        return
            Seq.zip3 stacks branches leafs
            |> Seq.map StackLeafIds.fromTuple
            |> List.ofSeq
        }
    let StackLeafIds3 =
        StackLeafIds 3
    let StackLeafIndex length = gen {
        let! stacks = uniqueGuids length
        let! branches = uniqueGuids length
        let! leafs = uniqueGuids length
        let! indexes = Arb.generate<int16> |> Gen.listOfLength length
        let! deckId = Arb.generate<Guid> |> Gen.listOfLength length
        let! cardId = uniqueGuids length
        return
            Seq.zip6 stacks branches leafs indexes deckId cardId
            |> Seq.map StackLeafIndex.fromTuple
            |> List.ofSeq
        }
    let StackLeafIndex3 =
        StackLeafIndex 3

type Generators =
    static member instant =
        NodaGen.instant()
    static member editStackCommand =
        Generators.editStackCommand |> Arb.fromGen
    static member clozeText =
        Generators.clozeText
        |> Gen.map Generators.ClozeText
        |> Arb.fromGen
    static member notificationEntity =
        Generators.notificationEntity |> Arb.fromGen
    static member StackLeafIds3 =
        Generators.StackLeafIds3 |> Arb.fromGen
    static member StackLeafIndex3 =
        Generators.StackLeafIndex3 |> Arb.fromGen

type GeneratorsAttribute() =
    inherit PropertyAttribute(Arbitrary = [| typeof<Generators> |])
