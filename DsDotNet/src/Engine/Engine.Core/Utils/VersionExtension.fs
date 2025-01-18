namespace Engine.Core

open System

open Dual.Common.Core.FS

[<AutoOpen>]
module VersionModule =
    type System.Version with
        member x.Duplicate() = new Version(x.Major, x.Minor, x.Build, x.Revision)
        member this.IsCompatible(that:Version) = this.Major <= that.Major
        member this.CheckCompatible(that:Version, versionCategory:string) =
            if this <> that then
                logWarn $"{versionCategory} version mismatch: {this} <> {that}"
                if this.Major < that.Major then
                    failwithlog " -- Update DS system"

