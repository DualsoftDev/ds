using Dual.Common.Winform;

using Dual.Common.Core;
using Dual.Common.Core.FS;
using Engine.Core;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Dual.Common.Core.FS.MessageEvent;
using static Engine.Core.CoreModule;
using static Engine.Core.RuntimeGeneratorModule;
using static Model.Import.Office.ImportPPTModule;
using Color = System.Drawing.Color;

namespace Dual.Model.Import
{

    public partial class FormMain : Form
    {

        //복수 Active system ppt 불러오기
        internal void ImportPowerPoint(List<string> paths)
        {
            if (UtilFile.BusyCheck()) return;
            try
            {
                _PPTResults = ImportPPT.GetLoadingAllSystem(paths);
                _DicViews = new Dictionary<DsSystem, IEnumerable<ViewModule.ViewNode>>();
                var storages = new Dictionary<string, Interface.IStorage>();
                foreach (var view in _PPTResults)
                {
                    _DicViews.Add(view.System, view.Views.ToList());
                    if (!view.IsActive) continue;
                    var s = view.System;
                    var systemView = new SystemView()
                    {
                        Display = s.Name,
                        System = s,
                        ViewNodes = view.Views.ToList()
                    };
                    comboBox_System.Items.Add(systemView);
                }

                if (comboBox_System.Items.Count > 0)
                    comboBox_System.SelectedIndex = 0;

                paths.ForEach(f =>
                    WriteDebugMsg(DateTime.Now, MSGLevel.MsgWarn, $"{f} 불러오기 성공!!"));

                ProcessEvent.DoWork(0);
            }
            catch (Exception ex)
            {
                WriteDebugMsg(DateTime.Now, MSGLevel.MsgError, ex.Message);
            }
            finally { ProcessEvent.DoWork(0); }
        }

        internal void ImportExcel(string path)
        {
            if (UtilFile.BusyCheck()) return;
            try
            {
                MSGInfo($"{path} 불러오는 중!!");
                Runtime.Target = RuntimeTargetType.XGI;

                ImportIOTable.ApplyExcel(path, GetSystems());

                this.Do(() =>
                {
                    DisplayTextModel(Color.FromArgb(0, 150, 0), SelectedSystem.ToDsText());
                    richTextBox_ds.ScrollToCaret();
                    button_copy.Visible = true;

                    MSGInfo($"{path} 적용완료!!");
                    MSGWarn($"파워포인트와 엑셀을 동시에 가져오면 IO 매칭된 설정값을 가져올수 있습니다.!!");
                    button_CreatePLC.Visible = true;
                });

            }

            catch (Exception ex) { WriteDebugMsg(DateTime.Now, MSGLevel.MsgError, ex.Message); }
            finally { ProcessEvent.DoWork(0); }
        }
    }
}
