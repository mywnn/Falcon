module Woop

open System
open Plugin

let woop: Plugin = fun msg ->
    state {
        match msg with
        | Regex "woop (\d+)" [woopCount] -> 
            let repeat n = String.Join(" ", Seq.replicate n "woop")
            let n = (Int32.Parse woopCount)
            return reply msg (repeat n)
        | _ -> return List.empty
    }