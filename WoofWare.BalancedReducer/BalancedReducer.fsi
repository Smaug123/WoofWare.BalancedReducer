namespace WoofWare.BalancedReducer

/// Stores a mutable fixed-length sequence of optional values, and incrementally maintains the result of folding an
/// associative operation over the sequence as its elements change.
type BalancedReducer<'a>

/// Stores a mutable fixed-length sequence of optional values, and incrementally maintains the result of folding an
/// associative operation over the sequence as its elements change.
[<RequireQualifiedAccess>]
module BalancedReducer =
    /// <summary>
    /// Creates a balanced reducer of length <c>len</c>, all of whose elements are initialised to <c>None</c>.
    ///
    /// To use this reducer, call <c>set</c> repeatedly to assign a value to every index, and then call
    /// <c>compute</c>.
    /// </summary>
    /// <exception cref="ArgumentException">Throws if <c>len</c> is less than <c>1</c> or greater than <c>2^30</c>.</exception>
    val create : len : int -> reduce : ('a -> 'a -> 'a) -> BalancedReducer<'a>

    /// <summary>
    /// Updates the value at index <c>i</c> to <c>newValue</c>.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">Throws if <c>i</c> is out of bounds for the underlying array.</exception>
    val set : reducer : BalancedReducer<'a> -> i : int -> newValue : 'a -> unit

    /// <summary>
    /// Gets the value at index <c>i</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Throws if there was never a value set at index <c>i</c>.</exception>
    /// <exception cref="IndexOutOfRangeException">Throws if <c>i</c> is out of bounds for the underlying array.</exception>
    val get : BalancedReducer<'a> -> i : int -> 'a

    /// <summary>
    /// Compute the value of the fold which the reducer is expressing.
    ///
    /// If this throws due to unset elements, some internal caches may have been populated for subtrees
    /// that were successfully computed before the error. These cached values remain valid and will be
    /// properly invalidated by subsequent <c>set</c> calls.
    /// </summary>
    /// <exception cref="InvalidOperationException">Throws if any values of the underlying array are None.</exception>
    val compute : BalancedReducer<'a> -> 'a

    /// <summary>
    /// Check expected invariants hold of the data structure.
    /// You should not call this, but we make it available for the tests.
    /// </summary>
    val invariant : BalancedReducer<'a> -> unit
