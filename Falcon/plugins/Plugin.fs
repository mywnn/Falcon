[<AutoOpen>]
module Plugin

open System
open System.Text.RegularExpressions
open System.Collections.Generic
open Json

type BotAction = 
    | Say of channel : string * message : string
    | Disconnect

type Name = string
type Identifier = string
type State = {
    Users : Map<Identifier, Name>
    Channels : Map<Identifier, Name>
    Responses : list<BotAction>
}

// a plugin takes an incoming message and an existing bot state. Produces a new state.
type Plugin = IncomingMessage.Root -> State -> State

/// regex active pattern
let (|Regex|_|) (pattern:string) (input:IncomingMessage.Root) =
    let m = Regex.Match(input.Text, pattern, RegexOptions.Compiled)
    if m.Success then 
        Some ([for x in m.Groups -> x.Value] |> List.tail)
    else 
        None

/// returns a new state that will send the provided text to the provided channel
let say state channel text =
    {state with Responses = BotAction.Say(channel, text) :: state.Responses}

/// returns a new state that will reply to the provided message
let reply state (message : IncomingMessage.Root) text =
    say state message.Channel text
