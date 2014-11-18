// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2013 IntelliFactory
//
// GNU Affero General Public License Usage
// WebSharper is free software: you can redistribute it and/or modify it under
// the terms of the GNU Affero General Public License, version 3, as published
// by the Free Software Foundation.
//
// WebSharper is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License
// for more details at <http://www.gnu.org/licenses/>.
//
// If you are unsure which license is appropriate for your use, please contact
// IntelliFactory at http://intellifactory.com/contact.
//
// $end{copyright}

/// Defines macros used by proxy definitions.
module IntelliFactory.WebSharper.Macro

module C = IntelliFactory.JavaScript.Core
module M = IntelliFactory.WebSharper.Core.Macros
module Q = IntelliFactory.WebSharper.Core.Quotations
module R = IntelliFactory.WebSharper.Core.Reflection

let macro x : M.Macro =
    {
        Body         = None
        Expand       = x
        Requirements = []
    }

let smallIntegralTypes =
    Set [
        "System.Byte"
        "System.SByte"
        "System.Int16"
        "System.Int32"
        "System.UInt16"
        "System.UInt32"
    ]

let bigIntegralTypes =
    Set [
        "System.Decimal"
        "System.Int64"
        "System.UInt64" 
    ]

let integralTypes = smallIntegralTypes + bigIntegralTypes

let scalarTypes =
    integralTypes
    + Set [
        "System.Char"
        "System.Double"
        "System.Single"
        "System.String" 
        "System.TimeSpan"
        "System.DateTime"
    ]

let isIn (s: string Set) (t: R.Type) = 
    match t with
    | R.Type.Concrete (d, _) ->
        s.Contains d.FullName
    | _ ->
        false

let (|CallOrCM|_|) q =
    match q with 
    | Q.Call (m, l)
    | Q.CallModule (m, l) -> Some (m, l)
    | _ -> None

let (|OptCoerce|) q =
    match q with
    | Q.Coerce (_, x)
    | x -> x

let cString s = !~ (C.String s)
let cCall t m x = C.Call (t, cString m, x)
let cCallG l m x = cCall (C.Global l) m x
let cInt x = !~ (C.Integer (int64 x))

let divisionMacro = macro <| fun tr q ->
    match q with
    | CallOrCM (m, [x; y]) ->
        match m.Generics with
        | t :: _ -> if isIn smallIntegralTypes t
                    then (tr x / tr y) &>> cInt 0
                    elif isIn bigIntegralTypes t
                    then cCallG ["Math"] "floor" [tr x / tr y]
                    else tr x / tr y
        | _      -> tr x / tr y
    | _ ->
        failwith "divisionMacro error"

let arithMacro name def = macro <| fun tr q ->
    match q with
    | CallOrCM (m, [x; y]) ->
        match m.Generics with
        | t :: _ ->
            if isIn scalarTypes t
                then def (tr x) (tr y)
                else cCall (tr x) name [tr y]
        | _ -> def (tr x) (tr y)
    | _ ->
        failwith "arithMacro error"

let addMacro = arithMacro "add" ( + )
let subMacro = arithMacro "sub" ( - )

[<Sealed>]
type Add() =
    interface M.IMacroDefinition with
        member this.Macro = addMacro

[<Sealed>]
type Sub() =
    interface M.IMacroDefinition with
        member this.Macro = subMacro

[<Sealed>]
type Division() =
    interface M.IMacroDefinition with
        member this.Macro = divisionMacro

type Comparison =
    | ``<``  = 0
    | ``<=`` = 1
    | ``>``  = 2
    | ``>=`` = 3
    | ``=``  = 4
    | ``<>`` = 5

type B = C.BinaryOperator

let toBinaryOperator cmp =
    match cmp with
    | Comparison.``<``  -> B.``<``
    | Comparison.``<=`` -> B.``<=``
    | Comparison.``>``  -> B.``>``
    | Comparison.``>=`` -> B.``>=``
    | Comparison.``=``  -> B.``===``
    | _                 -> B.``!==``

