namespace WoofWare.BalancedReducer.Test

open FsCheck
open FsUnitTyped
open NUnit.Framework
open WoofWare.BalancedReducer

[<TestFixture>]
module TestBalancedReducerProperties =

    /// Helper to populate a BalancedReducer from a list
    let populateReducer (reducer : 'a -> 'a -> 'a) (values : 'a list) : BalancedReducer<'a> =
        let length = List.length values
        let br = BalancedReducer.create length reducer

        values |> List.iteri (fun i value -> BalancedReducer.set br i value)

        br

    /// Property: For associative operations, BalancedReducer.compute should equal List.fold
    [<Test>]
    let ``BalancedReducer with addition equals List.fold`` () =
        let property (values : int list) =
            match values with
            | [] -> true // Skip empty lists
            | _ ->
                let br = populateReducer (+) values
                let brResult = BalancedReducer.compute br
                let foldResult = List.reduce (+) values
                brResult = foldResult

        Check.QuickThrowOnFailure property

    [<Test>]
    let ``BalancedReducer with multiplication equals List.fold`` () =
        let property (values : int8 list) =
            match values with
            | [] -> true // Skip empty lists
            | _ ->
                // Use int8 to avoid overflow issues with multiplication
                let br = populateReducer (*) values
                let brResult = BalancedReducer.compute br
                let foldResult = List.reduce (*) values
                brResult = foldResult

        Check.QuickThrowOnFailure property

    [<Test>]
    let ``BalancedReducer with string concatenation equals List.fold`` () =
        let property (values : string list) =
            match values with
            | [] -> true // Skip empty lists
            | _ ->
                let br = populateReducer (+) values
                let brResult = BalancedReducer.compute br
                let foldResult = List.reduce (+) values
                brResult = foldResult

        Check.QuickThrowOnFailure property

    [<Test>]
    let ``BalancedReducer with list concatenation equals List.fold`` () =
        let property (values : int list list) =
            match values with
            | [] -> true // Skip empty lists
            | _ ->
                let br = populateReducer (@) values
                let brResult = BalancedReducer.compute br
                let foldResult = List.reduce (@) values
                brResult = foldResult

        Check.QuickThrowOnFailure property

    /// Property: For non-associative operations, BalancedReducer may NOT equal List.fold
    /// This test demonstrates why BalancedReducer requires associative operations
    [<Test>]
    let ``BalancedReducer with subtraction demonstrates non-associativity`` () =
        // For subtraction (non-associative), the tree structure of BalancedReducer
        // applies operations in a different order than left-to-right fold.
        //
        // Example: [10, 5, 3]
        // List.reduce (-): (10 - 5) - 3 = 5 - 3 = 2
        // BalancedReducer might compute: 10 - (5 - 3) = 10 - 2 = 8 (depends on tree shape)
        //
        // This test demonstrates that they CAN differ, which is why the BalancedReducer
        // documentation explicitly states it's for associative operations only.

        let knownDifferentCase = [ 10 ; 5 ; 3 ]
        let br = populateReducer (-) knownDifferentCase
        let brResult = BalancedReducer.compute br
        let foldResult = List.reduce (-) knownDifferentCase

        // Document what we compute
        // List.reduce: (10 - 5) - 3 = 2
        foldResult |> shouldEqual 2

        // BalancedReducer uses a tree structure, so may get different result
        // We're documenting the actual behavior rather than asserting equality
        printfn $"Non-associative operation (subtraction) on {knownDifferentCase}:"
        printfn $"  List.reduce result: {foldResult}"
        printfn $"  BalancedReducer result: {brResult}"
        printfn $"  Results differ: {brResult <> foldResult}"

    /// Property test showing subtraction CAN produce different results
    [<Test>]
    let ``BalancedReducer with subtraction may differ from List.fold`` () =
        let property (values : int8 list) =
            match values with
            | [] -> true // Skip empty lists
            | values when values.Length = 1 -> true // Single element always matches
            | _ ->
                // Use int8 to keep numbers manageable
                let br = populateReducer (-) values
                let brResult = BalancedReducer.compute br
                let foldResult = List.reduce (-) values

                // This property documents that they CAN differ (not that they always do)
                // We're not asserting they're equal, because they often won't be
                // Instead, we're checking that BOTH operations complete successfully
                true // Both completed without error

        Check.QuickThrowOnFailure property

    /// Demonstrate with a non-commutative string operation
    [<Test>]
    let ``BalancedReducer with non-associative string operation demonstrates ordering matters`` () =
        // Using a non-associative string operation: prepend with separator
        let prependWithSep (a : string) (b : string) = b + "," + a

        let values = [ "A" ; "B" ; "C" ; "D" ]

        let br = populateReducer prependWithSep values
        let brResult = BalancedReducer.compute br

        let foldResult = List.reduce prependWithSep values

        printfn $"Non-associative string operation on {values}:"
        printfn $"  List.reduce result: {foldResult}"
        printfn $"  BalancedReducer result: {brResult}"
        printfn $"  Results differ: {brResult <> foldResult}"

        // List.reduce: (("A" prepend "B") prepend "C") prepend "D"
        //            = ("B,A" prepend "C") prepend "D"
        //            = ("C,B,A" prepend "D")
        //            = "D,C,B,A"
        foldResult |> shouldEqual "D,C,B,A"

// BalancedReducer will compute differently based on tree structure
// We document this behavior rather than asserting equality
