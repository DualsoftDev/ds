using Engine.Common;
using Engine.Common.FS;
using Engine.Core;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.CodeGenCPU.ExportModule;
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Cpu.RunTime;

namespace Dual.Model.Import
{

    public partial class FormMain : Form
    {
        internal void ExportExcel()
        {
            if (UtilFile.BusyCheck()) return;
            try
            {
                ResultBtnAbleUI(false);

                var pathXLS = UtilFile.GetNewPathXls(_PathPPTs);

                Directory.CreateDirectory(Path.GetDirectoryName(pathXLS));
                ExportIOTable.ToFiie(GetSystems(), pathXLS);

                WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{pathXLS} 생성완료!!");

                ResultBtnAbleUI(true);
                Process.Start($"{pathXLS}");
                FileWatcher.CreateFileWatcher(pathXLS);
                _ResultDirectory = Path.GetDirectoryName(pathXLS);
            }
            catch (Exception ex) { WriteDebugMsg(DateTime.Now, MSGLevel.MsgError, ex.Message); }
            finally { ProcessEvent.DoWork(0); }
        }

        internal void ExportPLC(string path)
        {
            if (UtilFile.BusyCheck()) return;
            try
            {
                Task.Run(async() =>
                {
                    WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{path} PLC 생성시작!!");
                    await Task.Delay(1);
                    _DicCpu = new Dictionary<DsSystem, DsCPU>();
                    var storages = new Storages();
                    int cnt = 0;
                    foreach (var view in _PPTResults)
                    {
                        ProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0) / (_PPTResults.Count() * 1.0) * 100));
                        await Task.Delay(10);
                        if (!view.IsActive) continue;
                        var rungs = Cpu.LoadStatements(view.System, storages);
                        rungs.ForEach(s =>
                        {
                            _DicCpu.Add(s.ToSystem(), new DsCPU(s.CommentedStatements(), s.ToSystem()));
                        });
                    }

                    EventExternal.CPUSubscribe();
                    _DicCpu.ForEach(f =>
                    {
                        f.Value.Run();
                        f.Value.ScanOnce();
                    });

                    var xmlTemplateFile = Path.ChangeExtension(_PathPPTs[0], "xml");
                    this.Do(() =>
                    {
                        if (File.Exists(xmlTemplateFile))
                            //사용자 xg5000 Template 형식으로 생성
                            ExportModuleExt.ExportXMLforXGI(SelectedSystem, path, xmlTemplateFile);
                        else  //기본 템플릿 CPU-E 타입으로 생성
                            ExportModuleExt.ExportXMLforXGI(SelectedSystem, path, null);

                    });

                    ResultBtnAbleUI(true);
                    _ResultDirectory = Path.GetDirectoryName(path);
                    WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{path} PLC 생성완료!!");

                    ProcessEvent.DoWork(0);
                });

            }
            catch (Exception ex) { WriteDebugMsg(DateTime.Now, MSGLevel.MsgError, ex.Message); }
            finally { ProcessEvent.DoWork(0); }
        }
    }
}
