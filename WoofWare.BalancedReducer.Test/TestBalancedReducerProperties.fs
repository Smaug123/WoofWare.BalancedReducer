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

    /// Property: For associative operations, BalancedReducer.compute should equal List.reduce
    [<Test>]
    let ``BalancedReducer with addition equals List.reduce`` () =
        let property (v1 : int) (values : int list) =
            let values = v1 :: values
            let br = populateReducer (+) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (+) values
            brResult = foldResult

        Check.One (config, property)

    [<Test>]
    let ``BalancedReducer with multiplication equals List.reduce`` () =
        let property (v1 : int8) (values : int8 list) =
            let values = v1 :: values
            // Use int8 to keep test cases small; both sides overflow identically
            let br = populateReducer (*) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (*) values
            brResult = foldResult

        Check.One (config, property)

    [<Test>]
    let ``BalancedReducer with string concatenation equals List.reduce`` () =
        let property (v1 : string) (values : string list) =
            let values = v1 :: values
            let br = populateReducer (+) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (+) values
            brResult = foldResult

        Check.One (config, property)

    [<Test>]
    let ``BalancedReducer with list concatenation equals List.reduce`` () =
        let property (v1 : int list) (values : int list list) =
            let values = v1 :: values
            let br = populateReducer (@) values
            let brResult = BalancedReducer.compute br
            let foldResult = List.reduce (@) values
            brResult = foldResult

        Check.One (config, property)

    /// Counts leaves in a subtree of a complete binary tree stored in level order.
    /// numBranches is the number of internal nodes (n-1 for n leaves).
    /// nodeIndex is the root of the subtree to count.
    let rec countLeavesInSubtree (numBranches : int) (nodeIndex : int) : int =
        if nodeIndex >= numBranches then
            1 // This node is a leaf
        else
            let left = 2 * nodeIndex + 1
            let right = left + 1
            countLeavesInSubtree numBranches left + countLeavesInSubtree numBranches right

    /// Computes the number of leaves in the left subtree of the root for a
    /// complete binary tree with n leaves stored in level order.
    let leftSubtreeSize n =
        if n <= 1 then 0 else countLeavesInSubtree (n - 1) 1

    /// Reference tree-fold that matches the balanced reducer's tree structure.
    /// The balanced reducer uses a complete binary tree stored in level order,
    /// so the split at each level is determined by the tree structure.
    let rec treeFold (reduce : 'a -> 'a -> 'a) (values : 'a list) : 'a =
        match values with
        | [] -> failwith "treeFold called with empty list"
        | [ x ] -> x
        | _ ->
            let n = List.length values
            let leftSize = leftSubtreeSize n
            let left, right = List.splitAt leftSize values
            reduce (treeFold reduce left) (treeFold reduce right)

    [<Test>]
    let ``BalancedReducer with nonassociative operation matches tree-fold`` () =
        // For non-associative operations, the balanced reducer should match a tree-fold
        // (not a left-fold like List.reduce). This tests that leaf ordering is correct.
        let property (v1 : int) (values : int list) =
            let values = v1 :: values
            let br = populateReducer (-) values
            let brResult = BalancedReducer.compute br
            let treeFoldResult = treeFold (-) values
            brResult = treeFoldResult

        Check.One (config, property)

    [<Test>]
    let ``BalancedReducer tree-fold differs from left-fold for non-associative operations`` () =
        // Verify that tree-fold and left-fold actually give different results for subtraction
        // on many inputs (demonstrating the importance of the tree structure).
        let differingCount =
            [ 1..1000 ]
            |> List.filter (fun seed ->
                let rng = System.Random seed
                let len = rng.Next (3, 20)
                let values = List.init len (fun _ -> rng.Next (-100, 100))

                let treeFoldResult = treeFold (-) values
                let leftFoldResult = List.reduce (-) values
                treeFoldResult <> leftFoldResult
            )
            |> List.length

        // Most random inputs should give different results
        differingCount |> shouldBeGreaterThan 900
