

#r  "Microsoft.TeamFoundation.Client"
#r  "System.Xml"
#r  "Microsoft.TeamFoundation.Build.Client"
#r  "Microsoft.TeamFoundation.VersionControl.Client"
#r  "Microsoft.TeamFoundation.TestManagement.Client"
#r  "Microsoft.TeamFoundation.WorkItemTracking.Client"
#r  @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\PrivateAssemblies\Microsoft.VisualStudio.Coverage.Analysis.dll"
#r  "System.Data.DatasetExtensions"
module TFS = 
    open Microsoft.TeamFoundation.Client 
    open Microsoft.TeamFoundation.VersionControl.Client 
    open Microsoft.TeamFoundation.Build.Client  
    open Microsoft.TeamFoundation.TestManagement.Client  
    open Microsoft.VisualStudio.Coverage.Analysis
    open System.IO

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
            History = vc.QueryHistory(srcFolder,RecursionType.Full) |> Seq.filter(fun h-> not(h.Committer.Contains("svc_TfsService")) )
        }

    let qualifContext = createContext "$/Front Office 5.0/1.Front/OrderPipe/Qualif"

    // Non livré encore
    qualifContext.History
        |> Seq.filter( fun h -> h.CreationDate >= System.DateTime.Parse("30/01/2013 11:57:00") )
        |> Seq.map( fun h-> h.AssociatedWorkItems )

    // Livré toujours en resolved
    qualifContext.History
        |> Seq.filter( fun h -> h.CreationDate <= System.DateTime.Parse("31/01/2013 10:12:00") && h.WorkItems |> Seq.exists(fun w->w.State = "Resolved" ) )
        |> Seq.map( fun h-> h.AssociatedWorkItems |> Seq.filter(fun w->w.State = "Resolved" ))
        

    let tfsUrl = new System.Uri("http://vptfs/tfs/sivp")
    let collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsUrl)
    let vc = collection.GetService<VersionControlServer>()
    let bs = collection.GetService<IBuildServer>()
    let tm = collection.GetService<ITestManagementService>()
    let project = vc.TryGetTeamProject("Front Office 5.0")
    let srcFolder = "$/Front Office 5.0/1.Front/OrderPipe/Dev/src/OrderPipe"
    let history = vc.QueryHistory(srcFolder,RecursionType.Full)

//    let refactos = history 
//                            |> Seq.filter( fun h -> h.Comment.ToLower().Contains("refacto"))
//                            |> Seq.groupBy( fun h -> h.CommitterDisplayName )
//                            |> Seq.map( fun (u,c) -> (u, Seq.length c))
//                            |> Seq.sortBy( fun( u,c) -> c)
//    printfn "refactos"
//    for c in refactos do
//        printfn "%A" c
//
//    let modifs = history 
//                            |> Seq.groupBy( fun h -> h.CommitterDisplayName )
//                            |> Seq.map( fun (u,c) -> (u, Seq.length c))
//                            |> Seq.sortBy( fun( u,c) -> c)
//    printfn "modifs"
//    for c in modifs do
//        printfn "%A" c
// float(m.Statistics.BlocksCovered) / float(m.Statistics.BlocksCovered+m.Statistics.BlocksNotCovered)
    
    let startProcess p pars = 
        System.Diagnostics.Process.Start(p, pars)
    let enumerateAllFiles dir pattern = Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories)

    type ChangeSetDetail = {
        id:int;
        author:string;
        date:System.DateTime;
        comment:string;
    }

    let allChangesets = 
        history 
            |> Seq.filter(fun h-> not(h.Committer.Contains("svc_TfsService")) )
            |> Seq.map(fun h-> {id=h.ChangesetId;author=h.Committer;date=h.CreationDate;comment=h.Comment}) 
            |> Seq.toList

    allChangesets |> Seq.iter(fun c-> printfn "%A" c.id)

    type coverageResult = {
        changeset:ChangeSetDetail
        value:float;
        totalCovered:uint32;
        totalNotCovered:uint32;
    }

    type FailedRun = {
        id:int
        date:System.DateTime
        reason:string
    }

    type CacheEntry =
        | Success of coverageResult
        | NoRecord
        | Failure of FailedRun

    let cachePath = @"C:\Users\llepinay\documents\visual studio 2012\Projects\Playground\Tutorial1\cache.txt"

    let appendAllLines path lines = System.IO.File.AppendAllLines(path, lines)

    allChangesets 
    |> Seq.map( fun c-> c.id.ToString()+";"+c.author+";"+c.date.ToString()+";"+c.comment )
    |> appendAllLines (cachePath + "_changesets")

    let cache = 
        System.IO.File.ReadLines(cachePath)
        |> Seq.map( fun l -> match l.Split(';') with
                                |[|id;date;value;author;totalCovered;totalNotCovered|] 
                                    -> Success({
                                                 coverageResult.changeset = 
                                                    {   
                                                        id=System.Int32.Parse(id)
                                                        date=System.DateTime.Parse(date)
                                                        author=author;
                                                        comment=""
                                                    };
                                                 value=System.Double.Parse(value);
                                                 totalCovered=System.UInt32.Parse(totalCovered);
                                                 totalNotCovered=System.UInt32.Parse(totalNotCovered)})
                                |[|id;date;reason|]
                                    -> Failure({id=System.Int32.Parse(id);date=System.DateTime.Parse(date);reason=reason})
                                | _ -> NoRecord )