let makeComparison cmp x y =
    let f m x y = cCallG ["IntelliFactory"; "WebSharper"; "Unchecked"] m [x; y]
    let c b i   = C.Binary (f "Compare" x y, b, cInt i)
    match cmp with
    | Comparison.``<``  -> c B.``===`` -1
    | Comparison.``<=`` -> c B.``<=`` 0
    | Comparison.``>``  -> c B.``===`` 1
    | Comparison.``>=`` -> c B.``>=`` 0
    | Comparison.``=``  -> f "Equals" x y
    | _                 -> !!(f "Equals" x y)

let comparisonMacro cmp = macro <| fun tr q ->
    match q with
    | CallOrCM (m, [x; y]) ->
        match m.Generics with
        | t :: _ ->
            if isIn scalarTypes t then
                C.Binary (tr x, toBinaryOperator cmp, tr y)
            else
                makeComparison cmp (tr x) (tr y)
        | _ ->
            failwith "comparisonMacro error"
    | _ ->
        failwith "comparisonMacro error"

[<AbstractClass>]
type CMP(c: Comparison) =
    interface M.IMacroDefinition with
        member this.Macro = comparisonMacro c

[<Sealed>] type EQ() = inherit CMP(Comparison.``=``)
[<Sealed>] type NE() = inherit CMP(Comparison.``<>``)
[<Sealed>] type LT() = inherit CMP(Comparison.``<``)
[<Sealed>] type GT() = inherit CMP(Comparison.``>``)
[<Sealed>] type LE() = inherit CMP(Comparison.``<=``)
[<Sealed>] type GE() = inherit CMP(Comparison.``>=``)

let charMacro = macro <| fun tr q ->
    match q with
    | CallOrCM (m, [x]) ->
        match m.Generics with
        | t :: _ ->
            if isIn integralTypes t then tr x else
                match t with
                | R.Type.Concrete (d, _) ->
                    match d.FullName with
                    | "System.String" -> cCall (tr x) "charCodeAt" [cInt 0]
                    | "System.Char"
                    | "System.Double"
                    | "System.Single" -> tr x
                    | _               -> failwith "charMacro error"
                | _ ->
                    failwith "charMacro error"
        | _ ->
            failwith "charMacro error"
    | _ ->
        failwith "charMacro error"

[<Sealed>]
type Char() =
    interface M.IMacroDefinition with
        member this.Macro = charMacro

let stringMacro = macro <| fun tr q ->
    match q with
    | CallOrCM (m, [x]) ->
        match m.Generics with
        | t :: _ ->
            match t.FullName with
            | "System.Char" -> cCallG ["String"] "fromCharCode" [tr x]
            | _             -> cCallG [] "String" [tr x]
        | _ ->
            failwith "comparisonMacro error"
    | _ ->
        failwith "comparisonMacro error"

[<Sealed>]
type String() =
    interface M.IMacroDefinition with
        member this.Macro = stringMacro

let getFieldsList q =
    let ``is (=>)`` (m: R.Method) =
        m.DeclaringType.FullName = "IntelliFactory.WebSharper.Pervasives"
        && m.Name = "op_EqualsGreater"
    let rec getFieldsListTC l q =
        match q with
        | Q.NewUnionCase (_, [Q.NewTuple [Q.Value (Q.String n); v]; t]) ->
            getFieldsListTC ((n, v) :: l) t         
        | Q.NewUnionCase (_, [Q.CallModule (m, [Q.Value (Q.String n); v]); t])
            when m.Entity |> ``is (=>)`` ->
            getFieldsListTC ((n, v) :: l) t         
        | Q.NewUnionCase (_, []) -> Some (l |> List.rev) 
        | Q.NewArray (_,  l) ->
            l |> List.map (
                function 
                | Q.NewTuple [Q.Value (Q.String n); v] -> n, v 
                | Q.CallModule (m, [Q.Value (Q.String n); v])
                    when m.Entity |> ``is (=>)`` -> n, v
                | _ -> failwith "Wrong type of array passed to New"
            ) |> Some
        | _ -> None
    getFieldsListTC [] q

let newMacro = macro <| fun tr q ->
    match q with
    | CallOrCM (_, [OptCoerce x]) ->
        match getFieldsList x with
        | Some xl ->
            C.NewObject (xl |> List.map (fun (n, v) -> n, tr v))
        | _ ->
            cCallG ["IntelliFactory"; "WebSharper"; "Pervasives"] "NewFromList" [tr x]
    | _ ->
        failwith "newMacro error"

