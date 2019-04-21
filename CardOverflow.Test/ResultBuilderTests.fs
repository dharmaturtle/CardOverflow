module ResultBuilderTests

open CardOverflow.Api
open Xunit

let result = new ResultBuilder()

type MyErr = Err1 | Err2

[<Fact>]
let ``Runs ResultBuilder's example``() = // http://www.fssnip.net/7UJ/title/ResultBuilder-Computational-Expression
    let aa: Result<string, MyErr> = 
        result {
            let! (a: string) = Ok "a string"
            printfn "A: %A" a
            //let! b = Error Err2
            //printfn "B: %A" b
            let! c = (Some "c string", Err1)
            //let! c = (None, Err1)
            printfn "C: %A" c
            let d = if true then a else c
            printfn "D: %A" d
            return d
        }
    printfn "Result: %A" aa

[<Fact>]
let ``ResultBuilder returns an Error (message) on None``() =
    let getResult =
        result {
            let option = Some "You got the value!"
            let option = None
            let! r = (option, "Oops, `option` is None!")
            return r
        }
    printfn "%A" getResult
