module Airline

open Plugin
open Json

let private formatAirline (airline : string) (hubs : seq<string>) : string =
    let hubCodes = hubs |> String.concat ", "
    sprintf "%s (Hubs: %s)" airline hubCodes

let private findAirlineByCode airlineIataCode : string option =
    let response = Wikidata.query "sparql/airlines-by-code-request.sparql" (Map.ofList ["{{airline_code}}", airlineIataCode])
    match AirlineByCodeResponse.Parse(response).Results.Bindings with
        | [||] -> Option.None
        | bindings -> bindings
                        |> Seq.groupBy (fun binding -> binding.Name.Value)
                        |> Seq.map (fun (airline, hubs) -> formatAirline airline (hubs |> Seq.map (fun hub -> hub.HubAirportCode.Value)))
                        |> String.concat "; "
                        |> Option.Some

let private findAirlineByName airlineName : string option =
    let response = Wikidata.query "sparql/airlines-by-name-request.sparql" (Map.ofList ["{{airline_name}}", airlineName])
    match AirlineByNameResponse.Parse(response).Results.Bindings with
    | [||] -> Option.None
    | bindings -> bindings
                    |> Seq.groupBy (fun binding -> binding.Name.Value)
                    |> Seq.map (fun (airline, hubs) -> formatAirline airline (hubs |> Seq.map (fun hub -> hub.HubAirportCode.Value)))
                    |> String.concat "; "
                    |> Option.Some

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
