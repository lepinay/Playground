namespace WebClient.Controllers

open System.Web.Http

type public LivraisonsController() =
    inherit ApiController()
        member public x.Get() = 
            TFS.SuivitLivraisons.report
