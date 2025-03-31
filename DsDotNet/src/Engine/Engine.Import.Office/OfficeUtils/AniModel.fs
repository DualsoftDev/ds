namespace Engine.Import.Office

open System.Drawing
open System.Linq
open System.Collections.Generic
open System
open Newtonsoft.Json

type AniDevice() =
    member val Name = "" with get, set
    member val Rectangle = new Rectangle() with get, set
    member val AddressIn = "" with get, set
    member val AddressOut = "" with get, set
    member val AniApis = HashSet<AniApi>() with get, set

and AniApi() =
    member val Name = "" with get, set //Coin  이름
    member val DevNApiName = "" with get, set //Api 순수 이름
    member val ApiFilePath = "" with get, set
    member val InitFinished = false with get, set
    member val Disable = false with get, set
    member val Device = new AniDevice() with get, set
    member val AniDuration = 1.0f with get, set
    member val AniSequence = 0 with get, set
    member val ApiRectangle = new Rectangle() with get, set
        
    member x.DisplayTimeChartName = String.Join(".", x.ApiFilePath.Split('.').Skip(1)).Replace(".", "\v")

type AniLink() =
    member val Source = new AniApi() with get, set
    member val Target = new AniApi() with get, set
    member x.IsInitLink = x.Source = x.Target

type AniModel() =
    member val AniLinks = HashSet<AniLink>() with get, set
    member val Speed = 0.5f with get, set
    member val FlowName = "" with get, set
    member val RealName = "" with get, set

    member x.AddLinks(aniLink:AniLink) = x.AniLinks.Add aniLink    
    member x.GetHeadLinks() =
        x.AniLinks |> Seq.filter (fun s -> s.IsInitLink)

    member x.GetApis() =
        x.AniLinks |> Seq.collect (fun link -> [link.Source; link.Target])
                   |> Seq.filter(fun s -> not s.Disable)
                   |> Seq.distinct

    member x.GetDevices() =
        x.AniLinks
        |> Seq.collect (fun link -> [link.Source.Device; link.Target.Device])
        |> Seq.distinct

    member x.GetTargets(srcName: string) =
        x.AniLinks
        |> Seq.filter (fun s -> not s.IsInitLink && s.Source.DevNApiName = srcName)


    member x.GetDistinctApis(findDevice: AniDevice) =
        x.GetApis()
        |> Seq.filter (fun api -> api.Device = findDevice)
        |> Seq.distinctBy(fun api -> api.Name)

