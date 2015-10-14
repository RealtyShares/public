﻿open System
open FSharp.Data
open FsUnit
open NUnit.Framework
open Newtonsoft.Json

let port = 8000

type AddRequest = {
    a : int
    b : int
}

type SumResponse = {
    sum : int
}

type ErrorResponse = {
    error : string
}

[<Test>]
let ``Adds correctly.``() =
    let response = Http.RequestString((sprintf "http://localhost:%d/add" port),
                                       httpMethod = "POST",
                                       body = TextRequest """{"a":1,"b":2}""")
    let parsed_response = try (Some (JsonConvert.DeserializeObject<SumResponse> response))
                          with :? JsonException -> None
    match parsed_response with
    | Some value -> 3 |> should equal <| value.sum
    | None -> failwith "Should sum 1 and 2."

[<Test>]
[<ExpectedException>]
let ``Requires JSON body.``() =
    should throw typeof<System.Net.WebException>
    <| Http.RequestString((sprintf "http://localhost:%d/add" port),
                          httpMethod = "POST",
                          body = TextRequest "a=1&b=2")

[<Test>]
let ``Explains it requires JSON body.``() =
    let response = Http.RequestString((sprintf "http://localhost:%d/add" port),
                                      httpMethod = "POST",
                                      body = TextRequest "a=1&b=2",
                                      silentHttpErrors = true)
    let parsed_response = try (Some (JsonConvert.DeserializeObject<ErrorResponse> response))
                          with :? JsonException -> None
    match parsed_response with
    | Some value -> "Need JSON body." |> should equal <| value.error
    | None -> failwith "Should return an error."

[<Test>]
[<ExpectedException>]
let ``Requires POST.``() =
    should throw typeof<System.Net.WebException>
    <| Http.RequestString (sprintf "http://localhost:%d/add" port)

[<Test>]
let ``Explains it requires POST.``() =
    let response = Http.RequestString((sprintf "http://localhost:%d/add" port),
                                      silentHttpErrors = true)
    let parsed_response = try (Some (JsonConvert.DeserializeObject<ErrorResponse> response))
                          with :? JsonException -> None
    match parsed_response with
    | Some value -> "Need POST request." |> should equal <| value.error
    | None -> failwith "Should return an error."

[<Test>]
[<ExpectedException>]
let ``Fails as resource is not found.``() =
    should throw typeof<System.Net.WebException>
    <| Http.RequestString((sprintf "http://localhost:%d/404" port),
                          httpMethod = "POST",
                          body = TextRequest "{}")

[<Test>]
let ``Explains the resource is not found.``() =
    let response = Http.RequestString((sprintf "http://localhost:%d/404" port),
                                      httpMethod = "POST",
                                      body = TextRequest "{}",
                                      silentHttpErrors = true)
    let parsed_response = try (Some (JsonConvert.DeserializeObject<ErrorResponse> response))
                          with :? JsonException -> None
    match parsed_response with
    | Some value -> "Not found." |> should equal <| value.error
    | None -> failwith "Should return an error."

let phpAdd a b =
    printfn "Adding %d and %d using the PHP server." a b
    let response = (Http.RequestString((sprintf "http://localhost:%d/add" port),
                                       httpMethod = "POST",
                                       body = (TextRequest (JsonConvert.SerializeObject({ a =a ; b = b })))))
    try (Some (JsonConvert.DeserializeObject<SumResponse> response))
    with :? JsonException -> None

[<EntryPoint>]
let main argv =
    match phpAdd 2 2 with
    | Some v -> printfn "OK: %d" v.sum
    | None -> printfn "Error."
    0
