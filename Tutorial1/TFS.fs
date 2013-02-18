namespace TFS 

open Microsoft.TeamFoundation.Client 
open Microsoft.TeamFoundation.VersionControl.Client 
open Microsoft.TeamFoundation.Build.Client  
open Microsoft.TeamFoundation.TestManagement.Client  
open Microsoft.VisualStudio.Coverage.Analysis
open System.IO

module TFS = 
    type TfsContext = 
        {
            ProjectCollection:TfsTeamProjectCollection
            VersionControlServer:VersionControlServer
            BuildServer:IBuildServer
            TestManagement:ITestManagementService
            TeamProject:TeamProject
            History:seq<Changeset>
        }

    let createContext srcFolder = 
        let tfsUrl = new System.Uri("http://vptfs/tfs/sivp")
        let collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUrl)
        let vc = collection.GetService<VersionControlServer>()
        {
            ProjectCollection=collection
            VersionControlServer=vc
            BuildServer=collection.GetService<IBuildServer>()
            TestManagement=collection.GetService<ITestManagementService>()
            TeamProject=vc.TryGetTeamProject("Front Office 5.0")
            History = vc.QueryHistory(srcFolder,RecursionType.Full) 
                        |> Seq.filter(fun h-> not(h.Committer.Contains("svc_TfsService")) )
                        |> Seq.toList
        }

    let getBuildsLabels = 
        let cache = ref []
        

        fun src ->
            let ctx = 
                createContext src
            match !cache with
                |[] ->
                    let res = 
                        ctx.BuildServer.QueryBuilds("Front Office 5.0", "OrderPipe_R7")
                        |> Seq.sortBy( fun b->b.StartTime)
                        |> Seq.toList
                        |> List.rev
                        |> Seq.map( fun b -> b.BuildNumber)
                        |> Seq.toList
                    cache := res
                    res
                |c-> c


    open System.Text.RegularExpressions
    
    let (|Integer|_|) (str: string) =
       let mutable intvalue = 0
       if System.Int32.TryParse(str, &intvalue) then Some(intvalue)
       else None

    let (|Float|_|) (str: string) =
       let mutable floatvalue = 0.0
       if System.Double.TryParse(str, &floatvalue) then Some(floatvalue)
       else None

    let (|ParseRegex|_|) regex str =
       let m = Regex(regex).Match(str)
       if m.Success
       then Some (List.tail [ for x in m.Groups -> x.Value ])
       else None


    let parseDate str =
       match str with
         | ParseRegex "(\d{1,2})/(\d{1,2})/(\d{1,2})$" [Integer m; Integer d; Integer y]
              -> new System.DateTime(y + 2000, m, d)
         | ParseRegex "(\d{1,2})/(\d{1,2})/(\d{3,4})" [Integer m; Integer d; Integer y]
              -> new System.DateTime(y, m, d)
         | ParseRegex "(\d{1,4})-(\d{1,2})-(\d{1,2})" [Integer y; Integer m; Integer d]
              -> new System.DateTime(y, m, d)
         | _ -> new System.DateTime()

    let dt1 = parseDate "12/22/08" 
    let dt2 = parseDate "1/1/2009" 
    let dt3 = parseDate "2008-1-15" 
    let dt4 = parseDate "1995-12-28"

    printfn "%s %s %s %s" (dt1.ToString()) (dt2.ToString()) (dt3.ToString()) (dt4.ToString())

module Other =
    let myFunc a b = (a+b+1+2+4).ToString()

module Reflect =
    open System.Reflection
    open Microsoft.FSharp.Reflection
    let myFunc a b = (a+b+1+2+4+10+55).ToString()
    let toto = 3
    
    let a = System.Reflection.Assembly.GetExecutingAssembly()
    a.GetTypes()
        |> Seq.map(fun t -> 
                t.GetMethods()
                |> Seq.filter(fun m -> 
                    m.Name = "myFunc" && m.ReturnType = typeof<string>  ))
        |> Seq.concat
        |> Seq.iter(fun m ->printfn "  %A %A %A %A %A" m.DeclaringType.FullName (m.Invoke(null,[|1;2|])) m.Name (m.GetParameters()) m.ReturnType )  
                


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

    type Request = 
        {
            Path:string
            Parms:List<Parm>
        }

    let (|Get|) path parms request =
        if path = request.Path then
            if parms |> Seq.forall( fun p-> request.Parms |> Seq.exists(fun pp -> p = pp)) then
                parms
                |> Seq.map( fun p -> request.Parms |> Seq.find( fun pp -> p = pp ).Value)
        []

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
        
        

