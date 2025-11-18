namespace WoofWare.BalancedReducer

open System
open System.Collections

(* The [data] array is an implicit binary tree with [children_length * 2 - 1] nodes,
   with each node being the sum of the two child nodes and the root node being the 0th
   node.  The leaves of the tree are the last [num_leaves] nodes.

   The children are not necessarily all at the same level of the tree. For instance if
   you have 3 children [| a; b; c |]:

   {v
          o
         / \
        o   c
       / \
      a   b
   v}

   We want this tree to be representated as [| o; o; c; a; b |], i.e. we need to apply
   first a rotation then a translation to convert an index in [| a; b; c |] to a (leaf)
   index in [| o; o; c; a; b |]. *)
type BalancedReducer<'a> =
    {
        Values : 'a array
        IsSet : BitArray
        NumLeaves : int
        NumLeavesNotInBottomLevel : int
        Reduce : 'a -> 'a -> 'a
    }

[<RequireQualifiedAccess>]
module BalancedReducer =
    let length (t : BalancedReducer<'a>) : int = t.NumLeaves

    (* {v
     parent:      0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 ...
     left child:  1  3  5  7  9 11 13 15 17 19 21 23 25 27 29 31 33 35 37 39 ...
     right child: 2  4  6  8 10 12 14 16 18 20 22 24 26 28 30 32 34 36 38 40 ... v} *)
    let parentIndex (childIndex : int) : int = (childIndex - 1) / 2
    let leftChildIndex (parentIndex : int) : int = (parentIndex * 2) + 1
    let rightChildIndex (leftChildIndex : int) : int = leftChildIndex + 1

    (* The first [num_leaves-1] elements are internal nodes of the tree.  The next
       [num_leaves] elements are the leaves. *)
    let numBranches (t : BalancedReducer<'a>) : int = t.NumLeaves - 1
    let indexIsLeaf (t : BalancedReducer<'a>) (i : int) : bool = i >= numBranches t

    (* The tree is complete, but not necessarily perfect, so we perform some rotation of the
       leaves to ensure that our reductions preserve ordering. *)
    let leafIndex (t : BalancedReducer<'a>) (i : int) : int =
        (* The tree layout is level order.  Any leaves in the second to last level need to occur
         in the array before the leaves in the bottom level. *)
        let rotatedIndex =
            let offsetFromStartOfLeavesInArray = i + t.NumLeavesNotInBottomLevel

            if offsetFromStartOfLeavesInArray < t.NumLeaves then
                offsetFromStartOfLeavesInArray
            else
                offsetFromStartOfLeavesInArray - t.NumLeaves in
        (* The leaves occur after the branches in the array. *)
        rotatedIndex + numBranches t

    let ceilPow2 x =
        if x <= 1 then
            1
        else
            let mutable n = x - 1
            n <- n ||| (n >>> 1)
            n <- n ||| (n >>> 2)
            n <- n ||| (n >>> 4)
            n <- n ||| (n >>> 8)
            n <- n ||| (n >>> 16)
            n + 1

    let create len reduce =
        if len < 1 then
            raise (ArgumentException $"non-positive number of leaves {len} in balanced reducer")

        let numBranches = len - 1
        let numLeavesNotInBottomLevel = ceilPow2 len - len
        let arraySize = numBranches + len
        let values = Array.create arraySize Unchecked.defaultof<'a>
        let isSet = BitArray arraySize

        {
            Values = values
            IsSet = isSet
            NumLeaves = len
            NumLeavesNotInBottomLevel = numLeavesNotInBottomLevel
            Reduce = reduce
        }

    let validateIndex (t : BalancedReducer<'a>) (i : int) : unit =
        if i < 0 then
            raise (IndexOutOfRangeException $"attempt to access negative index %i{i} in balanced reducer")

        let length = t.NumLeaves

        if i >= length then
            raise (IndexOutOfRangeException $"attempt to access out of bounds index %i{i} in balanced reducer")

    let set (reducer : BalancedReducer<'a>) (i : int) (newValue : 'a) : unit =
        validateIndex reducer i
        let values = reducer.Values
        let isSet = reducer.IsSet
        let mutable i = leafIndex reducer i
        values.[i] <- newValue
        isSet.[i] <- true

        while i <> 0 do
            let parent = parentIndex i

            if not isSet.[parent] then
                i <- 0
            else
                isSet.[parent] <- false
                i <- parent

    let get (t : BalancedReducer<'a>) (i : int) : 'a =
        validateIndex t i
        let leafIdx = leafIndex t i

        if not t.IsSet.[leafIdx] then
            raise (InvalidOperationException $"no value was ever set at index %i{i} for get operation")

        t.Values.[leafIdx]

    let rec compute' (t : BalancedReducer<'a>) (i : int) : 'a =
        if i < 0 || i >= t.Values.Length then
            raise (IndexOutOfRangeException $"index %i{i} out of bounds")

        if t.IsSet.[i] then
            t.Values.[i]
        else
            let left = leftChildIndex i
            let right = rightChildIndex left

            if left >= t.Values.Length then
                raise (InvalidOperationException "attempted to compute balanced reducer with unset elements")

            let a = t.Reduce (compute' t left) (compute' t right)
            t.Values.[i] <- a
            t.IsSet.[i] <- true
            a

    let compute (t : BalancedReducer<'a>) = compute' t 0

    let invariant t =
        let isSet = t.IsSet

        for i = 0 to numBranches t - 1 do
            let left = leftChildIndex i
            let right = rightChildIndex left
            let leftIsNotSet = not isSet.[left]
            let rightIsNotSet = not isSet.[right]

            if isSet.[i] then
                assert (not (leftIsNotSet || rightIsNotSet))
            else
                assert (indexIsLeaf t left || indexIsLeaf t right || leftIsNotSet || rightIsNotSet)
