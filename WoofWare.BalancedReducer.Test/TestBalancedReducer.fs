namespace WoofWare.BalancedReducer.Test

open System.Text.Json
open FsUnitTyped
open NUnit.Framework
open WoofWare.Expect
open WoofWare.BalancedReducer

[<TestFixture>]
module TestBalancedReducer =
    let reduce (a1 : 'a list) (a2 : 'a list) = a1 @ a2

    type Messages =
        {
            Freeze : unit -> string list
            Clear : unit -> unit
        }

    let create<'a> (len : int) : 'a list BalancedReducer * Messages =
        let messages = ResizeArray ()

        let t =
            BalancedReducer.create
                len
                (fun a b ->
                    messages.Add $"reduce {a} {b}"
                    reduce a b
                )

        BalancedReducer.invariant t

        let messages =
            {
                Freeze = fun () -> Seq.toList messages
                Clear = fun () -> messages.Clear ()
            }

        t, messages

    let set (t : BalancedReducer<'a list>) (i : int) (v : 'a) : unit =
        BalancedReducer.set t i [ v ]
        BalancedReducer.invariant t

    let compute (t : BalancedReducer<'a>) : 'a =
        let result = BalancedReducer.compute t
        BalancedReducer.invariant t
        result

    [<Test>]
    let ``create with invalid length`` () =
        expect {
            snapshotThrows @"System.ArgumentException: non-positive number of leaves 0 in balanced reducer"
            return! fun () -> create<int> 0
        }

    [<Test>]
    let ``[set] with invalid index`` () =
        let t, _ = create 1

        expect {
            snapshotThrows @"System.IndexOutOfRangeException: attempt to access negative index -1 in balanced reducer"
            return! fun () -> set t -1 13
        }

        expect {
            snapshotThrows
                @"System.IndexOutOfRangeException: attempt to access out of bounds index 1 in balanced reducer"

            return! fun () -> set t 1 13
        }

    [<Test>]
    let ``get test`` () =
        let t, _ = create 1

        expect {
            snapshotThrows @"System.InvalidOperationException: no value was ever set at index 0 for get operation"
            return! fun () -> BalancedReducer.get t 0
        }

        set t 0 5

        expect {
            snapshot @"[5]"
            return BalancedReducer.get t 0
        }

        expect {
            snapshotThrows @"System.IndexOutOfRangeException: attempt to access negative index -1 in balanced reducer"
            return! fun () -> BalancedReducer.get t -1
        }

        expect {
            snapshotThrows
                @"System.IndexOutOfRangeException: attempt to access out of bounds index 2 in balanced reducer"

            return! fun () -> BalancedReducer.get t 2
        }

    [<Test>]
    let ``compute with a None`` () =
        let t, _ = create<int> 1

        expect {
            snapshotThrows
                @"System.InvalidOperationException: attempted to compute balanced reducer with unset elements"

            return! fun () -> compute t
        }

    [<Test>]
    let ``compute with a None and a Some`` () =
        let t, _ = create 2
        set t 0 13

        expect {
            snapshotThrows
                @"System.InvalidOperationException: attempted to compute balanced reducer with unset elements"

            return! fun () -> compute t
        }

    [<Test>]
    let ``compute test`` () =
        let t, _ = create 1
        set t 0 13

        expect {
            snapshot @"[13]"
            return compute t
        }

    [<Test>]
    let ``compute caches reduce`` () =
        let t, messages = create 2
        set t 0 13
        set t 1 14

        expect {
            snapshot @"[13; 14]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [13] [14]""
     ]"

            return messages.Freeze ()
        }

        expect {
            snapshot @"[13; 14]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [13] [14]""
     ]"

            return messages.Freeze ()
        }

    [<Test>]
    let ``compute recomputes when input changes`` () =
        let t, messages = create 2
        set t 0 13
        set t 1 14

        expect {
            snapshot @"[13; 14]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [13] [14]""
     ]"

            return messages.Freeze ()
        }

        set t 1 15

        expect {
            snapshot @"[13; 15]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [13] [14]"",
       ""reduce [13] [15]""
     ]"

            return messages.Freeze ()
        }

    [<Test>]
    let ``compute only recomputes what's necessary`` () =
        let t, messages = create 3
        set t 0 13
        set t 1 14
        set t 2 15

        expect {
            snapshot @"[13; 14; 15]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [13] [14]"",
       ""reduce [13; 14] [15]""
     ]"

            return messages.Freeze ()
        }

        messages.Clear ()
        set t 2 16

        expect {
            snapshot @"[13; 14; 16]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [13; 14] [16]""
     ]"

            return messages.Freeze ()
        }

    [<Test>]
    let ``compute only recomputes what's necessary, larger`` () =
        let t, messages = create 10

        for i = 0 to 9 do
            set t i (i + 13)

        expect {
            withJsonSerializerOptions (JsonSerializerOptions ())
            snapshotJson @"[13,14,15,16,17,18,19,20,21,22]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [13] [14]"",
       ""reduce [15] [16]"",
       ""reduce [13; 14] [15; 16]"",
       ""reduce [17] [18]"",
       ""reduce [13; 14; 15; ... ] [17; 18]"",
       ""reduce [19] [20]"",
       ""reduce [21] [22]"",
       ""reduce [19; 20] [21; 22]"",
       ""reduce [13; 14; 15; ... ] [19; 20; 21; ... ]""
     ]"

            return messages.Freeze ()
        }

        messages.Clear ()
        set t 9 23

        expect {
            withJsonSerializerOptions (JsonSerializerOptions ())
            snapshotJson @"[13,14,15,16,17,18,19,20,21,23]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [21] [23]"",
       ""reduce [19; 20] [21; 23]"",
       ""reduce [13; 14; 15; ... ] [19; 20; 21; ... ]""
     ]"

            return messages.Freeze ()
        }

        messages.Clear ()
        set t 0 12
        set t 9 24

        expect {
            withJsonSerializerOptions (JsonSerializerOptions ())
            snapshotJson @"[12,14,15,16,17,18,19,20,21,24]"
            return compute t
        }

        expect {
            snapshotJson
                @"[
       ""reduce [12] [14]"",
       ""reduce [12; 14] [15; 16]"",
       ""reduce [12; 14; 15; ... ] [17; 18]"",
       ""reduce [21] [24]"",
       ""reduce [19; 20] [21; 24]"",
       ""reduce [12; 14; 15; ... ] [19; 20; 21; ... ]""
     ]"

            return messages.Freeze ()
        }

    [<Test>]
    let ``different lengths`` () =
        let results = ResizeArray ()

        for len = 1 to 10 do
            let t, _ = create len

            for i = 0 to len - 1 do
                set t i i

            results.Add (compute t)

            for i = 0 to len - 1 do
                set t i i

            for i = 0 to len - 1 do
                set t i (len - 1 - i)

            results.Add (compute t)

        results.ToArray ()
        |> Array.chunkBySize 2
        |> Array.iteri (fun i v ->
            match v with
            | [| forward ; backward |] ->
                forward |> shouldEqual [ 0 .. forward.Length - 1 ]
                forward.Length |> shouldEqual (i + 1)
                backward |> shouldEqual (List.rev forward)
            | _ -> failwith "oh no"
        )
