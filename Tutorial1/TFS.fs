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


                



        
        

