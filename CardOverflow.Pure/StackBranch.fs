module Domain.StackBranch

open FsToolkit.ErrorHandling

type Service
    (   stacks : Stack.Service,
        branches : Branch.Service) =

    member _.Create(stackSnapshot, branchSnapshot) = asyncResult {
        do! stacks.Create stackSnapshot
        return! branches.Create branchSnapshot
    }
