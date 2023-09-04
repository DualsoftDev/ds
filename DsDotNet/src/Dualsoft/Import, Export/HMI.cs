using DevExpress.XtraSplashScreen;
using System.Text;
using Engine.CodeGenHMI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using DevExpress.ClipboardSource.SpreadsheetML;
using System.Runtime.InteropServices.ComTypes;
using static Engine.Core.CoreModule;
using Dual.Common.Core.FS;
using System.Collections.Generic;
using static Engine.Core.DsText;
using static Engine.Core.SystemToDsExt;
using Engine.Core;
using Engine.Parser.FS;
using Microsoft.Msagl.GraphViewerGdi;
using static Engine.Parser.FS.ParserOptionModule;
using System.Linq;

namespace DSModeler
{
    public static class HMI
    {
        public static string Export(FormMain formMain)
        {
            if (!Global.IsLoadedPPT())
            {
                Global.Logger.Warn("PPTX 가져오기를 먼저 수행하세요");
                return "";
            }
            
            SplashScreenManager.ShowForm(typeof(DXWaitForm));
            var model = formMain.Model;
            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.All
            };
            settings.Converters.Add(
                new Newtonsoft.Json.Converters.StringEnumConverter()
            );

            //string dsText = "";
            //foreach (var sys in PcControl.RunCpus.SelectMany(s=>s.Systems))
            //    dsText += $"{sys.ToDsText(Global.IsDebug)}\r\n\r\n";

            var systemRepo = new Dictionary<string, DsSystem>();
            var jsonPath = Global.ExportPathDS.Replace(".ds", ".json");
            var md = ModelLoader.LoadFromConfig(jsonPath);
            var newSys = md.Systems;
            //var option = ParserOptions.Create4Runtime(systemRepo, "./", "ActiveCpuName", "./", ParserLoadingType.DuNone);
            //var newSys = ModelParser.ParseFromString(dsText, option);

            var json = JsonConvert.SerializeObject(model, settings);
            //var md = JsonConvert.DeserializeObject<Engine.Core.ModelLoaderModule.Model>(json);
            //var hmiGenModule = new HmiGenModule.HmiCode(model);
            //var json = CodeGenHandler.JsonWrapping(hmiGenModule.Generate());
            SplashScreenManager.CloseForm();

            return json;
        }
    }
}