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
