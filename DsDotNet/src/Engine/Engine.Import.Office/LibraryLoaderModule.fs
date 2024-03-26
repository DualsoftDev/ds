// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open Dual.Common.Core.FS
open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Reflection


module LibraryLoaderModule =
    
   
    type LibraryConfig = {
        ///parent �ý��ۿ��� ����� Lib������ ���� ��ġ�� Lib ������ �׻� ���ƾ� �Ѵ�
        Version: string         
        ///Api �̸� �ߺ��� �������� Dictionary ó��
        LibraryInfos: Dictionary<string, string> //Api, filePath 
    }

    let private jsonSettings = JsonSerializerSettings()

    let LoadLibraryConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<LibraryConfig>(json, jsonSettings)

    let SaveLibraryConfig (path: string) (libraryConfig:LibraryConfig) =
        let json = JsonConvert.SerializeObject(libraryConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)
