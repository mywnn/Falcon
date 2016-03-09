[<AutoOpen>]
module NatTrans
    /// <summary>
    /// A natural transformation to map seq<State<'x, _>> to State<seq<'x>, _>
    /// </summary>
    val seqStateToStateSeq: seq<State<'x, 's>> -> State<seq<'x>, 's>
