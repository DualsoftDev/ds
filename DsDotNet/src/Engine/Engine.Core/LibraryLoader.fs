namespace rec Engine.Core

open System.IO
open System.Linq
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic


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
