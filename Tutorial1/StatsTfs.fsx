

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

    let cachePath = @"C:\Users\llepinay\Documents\Visual Studio 2012\Projects\Playground\Tutorial1\cache.txt"
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
                    let msbuild = System.Diagnostics.Process.Start(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe", @""""+ checkoutFolder + @"$/Front Office 5.0/1.Front/OrderPipe/Dev/src/OrderPipe/OrderPipe.sln""")
                    msbuild.WaitForExit()
                    match msbuild.ExitCode with
                        | 0 -> 
                            let tests = 
                                    let tests = Directory.EnumerateFiles(checkoutFolder+sourceFolder, "*tests*.dll", SearchOption.AllDirectories)
                                    let specs = Directory.EnumerateFiles(checkoutFolder+sourceFolder, "*specs.inmem.dll", SearchOption.AllDirectories)
                                    Seq.append tests specs  
                                    |> Seq.filter(fun f-> f.Contains("bin"))
                                    |> Seq.map(fun f-> 
                                                      printfn "%A" f
                                                      "/testcontainer:\""+f+"\"" )
                                    |> Seq.fold (fun a b -> a + " " + b) ""

                            match tests with
                            | "" -> Failure({id=changeset.id;date=changeset.date;reason="No tests"})
                            | _ ->
                                    let p = System.Diagnostics.Process.Start(@"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\mstest.exe", 
                                                                         @"/runconfig:"""+checkoutFolder+"$/Front Office 5.0/1.Front/OrderPipe/Dev/src/OrderPipe/local.testsettings\" " +
                                                                         @"/resultsfile:"""+checkoutFolder+"testresults4.trx \" "
                                                                         + tests) 
                                    p.WaitForExit()
                                    match p.ExitCode with
                                        | 0 ->
                                            let coverageFiles = Directory.EnumerateFiles(checkoutFolder,"*.coverage",SearchOption.AllDirectories)
                                            match Seq.toList coverageFiles with
                                                | [] -> Failure({id=changeset.id;date=changeset.date;reason="No coverage data"})
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
                                            System.IO.File.AppendAllLines(cachePath,[changeset.id.ToString()+";"+"Unit tests failed"])
                                            Failure({id=changeset.id;date=changeset.date;reason="Unit tests failed"})
                        | _ -> 
                                System.IO.File.AppendAllLines(cachePath,[changeset.id.ToString()+";"+"Build failed"])
                                Failure({id=changeset.id;date=changeset.date;reason="Build failed"})


    let rec averageCoverage (changesets:list<ChangeSetDetail>) (coverageForChangeset: ChangeSetDetail -> CacheEntry) results : list<CacheEntry> = 
        match changesets with
            | [] -> results
            | changeset::xs -> 
                let r = coverageForChangeset changeset :: results
                averageCoverage xs coverageForChangeset r
                    
    let cov:list<CacheEntry> = averageCoverage (allChangesets |> Seq.toList) coverageForChangeset []

    

    type progressResult = 
        { 
            coverage:CacheEntry
            progress:int
        }

    let rec progressReport (resultin:list<CacheEntry>) (prev:CacheEntry option) :list<progressResult> = 
        match resultin with
            | [] -> []
            | Success head::queue ->
                let progress = 
                    match prev with
                                |None -> 0
                                |Some(Success prev) -> (int head.totalCovered - int prev.totalCovered ) - (int head.totalNotCovered - int prev.totalNotCovered)
                {
                    coverage=head
                    progress= progress
                }::progressReport queue (Some(head))


     //cov |> Seq.iter(fun c-> printfn "\"%A\" %A %A" c.date c.value c.author )   
//     let cov = 
//                [
//                    {coverageResult.date=System.DateTime.Now;value=float 0;totalCovered=uint32 10;totalNotCovered=uint32 10;author="a"}
//                    {coverageResult.date=System.DateTime.Now;value=float 0;totalCovered=uint32 15;totalNotCovered=uint32 12;author="b"}
//                    {coverageResult.date=System.DateTime.Now;value=float 0;totalCovered=uint32 16;totalNotCovered=uint32 12;author="c"}
//                    {coverageResult.date=System.DateTime.Now;value=float 0;totalCovered=uint32 16;totalNotCovered=uint32 12;author="c"}
//                    {coverageResult.date=System.DateTime.Now;value=float 0;totalCovered=uint32 20;totalNotCovered=uint32 12;author="c"}
//                ]

    let makereportFromCache c = 
        (progressReport (Seq.toList c) None )

    let printReport r = 
        r
        |> Seq.iter(fun c-> printfn "\"%A\" %A %A %A %A %A" c.coverage.changeset.date c.coverage.totalCovered c.coverage.totalNotCovered c.progress c.coverage.changeset.author c.coverage.changeset.id )       

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