//    let correctedCache = 
//        cache 
//        |> Seq.map( fun c-> 
//            match c with 
//                |Failure r-> 
//                    let correctedDate = allChangesets |> Seq.find( fun c -> c.id = r.id)
//                    Failure({id=r.id;date=correctedDate.date;reason=r.reason})
//                |r->r)
//
//    correctedCache
//        |> Seq.filter(fun c -> match c with |Failure r->true|_->false)
//
//    correctedCache
//        |> Seq.sortBy(fun c-> match c with 
//                                | NoRecord -> System.DateTime.MaxValue
//                                | Success(r) -> r.changeset.date
//                                | Failure(r) -> r.date) 
//        |> Seq.map(fun c-> match c with
//                            | Failure r -> r.id.ToString() + ";" + r.date.ToString() + ";" + r.reason
//                            | Success r -> r.changeset.id.ToString() + ";" + r.changeset.date.ToString() + ";" + r.value.ToString() + ";" + r.changeset.author + ";" + r.totalCovered.ToString() + ";" + r.totalNotCovered.ToString()
//                            | NoRecord -> "" )
//        |> appendAllLines (cachePath + "_new.txt")

    let sortedCache = cache 
                        |> Seq.sortBy(fun c-> match c with 
                                                | NoRecord -> System.DateTime.MaxValue
                                                | Success(r) -> r.changeset.date
                                                | Failure(r) -> r.date)

    let coverageForChangeset (changeset:ChangeSetDetail) = 
        match cache |> Seq.tryFind ( fun c -> match c with 
                                                 |Success(r) -> r.changeset.id = changeset.id
                                                 |Failure(r) -> r.id = changeset.id
                                                 |NoRecord -> false ) with
            |Some(r) -> r
            |None ->
                    let checkoutFolder = @"c:/temp/yo3/"
                    let sourceFolder = "$/Front Office 5.0/1.Front/OrderPipe/Dev/src/OrderPipe"
                    if Directory.Exists(checkoutFolder) then Directory.Delete(checkoutFolder, true)
                    Directory.CreateDirectory(checkoutFolder) |> ignore
                    vc.GetItems(sourceFolder, new ChangesetVersionSpec(changeset.id) ,RecursionType.Full).Items 
                        |> Seq.filter( fun i -> i.ItemType = ItemType.File) 
                        |> Seq.iter( fun i -> printfn "%A" i; i.DownloadFile(checkoutFolder + i.ServerItem ))
                    let msbuild = startProcess @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe" (@""""+ checkoutFolder + @"$/Front Office 5.0/1.Front/OrderPipe/Dev/src/OrderPipe/OrderPipe.sln""")
                    msbuild.WaitForExit()
                    match msbuild.ExitCode with
                        | 0 -> 
                            let tests = 
                                    let tests = enumerateAllFiles (checkoutFolder+sourceFolder) "*tests*.dll"
                                    let specs = enumerateAllFiles (checkoutFolder+sourceFolder) "*specs.inmem.dll"
                                    Seq.append tests specs  
                                    |> Seq.filter(fun f-> f.Contains("bin"))
                                    |> Seq.map(fun f-> 
                                                      printfn "%A" f
                                                      "/testcontainer:\""+f+"\"" )
                                    |> Seq.fold (fun a b -> a + " " + b) ""

                            match tests with
                            | "" -> Failure({id=changeset.id;date=changeset.date;reason="No tests"})
                            | _ ->
                                    let p = startProcess (@"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\mstest.exe") 
                                                         (@"/runconfig:"""+checkoutFolder+"$/Front Office 5.0/1.Front/OrderPipe/Dev/src/OrderPipe/local.testsettings\" " +
                                                          @"/resultsfile:"""+checkoutFolder+"testresults4.trx \" " + tests) 
                                    p.WaitForExit()
                                    match p.ExitCode with
                                        | 0 ->
                                            let coverageFiles = enumerateAllFiles checkoutFolder "*.coverage"
                                            match Seq.toList coverageFiles with
                                                | [] -> 
                                                    System.IO.File.AppendAllLines(cachePath,[changeset.id.ToString()+";"+changeset.date.ToString()+";No coverage data"])
                                                    Failure({id=changeset.id;date=changeset.date;reason="No coverage data"})
                                                | _ ->
                                                        let coverageFile = Seq.head(coverageFiles)
                                                        let ci = CoverageInfo.CreateFromFile(coverageFile);
                                                        let dataSet = ci.BuildDataSet();
                                                        let totalCovered = dataSet.Module |> Seq.sumBy( fun m -> m.BlocksCovered) 
                                                        let totalNotCovered = dataSet.Module |> Seq.sumBy( fun m -> m.BlocksNotCovered) 
                                                        let coveragePct = float totalCovered / float(totalCovered+totalNotCovered)
                                                        System.IO.File.AppendAllLines(cachePath, 
                                                            [
                                                                changeset.id.ToString()+";"+
                                                                changeset.date.ToString()+";"+
                                                                coveragePct.ToString()+";"+
                                                                changeset.author+";"+
                                                                totalCovered.ToString()+";"+
                                                                totalNotCovered.ToString()
                                                            ])
                                                        Success({
                                                                changeset = changeset
                                                                value=coveragePct;
                                                                totalCovered = totalCovered;
                                                                totalNotCovered = totalNotCovered;
                                                        })
                                        | _ ->
                                            System.IO.File.AppendAllLines(cachePath,[changeset.id.ToString()+";"+changeset.date.ToString()+";Unit tests failed"])
                                            Failure({id=changeset.id;date=changeset.date;reason="Unit tests failed"})
                        | _ -> 
                                System.IO.File.AppendAllLines(cachePath,[changeset.id.ToString()+";"+changeset.date.ToString()+";Build failed"])
                                Failure({id=changeset.id;date=changeset.date;reason="Build failed"})


    let rec averageCoverage (changesets:list<ChangeSetDetail>) (coverageForChangeset: ChangeSetDetail -> CacheEntry) results : list<CacheEntry> = 
        match changesets with
            | [] -> results
            | changeset::xs -> 
                let r = coverageForChangeset changeset :: results
                averageCoverage xs coverageForChangeset r
                    
    let cov:list<CacheEntry> = averageCoverage (allChangesets |> Seq.toList) coverageForChangeset []

    allChangesets |> Seq.iter(fun c -> printfn "%A" c.date )

    coverageForChangeset {ChangeSetDetail.author="test";ChangeSetDetail.comment="";ChangeSetDetail.date=System.DateTime.Now;ChangeSetDetail.id=20928;}
    

    type progressResult = 
        { 
            coverage:coverageResult
            progress:int
        }

    let rec progressReport (resultin:list<CacheEntry>) (prev:coverageResult option) :list<progressResult> = 
        match resultin with
            | [] -> []
            | Success head::queue ->
                let progress = 
                    match prev with
                                |Some(prev) -> (int head.totalCovered - int prev.totalCovered ) - (int head.totalNotCovered - int prev.totalNotCovered)
                                | _ -> 0
                {
                    coverage=head
                    progress= progress
                }::progressReport queue (Some(head))
            | _::queue -> progressReport queue prev

    let makereportFromCache c = 
        (progressReport (Seq.toList c) None  )

    type progressResultWithCumul = 
        {
            progress:progressResult
            cumul:int
        }

    let rec cumul cumu (progress:list<progressResult>) :list<progressResultWithCumul> = 
        match progress with
            | head::queue when System.Math.Abs(head.progress) < 100 -> 
                let newCumul = cumu+head.progress
                {progress=head;cumul=newCumul}:: cumul newCumul queue 
            | head::queue -> 
                cumul cumu queue 
            | [] -> []
        

    let printReport r = 
        r
        |> Seq.groupBy( fun c -> c.coverage.changeset.author )
        |> Seq.iter(fun(k,v)-> 
            v 
            |> Seq.toList 
            |> cumul 0
            |> List.iter( fun c -> printfn "\"%A\" %A %A %A %A %A %A" 
                                    c.progress.coverage.changeset.date 
                                    c.progress.coverage.totalCovered 
                                    c.progress.coverage.totalNotCovered 
                                    c.progress.progress
                                    c.progress.coverage.changeset.author 
                                    c.progress.coverage.changeset.id 
                                    c.cumul ))       

    sortedCache |> makereportFromCache |> printReport

