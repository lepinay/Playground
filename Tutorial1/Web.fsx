module Web =
    open System.Text.RegularExpressions
    open System.Reflection
    open Microsoft.FSharp.Reflection
    open System.Web
    open System.Net

    type Parm = 
        {
            Name:string
            Value:string
        }

    type Method =
        |Get
        |Post

    type Request = 
        {
            Path:string
            Method:Method
            Parms:List<Parm>
        }

    let (|Get|_|) path parms request:List<string> option =
        let getValue (parm:Parm option) =
            match parm with
                |Some(p) -> Some(p.Value)
                |None -> None
            
        match (request.Method,path) with
            |(Post,_) -> None
            |(Get,path) when path = request.Path -> 
                parms
                    |> Seq.map( fun p -> Seq.tryFind( fun pp -> p = pp.Name) |> getValue )
            | _ -> None

    let (|Post|) path parms request =
        []

    let view id = 
        printfn "GET %A" id
        id.ToString()

    let post id name =
        printfn "Posted: %A %A" id name
        ""

    let index =
        printfn "GET"
        "index"

    let handleRequest request = 
        match request with
            | Get "/home/index" [] [] -> index
            | Get "/home/view" ["id"] [id] -> view id
            | Post "/home/post" ["id","name"] [id;name] -> post id name
