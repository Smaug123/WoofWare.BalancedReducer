namespace WoofWare.BalancedReducer.Test

open FsCheck
open FsUnitTyped
open NUnit.Framework
open WoofWare.BalancedReducer

[<TestFixture>]
module TestBalancedReducerProperties =
    let config = Config.QuickThrowOnFailure.WithQuietOnSuccess(true).WithMaxTest (10000)

    /// Helper to populate a BalancedReducer from a list
    let populateReducer (reducer : 'a -> 'a -> 'a) (values : 'a list) : BalancedReducer<'a> =
        let length = List.length values
        let br = BalancedReducer.create length reducer

        values |> List.iteri (fun i value -> BalancedReducer.set br i value)

        br

    /// Property: For associative operations, BalancedReducer.compute should equal List.fold
    [<Test>]
    let ``BalancedReducer with addition equals List.fold`` () =
        let property (v1 : int) (values : int list) =
            let values = v1 :: values
            let br = populateReducer (+) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (+) values
            brResult = foldResult

        Check.One (config, property)

    [<Test>]
    let ``BalancedReducer with multiplication equals List.fold`` () =
        let property (v1 : int8) (values : int8 list) =
            let values = v1 :: values
            // Use int8 to avoid overflow issues with multiplication
            let br = populateReducer (*) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (*) values
            brResult = foldResult

        Check.One (config, property)

    [<Test>]
    let ``BalancedReducer with string concatenation equals List.fold`` () =
        let property (v1 : string) (values : string list) =
            let values = v1 :: values
            let br = populateReducer (+) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (+) values
            brResult = foldResult

        Check.One (config, property)

    [<Test>]
    let ``BalancedReducer with list concatenation equals List.fold`` () =
        let property (v1 : int list) (values : int list list) =
            let values = v1 :: values
            let br = populateReducer (@) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (@) values
            brResult = foldResult

        Check.One (config, property)

    [<Test>]
    let ``BalancedReducer with nonassociative operation demonstrates non-equivalence to List reduce`` () =
        let property (v1 : int) (values : int list) =
            let values = v1 :: values
            let br = populateReducer (-) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (-) values
            brResult = foldResult

        property 0 [ 0 ; 0 ; 1 ] |> shouldEqual false
