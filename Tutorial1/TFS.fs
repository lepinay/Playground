//#r  "Microsoft.TeamFoundation.Client"
//#r  "System.Xml"
//#r  "Microsoft.TeamFoundation.Build.Client"
//#r  "Microsoft.TeamFoundation.VersionControl.Client"
//#r  "Microsoft.TeamFoundation.TestManagement.Client"
//#r  "Microsoft.TeamFoundation.WorkItemTracking.Client"
//#r  @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\PrivateAssemblies\Microsoft.VisualStudio.Coverage.Analysis.dll"
//#r  "System.Data.DatasetExtensions"
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
            History = vc.QueryHistory(srcFolder,RecursionType.Full) |> Seq.filter(fun h-> not(h.Committer.Contains("svc_TfsService")) )
        }