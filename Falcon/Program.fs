open System
open System.IO
open FSharp.Configuration
open FSharp.Control
open Json
open Plugin
open WebSocket
open FSharp.Data


let plugins : list<Plugin> = [Woop.woop; Airline.airline]


// authenticate to the slack api, and receive a payload of connection info
let authenticate apiToken =
    let endpoint = sprintf "https://slack.com/api/rtm.start?no_unreads=1&simple_latest=1&token=%s" apiToken
    Json.RtmStart.Load endpoint

let constructInitialState (msg: RtmStart.Root) =
    {
        Users = msg.Users
                |> Seq.map (fun u -> (u.Id, u.Name))
                |> Map.ofSeq
        Channels = msg.Channels
                |> Seq.map (fun u -> (u.Id, u.Name))
                |> Map.ofSeq
        Responses = []
    }

/// do a little bit of parsing of the json to determine what type of message we received
let extractResponseType (messageJson : string) : string option = 
    let separators = [| '"'; ':'; ',' |]
    match messageJson.Split(separators, 4, StringSplitOptions.RemoveEmptyEntries) with
        | [| "{"; "type"; responseType; _ |] -> Some responseType
        | [| "{"; "ok"; "true"; _ |] -> Some "ack"
        | _ -> None

let handleNonMessage (responseType : string option, messageJson: string) =
    match responseType with
    | Some "hello" | Some "ack" -> ()
    | Some unknown ->
        printf "haven't learned %s\n" unknown
    | None -> 
        File.WriteAllText(Guid.NewGuid().ToString(), messageJson)
        printf "parsing failure!\n"
/// given the bot's existing state and a new message from slack, react to the message and optionally update the state
let getActions (messageJson : string) =
    state {
        let! currentState = State.get()
        printf "received %s\n" messageJson
        let responseType = extractResponseType messageJson
        match responseType with
        | Some "message" -> 
            let message = IncomingMessage.Parse(messageJson)
            let! actions = plugins |> Seq.map(fun plugin -> plugin message) |> seqStateToStateSeq
            let actions = actions |> Seq.collect(fun x -> x) |> Seq.toList
            do! State.set { currentState with Responses = List.append (currentState.Responses) actions }
            return actions
        | _ -> 
            handleNonMessage(responseType, messageJson)
            return List.empty
    }

let botActionToWebSocketCommand response = 
    match response with
        | BotAction.Disconnect -> WebSocketCommand.Close
        | BotAction.Say(channel, msg) -> 
            let jsonResponse = (new OutgoingMessage.Root("message", channel, msg, 1)).JsonValue.ToString()
            WebSocketCommand.Message(jsonResponse)

/// take any actions the bot wants to take (e.g. queued messages) and send them to the websocket
let sendStateToWebSocket ws actionList =
    async {
        let commands = actionList |> Seq.map botActionToWebSocketCommand
        for command in commands do
            do! WebSocket.send ws command
    }
type Settings = AppSettings<"App.config">
[<EntryPoint>]
let main argv = 
    let remoteState = authenticate Settings.SlackApiToken
    let initialState = constructInitialState remoteState
    let ws = WebSocket.create()
    // fold over the messages from the websocket connection, threading the bot's state through each successive call
    WebSocket.read ws remoteState.Url
        |> AsyncSeq.foldAsync(fun state msg -> 
            async {
                let actions, state = getActions msg state
                do! sendStateToWebSocket ws actions
                return state
            }
        ) initialState
        |> Async.RunSynchronously
        |> ignore
    printf "done"
    Console.ReadKey() |> ignore
    0