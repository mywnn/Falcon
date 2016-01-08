module Airline

open Plugin
open Json

/// format airline json responses. Since type providers are used that return identical, but unrelated object hierarchies, we need structural typing
module AirlineFormat =
    let inline private getName binding : string =
        let code = (^t: (member Name: 'u) binding)
        let value = (^u: (member Value: string) code)
        value

    let inline private getHub binding : string =
        let code = (^t: (member HubAirportCode: 'u) binding)
        let value = (^u: (member Value: string) code)
        value

    let inline private formatAirline (airlineName : string, hubs) : string =
        let hubCodes = hubs |> Seq.map getHub |> String.concat ", "
        sprintf "%s (Hubs: %s)" airlineName hubCodes

    let inline format bindings : string =
        bindings
            |> Seq.groupBy getName
            |> Seq.map formatAirline
            |> String.concat "; "

let private findAirlineByCode airlineIataCode : string option =
    let response = Wikidata.query "sparql/airlines-by-code-request.sparql" (Map.ofList ["{{airline_code}}", airlineIataCode])
    match AirlineByCodeResponse.Parse(response).Results.Bindings with
        | [||] -> Option.None
        | bindings -> Some (AirlineFormat.format bindings)

let private findAirlineByName airlineName : string option =
    let response = Wikidata.query "sparql/airlines-by-name-request.sparql" (Map.ofList ["{{airline_name}}", airlineName])
    match AirlineByNameResponse.Parse(response).Results.Bindings with
    | [||] -> Option.None
    | bindings -> Some (AirlineFormat.format bindings)

let airline : Plugin = fun msg state ->
    match msg with
    | Regex "airline ([a-zA-Z0-9]{2})$" [airlineIataCode] -> 
        let airline = findAirlineByCode airlineIataCode
        let answer = match airline with
                        | None -> "I couldn't find an airline by that code."
                        | Some a -> a
        reply state msg answer
    | Regex "airline (.{3,})$" [airlineName] -> 
        let airline = findAirlineByName airlineName
        let answer = match airline with
                        | None -> "I couldn't find an airline matching that name."
                        | Some a -> a
        reply state msg answer
    | _ -> state
