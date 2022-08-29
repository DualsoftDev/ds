namespace Dual.ConvertPLC.FS.LsXGI

open System.Reflection
open Dual.Common

module ConfigXml =
    let private loadResource file =
        let assembly = Assembly.GetExecutingAssembly()
        EmbeddedResource.readFile assembly file


    //<EmbeddedResource Include="LS\ConfigXml\DEVICE_INFO.xml" />
    //<EmbeddedResource Include="LS\ConfigXml\FLAG_COMMENT.xml" />
    //<EmbeddedResource Include="LS\ConfigXml\FLAG_INFO_0.xml" />
    //<EmbeddedResource Include="LS\ConfigXml\PLC_TYPE_LIST.xml" />
    //<EmbeddedResource Include="LS\ConfigXml\tblIOModule.xml" />

    let getDeviceInfoText()  = loadResource "DEVICE_INFO.xml"
    let getFlagCommentText() = loadResource "FLAG_COMMENT.xml"
    let getFlagInfoText()    = loadResource "FLAG_INFO_0.xml"
    let getPlcTypeListText() = loadResource "PLC_TYPE_LIST.xml"
    let getTblIoModuleText() = loadResource "tblIOModule.xml"
    let getXGTCodeDBText()   = loadResource "XGTCodeDB.xml"
