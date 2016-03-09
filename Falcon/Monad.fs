[<AutoOpen>]
module Monad
    type State<'x, 's> = 's -> 'x * 's 
    type StateMonad() =
        member this.Bind(x, f) = fun s -> x s ||> f
        member this.Return x = fun s -> x, s
        member this.ReturnFrom x = x
        member this.Delay f = fun s -> f() s
        member this.Zero() = fun s -> (), s
    let state = new StateMonad()

    [<RequireQualifiedAccess>]
    module State =
        let get(): State<'s, 's> = fun s -> s, s
        let set s: State<unit, 's> = fun _ -> (), s