[<Sealed>]
type New() =
    interface M.IMacroDefinition with
        member this.Macro = newMacro

/// Set of helpers to parse format string
/// Source: https://github.com/fsharp/fsharp/blob/master/src/fsharp/FSharp.Core/printf.fs
module private FormatString =
    [<System.Flags>]
    type FormatFlags = 
        | None = 0
        | LeftJustify = 1
        | PadWithZeros = 2
        | PlusForPositives = 4
        | SpaceForPositives = 8

    let inline hasFlag flags (expected : FormatFlags) = (flags &&& expected) = expected
    let inline isLeftJustify flags = hasFlag flags FormatFlags.LeftJustify
    let inline isPadWithZeros flags = hasFlag flags FormatFlags.PadWithZeros
    let inline isPlusForPositives flags = hasFlag flags FormatFlags.PlusForPositives
    let inline isSpaceForPositives flags = hasFlag flags FormatFlags.SpaceForPositives

    /// Used for width and precision to denote that user has specified '*' flag
    [<Literal>]
    let StarValue = -1
    /// Used for width and precision to denote that corresponding value was omitted in format string
    [<Literal>]
    let NotSpecifiedValue = -2

    [<NoComparison; NoEquality>]
    type FormatSpecifier =
        {
            TypeChar : char
            Precision : int
            Width : int
            Flags : FormatFlags
        }
        member this.IsStarPrecision = this.Precision = StarValue
        member this.IsPrecisionSpecified = this.Precision <> NotSpecifiedValue
        member this.IsStarWidth = this.Width = StarValue
        member this.IsWidthSpecified = this.Width <> NotSpecifiedValue

    let inline isDigit c = c >= '0' && c <= '9'
    let intFromString (s : string) pos = 
        let rec go acc i =
            if isDigit s.[i] then 
                let n = int s.[i] - int '0'
                go (acc * 10 + n) (i + 1)
            else acc, i
        go 0 pos

    let parseFlags (s : string) i : FormatFlags * int = 
        let rec go flags i = 
            match s.[i] with
            | '0' -> go (flags ||| FormatFlags.PadWithZeros) (i + 1)
            | '+' -> go (flags ||| FormatFlags.PlusForPositives) (i + 1)
            | ' ' -> go (flags ||| FormatFlags.SpaceForPositives) (i + 1)
            | '-' -> go (flags ||| FormatFlags.LeftJustify) (i + 1)
            | _ -> flags, i
        go FormatFlags.None i

    let parseWidth (s : string) i : int * int = 
        if s.[i] = '*' then StarValue, (i + 1)
        elif isDigit (s.[i]) then intFromString s i
        else NotSpecifiedValue, i

    let parsePrecision (s : string) i : int * int = 
        if s.[i] = '.' then
            if s.[i + 1] = '*' then StarValue, i + 2
            elif isDigit (s.[i + 1]) then intFromString s (i + 1)
            else failwith "invalid precision value"
        else NotSpecifiedValue, i
    
    let parseTypeChar (s : string) i : char * int = 
        s.[i], (i + 1)

    type Part =
        | StringPart of string
        | FormatPart of FormatSpecifier

    /// modified version of FSharp.Core findNextFormatSpecifier, parses whole format string
    let parseAll (s : string) = 
        let parts = ResizeArray() 
        let rec go i (buf : System.Text.StringBuilder) =
            if i >= s.Length then 
                if buf.Length > 0 then parts.Add (StringPart (string buf))
            else
                let c = s.[i]
                if c = '%' then
                    if i + 1 < s.Length then
                        let f, i1 = parseFlags s (i + 1)
                        let w, i2 = parseWidth s i1
                        let p, i3 = parsePrecision s i2
                        let typeChar, i4 = parseTypeChar s i3
                        // shortcut for the simpliest case
                        // if typeChar is not % or it has star as width\precision - resort to long path
                        if typeChar = '%' && not (w = StarValue || p = StarValue) then 
                            buf.Append('%') |> ignore
                            go i4 buf
                        else 
                            if buf.Length > 0 then parts.Add (StringPart (string buf))
                            parts.Add (
                                FormatPart {
                                    TypeChar  = typeChar
                                    Precision = p
                                    Width     = w
                                    Flags     = f
                                }
                            )
                            go i4 (buf.Clear())
                    else
                        failwith "Missing format specifier"
                else 
                    buf.Append(c) |> ignore
                    go (i + 1) buf
        go 0 (System.Text.StringBuilder())
        parts.ToArray()

