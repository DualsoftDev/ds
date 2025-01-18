// Copyright (c) Dualsoft  All Rights Reserved.
// Dualsoft에 저작권이 있습니다. 모든 권한 보유.

namespace Engine.Core

open System
open System.Linq
open System.Diagnostics
open System.Collections.Generic
open System.Reflection

open Dual.Common.Core.FS
open Dual.Common.Base.FS
open Engine.Common

[<AutoOpen>]
module CoreDevicesModule =

    // 파서 로딩 타입 정의
    type ParserLoadingType = DuNone | DuDevice | DuExternal

    /// External 시스템 로딩 시 공유할 정보를 저장하는 클래스
    type ShareableSystemRepository = Dictionary<string, ISystem>

    [<AutoOpen>]
    module DeviceModule =

        // 장치 로딩 파라미터 정의
        type DeviceLoadParameters = {
            /// 로딩된 시스템이 속한 컨테이너 시스템
            ContainerSystem: ISystem
            AbsoluteFilePath: string
            /// 로딩을 위해 사용자가 지정한 파일 경로. 직렬화 시에는 절대 경로를 사용하지 않기 위한 용도로 사용됩니다.
            RelativeFilePath: string
            /// *.ds 파일에서 정의된 이름과 로딩할 때의 이름이 다를 수 있습니다.
            LoadedName: string
            ShareableSystemRepository: ShareableSystemRepository
            LoadingType: ParserLoadingType
        }

        // 장치 LayoutInfo  정의
        type DeviceLayoutInfo = {
            DeviceName: string
            ChannelName: string
            Path: string
            ScreenType: ScreenType
            Xywh: Xywh
        }

        [<AbstractClass>]
        type LoadedSystem (loadedSystem: ISystem, param: DeviceLoadParameters, autoGenFromParentSystem:bool) =
            inherit FqdnObject(param.LoadedName, param.ContainerSystem)
            let mutable loadedName = param.LoadedName // 로딩 주체에 따라 런타임에 변경
            do
                if not(param.AbsoluteFilePath  |> PathManager.isPathRooted)
                then raise (new ArgumentException($"The AbsoluteFilePath must be PathRooted ({param.AbsoluteFilePath})"))
                if param.RelativeFilePath |> PathManager.isPathRooted
                then raise (new ArgumentException($"The RelativeFilePath must be not PathRooted ({param.RelativeFilePath})"))

            interface ISystem

            member _.LoadedName with get() = loadedName and set(value) = loadedName <- value
            member _.AutoGenFromParentSystem  = autoGenFromParentSystem

            ///CCTV 경로 및 배경 이미지 경로 복수의 경로에 배치가능
            member val ChannelPoints = Dictionary<string, Xywh>()

            /// 다른 장치를 로딩하려는 시스템에서 로딩된 시스템을 참조합니다.
            member internal _.ReferenceISystem = loadedSystem
            member internal _.ContainerISystem = param.ContainerSystem
            member _.RelativeFilePath:string = param.RelativeFilePath
            member _.AbsoluteFilePath:string = param.AbsoluteFilePath
            member _.LoadingType: ParserLoadingType = param.LoadingType

        /// *.ds 파일을 읽어 새로운 인스턴스를 만들어 삽입하는 구조입니다.
        type Device (loadedDevice: ISystem, param: DeviceLoadParameters, autoGenFromParentSystem:bool) =
            inherit LoadedSystem(loadedDevice, param, autoGenFromParentSystem)
            static let mutable id = 0
            do
                id <- id + 1
            member val Id = id with get

        /// 공유 인스턴스. *.ds 파일의 절대 경로를 기준으로 하나의 인스턴스만 생성하고 이를 참조하는 개념입니다.
        type ExternalSystem (loadedSystem: ISystem, param: DeviceLoadParameters, autoGenFromParentSystem:bool) =
            inherit LoadedSystem(loadedSystem, param, autoGenFromParentSystem)

