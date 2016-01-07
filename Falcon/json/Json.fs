module Json

open FSharp.Data

// type providers for each json message to/from slack
type RtmStart = JsonProvider<"json/rtm.start.json">
type Hello = JsonProvider<"json/hello.json">
type IncomingMessage = JsonProvider<"json/incoming-message.json">
type OutgoingMessage = JsonProvider<"json/outgoing-message.json">

// type providers for each json response from wikidata
type AirlineByCodeResponse = JsonProvider<"json/airlines-by-code-response.json">
type AirlineByNameResponse = JsonProvider<"json/airlines-by-name-response.json">
