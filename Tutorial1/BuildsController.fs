namespace WebClient.Controllers

open System.Web.Http

type public BuildsController() =
    inherit ApiController()
        member public x.Get() = 
            TFS.TFS.getBuildsLabels "$/Front Office 5.0/1.Front/OrderPipe/Qualif"

            