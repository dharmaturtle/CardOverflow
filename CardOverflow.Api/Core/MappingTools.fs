module MappingTools

let stringIntToBool =
    function
    | "0" -> false
    | _-> true

// https://en.wikipedia.org/wiki/C0_and_C1_control_codes

let split separator (string: string) =
    string.Split [|separator|] |> Array.toList

let splitByFileSeparator =
    split '\x1c'

let splitByGroupSeparator =
    split '\x1d'

let splitByRecordSeparator =
    split '\x1e'

let splitByUnitSeparator =
    split '\x1f'
