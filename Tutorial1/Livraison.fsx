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
    
    //qualifContext.VersionControlServer.QueryMerges(null, null, "$/Front Office 5.0/1.Front/OrderPipe/Qualif", new ChangesetVersionSpec(36151), null, null, RecursionType.Full ).[0].

    // Non livré encore
    qualifContext.History
        |> Seq.filter( fun h -> h.CreationDate >= System.DateTime.Parse("30/01/2013 11:57:00") )
        |> Seq.map( fun h-> h.AssociatedWorkItems )

    // Livré toujours en resolved
    let latestLivraison = 
        qualifContext.BuildServer.QueryBuilds("Front Office 5.0", "OrderPipe_R7")
        |> Seq.find(fun b-> b.BuildNumber = "OrderPipe_R7_v1.0.0.290.01")

    qualifContext.History
        |> Seq.filter( fun h -> 
            h.ChangesetId <= System.Convert.ToInt32(latestLivraison.SourceGetVersion.Replace("C","")) && 
            h.WorkItems |> Seq.exists(fun w->w.State = "Resolved" ) )
        |> Seq.map( fun h-> h.AssociatedWorkItems |> Seq.filter(fun w->w.State = "Resolved" ))
        |> Seq.concat
        |> Seq.distinctBy( fun h-> h.Id)
        |> Seq.iter( fun h-> printfn "%A %A" h.Id h.Title )
