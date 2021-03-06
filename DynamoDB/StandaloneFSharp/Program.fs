﻿open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.DocumentModel
open Amazon.DynamoDBv2.Model
open Amazon.Runtime
open RealtyShares
open RealtyShares.DynamoDb
open RealtyShares.DynamoEventStore

[<EntryPoint>]
let main argv = 
    // Use credentials from environmental variables, and "N. California",
    // which is `Amazon.RegionEndpoint.USWest1`, is our default. -- D.K.
    let credentials = new EnvironmentVariablesAWSCredentials()
    let client = new AmazonDynamoDBClient(credentials, Amazon.RegionEndpoint.USWest1)
    let tableName = "StandaloneFSharp-demo-table"

    async {
        printfn "Creating table, %s..." tableName
        let! res = createTable client tableName
        let! status = waitForStatus client tableName TableStatus.ACTIVE
        printfn "Table created!"

        let table = Table.LoadTable(client, tableName)

        let sw = new System.Diagnostics.Stopwatch()
        sw.Start()
    
        printfn "inserting %d entries..." 1000
        try 
            async {
                let inserts = 
                    [1..1000]
                    |> List.mapi (fun index version -> save table index version (sprintf "version %d" version))
                for entry in inserts do 
                    let! _ = entry
                    ()
            } |> Async.RunSynchronously |> ignore
        with e -> printfn "error: %s" (string e)

        sw.Stop()

        let writeSpeed = 1000.0 / sw.Elapsed.TotalMilliseconds * 1000.00
        printfn "wrote %d entries at %f writes/sec" 1000 writeSpeed

        let sw2 = new System.Diagnostics.Stopwatch()
        sw2.Start()
    
        load table "0"  
        |> Async.RunSynchronously
        |> (fun events -> printfn "read %d events" events.Length)

        sw2.Stop()

        let readSpeed = 1000.0 / sw2.Elapsed.TotalMilliseconds * 1000.00
        printfn "read %d entries at %f reads/sec" 1000 readSpeed

        printfn "Deleting table, %s..." tableName
        let! del = deleteTable client tableName
        //let! status = DynamoDb.waitForStatus tableName TableStatus.
        
        printfn "Table deleted!"
    } |> Async.RunSynchronously

    0 // return an integer exit code
