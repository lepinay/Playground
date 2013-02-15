namespace TFS
 
open Microsoft.TeamFoundation.Client 
open Microsoft.TeamFoundation.VersionControl.Client 
open Microsoft.TeamFoundation.Build.Client  
open Microsoft.TeamFoundation.TestManagement.Client  
open Microsoft.VisualStudio.Coverage.Analysis
open System.IO

module SuivitLivraisons =
    open TFS.TFS
    
    let qualifContext = createContext "$/Front Office 5.0/1.Front/OrderPipe/Qualif"

    let mergeHistory = 
        qualifContext.VersionControlServer.QueryMerges(null, null, "$/Front Office 5.0/1.Front/OrderPipe/Qualif", LatestVersionSpec.Latest, null, null, RecursionType.Full )
        |> Seq.groupBy(fun m -> m.TargetVersion )
        |> Seq.map(fun (target, source) -> 
                (target, source |> Seq.map(fun s -> qualifContext.VersionControlServer.GetChangeset(s.SourceVersion))))

    // Livré toujours en resolved
    let latestLivraison = 
        qualifContext.BuildServer.QueryBuilds("Front Office 5.0", "OrderPipe_R7")
        |> Seq.find(fun b-> b.BuildNumber = "OrderPipe_R7_v1.0.0.304.02")

    let getMergedWorkItems id = 
        let mergedChangesets = 
            mergeHistory 
            |> Seq.tryFind(fun (k,v)->k = id)
        match mergedChangesets with
        | Some m ->
                    let (k:int),(v:seq<Changeset>)=m
                    v 
                    |> Seq.map( fun c -> c.AssociatedWorkItems) 
                    |> Seq.concat
        | None -> Seq.empty

    let report = 
        qualifContext.History
        |> Seq.map( fun h -> 
            if h.ChangesetId <= System.Convert.ToInt32(latestLivraison.SourceGetVersion.Replace("C",""))
                then 
                    let allWorkItems = h.AssociatedWorkItems |> Seq.append (getMergedWorkItems h.ChangesetId)
                    allWorkItems |> Seq.filter(fun w->w.State = "Resolved")
                else Seq.empty
                  )
        |> Seq.concat
        |> Seq.distinctBy( fun h-> h.Id)
        |> Seq.map( fun h-> h.Id.ToString() + " "+ h.Title )
    
