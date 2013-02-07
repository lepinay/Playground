#load "references.fsx"
#load "TFS.fs"

open Microsoft.TeamFoundation.Client 
open Microsoft.TeamFoundation.VersionControl.Client 
open Microsoft.TeamFoundation.Build.Client  
open Microsoft.TeamFoundation.TestManagement.Client  
open Microsoft.VisualStudio.Coverage.Analysis
open System.IO

module SuivitLivraisons =
    open TFS.TFS
    
    let qualifContext = createContext "$/Front Office 5.0/1.Front/OrderPipe/Qualif"

    let c = qualifContext.History
            |> Seq.find(fun c -> c.ChangesetId = 36151)
    
    let mergeHistory = 
        qualifContext.VersionControlServer.QueryMerges(null, null, "$/Front Office 5.0/1.Front/OrderPipe/Qualif", LatestVersionSpec.Latest, null, null, RecursionType.Full )
        |> Seq.groupBy(fun m -> m.TargetVersion )

    mergeHistory
        |> Seq.iter(fun (target, source) -> 
                printfn "%A" target
                source |> Seq.iter(fun c-> printfn "    %A" c.SourceVersion)
                )
    //    |> Seq.sortBy(fun c -> c.TargetChangeset.CreationDate)
    //    |> Seq.iter( fun c-> printfn "%A %A %A" (c.TargetChangeset.CreationDate.ToString()) c.TargetChangeset.ChangesetId c.SourceVersion )

    // Livré toujours en resolved
    let latestLivraison = 
        qualifContext.BuildServer.QueryBuilds("Front Office 5.0", "OrderPipe_R7")
        |> Seq.find(fun b-> b.BuildNumber = "OrderPipe_R7_v1.0.0.297.01")

    type ChangesetWithMerges = 
        {
            Changeset:Changeset
            MergedChangesets:seq<Changeset>
        }

    qualifContext.History
        |> Seq.filter( fun h -> 
            if h.ChangesetId <= System.Convert.ToInt32(latestLivraison.SourceGetVersion.Replace("C","")) 
                then 
                    h.AssociatedWorkItems |> Seq.exists(fun w->w.State = "Resolved" ) 
                else false
                )
        |> Seq.map( fun h-> h.AssociatedWorkItems |> Seq.filter(fun w->w.State = "Resolved" ))
        |> Seq.concat
        |> Seq.distinctBy( fun h-> h.Id)
        |> Seq.iter( fun h-> printfn "%A %A" h.Id h.Title )
