module NoCQS

open Equinox
open FsCodec
open FsCodec.NewtonsoftJson
open Serilog
open TypeShape
open CardOverflow.Pure
open CardOverflow.Api
open FsToolkit.ErrorHandling
open Domain
open Infrastructure
open FSharp.UMX
open CardOverflow.Pure.AsyncOp
open System
open NodaTime
open EventAppender
open FSharp.Control.Tasks

type User (appender: UserSaga.Appender, keyValueStore: KeyValueStore) =
    member _.getsert meta displayName = task {
        let! summary = keyValueStore.TryGet<Summary.User> meta.UserId
        return!
            match summary with
            | Some (s, _) -> s |> Ok |> Async.singleton
            | None -> asyncResult {
                let signedUp = appender.BuildSignedUp meta displayName
                do! appender.Create signedUp
                return User.Fold.evolveSignedUp signedUp
            }
        }