//    let builds = bs.QueryBuilds("Front Office 5.0", "OrderPipe_DEV_FT")
//    let teamProject = tm.GetTeamProject("Front Office 5.0")
//    let am = teamProject.CoverageAnalysisManager

//    let coverageReport =  
//            seq { for build in builds do
//                    for coverage in am.QueryBuildCoverage(build.Uri.ToString(), CoverageQueryFlags.Modules) do
//                            yield (build.BuildDefinition.Name + " " + coverage.Configuration.BuildFlavor + " " + coverage.Configuration.BuildPlatform, 
//                                   build.FinishTime.ToString(), 
//                                   coverage.Modules 
//                                    |> Seq.map( fun m -> float(m.Statistics.BlocksCovered) / float(m.Statistics.BlocksCovered+m.Statistics.BlocksNotCovered) )
//                                    |> Seq.average )  }
//            |> Seq.groupBy( fun (conf, time, stat) -> conf )
//
//    for (key,value) in coverageReport do
//        printfn "%A" key
//        for (conf, time, stat) in value do
//            printfn "%A %A %%" time (int(stat)*int(100))

    ()

#r "System.Data.dll"
#r "FSharp.Data.TypeProviders.dll"
#r "System.Data.Linq.dll"

open System
open System.Data
open System.Data.Linq
open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq

type dbSchema = SqlDataConnection<"">
let db = dbSchema.GetDataContext()    
query {
    for row in db.VP_T_Carts do
    where (row.MemberId = 259)
    sortBy row.CreationDate
    select row
} |> Seq.last


#if COMPILED
module BoilerPlateForForm = 
    [<System.STAThread>]
    do ()
    do System.Windows.Forms.Application.Run()
#endif
