module Wikidata

open System
open System.Text
open System.IO
open System.Runtime.Caching
open FSharp.Data

// https://query.wikidata.org/
let private wikidataEndpoint = "https://query.wikidata.org/sparql"

let private cache = new MemoryCache("Wikidata")

let private replaceTemplateValues (template : string) (replacements : Map<string, string>) : string =
    let queryTemplate = new StringBuilder(template)
    let replacer (builder:StringBuilder) (key:string) (value:string) = builder.Replace(key, value)
    let replaced = Map.fold replacer queryTemplate replacements
    replaced.ToString()
    
let query (wikidataQueryFile : string) (replacements : Map<string, string>) : string =
    let queryTemplate = File.ReadAllText(wikidataQueryFile)
    let query = replaceTemplateValues queryTemplate replacements
    match cache.Get(query) with
        | null -> 
            let httpResponse = Http.RequestString(wikidataEndpoint, [ "format", "json"; "query", query], 
                                    customizeHttpRequest = fun req -> req.RequestUri.Query.Replace("%20", "+") |> ignore; req)
            cache.Set(query, httpResponse, DateTimeOffset.Now.AddDays(float 7)) |> ignore
            httpResponse
        | response -> 
            response :?> string