let createPrinter fs =
    let parts = FormatString.parseAll fs
    let args =
        [
            for p in parts do
                match p with
                | FormatString.FormatPart f ->
                    yield C.Id()
                    if f.IsStarWidth then yield C.Id()
                    if f.IsStarPrecision then yield C.Id()
                | _ -> () 
        ]
    let helpers = ["IntelliFactory"; "WebSharper"; "PrintfHelpers"] 
    let strings = ["IntelliFactory"; "WebSharper"; "Strings"]
        
    let rArgs = ref args
    let nextVar() =
        match !rArgs with
        | a :: r ->
            rArgs := r
            C.Var a
        | _ -> failwith "sprintfMacro error"   
        
    let withPadding (f: FormatString.FormatSpecifier) t =
        if f.IsWidthSpecified then
            let width = if f.IsStarWidth then nextVar() else cInt f.Width
            let s = t (nextVar())
            if FormatString.isLeftJustify f.Flags then
                cCallG strings "PadRight" [s; width]
            else
                if FormatString.isPadWithZeros f.Flags then
                    cCallG helpers "padNumLeft" [s; width]
                else
                    cCallG strings "PadLeft" [s; width]
        else t (nextVar())
        
    let numberToString (f: FormatString.FormatSpecifier) t =
        withPadding f (fun n ->
            if FormatString.isPlusForPositives f.Flags then cCallG helpers "plusForPos" [n; t n]
            elif FormatString.isSpaceForPositives f.Flags then cCallG helpers "spaceForPos" [n; t n]
            else t n
        )

    let inner = 
        parts
        |> Seq.map (function
            | FormatString.StringPart s -> cString s
            | FormatString.FormatPart f ->
                match f.TypeChar with
                | 'b'
                | 'O' -> 
                    withPadding f (fun s -> cCallG [] "String" [s])
                | 'A' -> 
                    withPadding f (fun s -> cCallG helpers "prettyPrint" [s])
                | 'c' -> 
                    withPadding f (fun s -> cCallG ["String"] "fromCharCode" [s])   
                | 's' -> 
                    withPadding f (fun s -> cCallG helpers "toSafe" [s])
                | 'd' | 'i' ->
                    numberToString f (fun s -> cCallG [] "String" [s])
                | 'x' ->                                           
                    numberToString f (fun n -> cCall n "toString" [cInt 16])
                | 'X' ->                                           
                    numberToString f (fun n -> cCall (cCall n "toString" [cInt 16]) "toUpperCase" [])
                | 'o' ->                                           
                    numberToString f (fun n -> cCall n "toString" [cInt 8])
                | 'e' ->
                    numberToString f (fun n -> cCall n "toExponential" []) 
                | 'E' ->
                    numberToString f (fun n -> cCall (cCall n "toExponential" []) "toUpperCase" []) 
                | 'f' | 'F' | 'M' ->
                    numberToString f (fun n ->
                        let prec =
                            if f.IsPrecisionSpecified then
                                if f.IsStarPrecision then nextVar() else cInt f.Precision
                            else cInt 6 // Default precision
                        cCall n "toFixed" [prec]
                    )
                | c -> failwithf "Failed to parse format string: '%%%c' is not supported." c
        )
        |> Seq.reduce (+)
    
    let k = C.Id() 
    C.Lambda(None, [k],
        args |> List.rev |> List.fold (fun c a -> C.Lambda(None, [a], c)) (C.Var k).[[inner]]
    )
    
let printfMacro = macro <| fun tr q ->
    match q with
    | Q.NewObject (_, [Q.Value (Q.String fs)]) ->
        createPrinter fs
    | _ ->
        failwith "printfMacro error"

[<Sealed>]
type PrintF() =
    interface M.IMacroDefinition with
        member this.Macro = printfMacro
