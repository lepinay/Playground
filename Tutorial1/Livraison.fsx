#time "on"
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
    
    getBuildsLabels "$/Front Office 5.0/1.Front/OrderPipe/Qualif"
    |> Seq.iter(fun b->printfn "%A" b)    
