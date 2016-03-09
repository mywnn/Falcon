[<AutoOpen>]
module NatTrans
    /// <summary>
    /// A natural transformation to map seq<State<'x, _>> to State<seq<'x>, _>
    /// </summary>
    let seqStateToStateSeq (xs: seq<State<'x, 's>>): State<seq<'x>, 's> =
        fun state ->
            xs
            |> Seq.fold(fun (xs, state) m ->
                let x, state = m state
                (Seq.append xs [x]), state
            ) (Seq.empty, state)
