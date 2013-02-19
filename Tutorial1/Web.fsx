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

    let (|Get|_|) path parms request :List<string> option =
    
        let tryFindValue p =
            match request.Parms |> Seq.tryFind( fun pp -> pp.Name = p ) with
                |Some(v) -> Some(v.Value)
                |_ -> None
            
        let checkForNones l = 
            printfn "check noness %A" l
            if l |> List.forall(fun p -> Option.isSome(p) ) then
                Some(l |> List.map( fun p-> p.Value))
            else None
            
        match (request.Method,path) with
            |(Post,_) -> None
            |(Get,path) when path = request.Path -> 
                printfn "check parms %A " parms
                parms
                    |> List.map( fun p -> tryFindValue p  )
                    |> checkForNones
            | _ -> None

    let (|Post|_|) path parms request :List<string> option =
        let tryFindValue p =
            match request.Parms |> Seq.tryFind( fun pp -> pp.Value = p ) with
                |Some(v) -> Some(v.Value)
                |_ -> None
            
        let checkForNones l = 
            if l |> List.forall(fun p -> Option.isSome(p) ) then
                Some(l |> List.map( fun p-> p.Value))
            else None
            
        match (request.Method,path) with
            |(Method.Get,_) -> None
            |(Method.Post,path) when path = request.Path -> 
                parms
                    |> List.map( fun p -> tryFindValue p  )
                    |> checkForNones
            | _ -> None

    let view id = 
        printfn "GET %A" id
        id.ToString()

    let viewNamed id name = 
        printfn "GET %A %A" id name
        id.ToString() + name

    let post id name =
        printfn "Posted: %A %A" id name
        "Post view"

    let index =
        "index view"

    let postEmpty =
        "Post empty"

    let handleRequest request = 
        match request with
            | Post "/home/post" ["id";"name"] [id;name] -> post id name
            | Post "/home/post" [] [] -> postEmpty
            | Get "/home/index" [] [] -> index
            | Get "/home/view" ["id";"name"] [id;name] -> viewNamed id name
            | Get "/home/view" ["id"] [id] -> view id
            | _ -> failwith (sprintf "not handled %A"  request)

    handleRequest {Path = "/home/view";Method=Get;Parms=[{Name="id";Value="10"};{Name="name";Value="toto"}]}
    handleRequest {Path = "/home/view";Method=Get;Parms=[{Name="id";Value="10"}]}
    handleRequest {Path = "/home/post";Method=Post;Parms=[]}
    handleRequest {Path = "/home/post";Method=Post;Parms=[{Name="id";Value="10"};{Name="name";Value="toto"}]}