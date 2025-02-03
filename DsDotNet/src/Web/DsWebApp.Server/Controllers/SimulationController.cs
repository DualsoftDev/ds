using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using static Engine.Info.DBLoggerAnalysisDTOModule;
using static Engine.Info.DBLoggerORM;
using static Engine.Core.InfoPackageModule;
using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;
using FlatSpans = System.Tuple<string, Engine.Info.DBLoggerAnalysisDTOModule.Span[]>[];
using static Engine.Info.LoggerDB;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// Simulation controller.  api/simulation
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Administrator")]
public class SimulationController(ServerGlobal serverGlobal) : ControllerBaseWithLogger(serverGlobal.Logger)
{
    RuntimeModel _model => serverGlobal.RuntimeModel;
}


/*
 * 
 * PLAY     button_Play_Click       _model.Cpu.Run()
 * PAUSE    button_Pause_Click      _model.Cpu.Stop()
 * STEP     button_Step_Click       _DsCPU.StepByStatus();
 * 원위치    SetOrigin
 * 초기화    button_Reload_Click
 * 
 * Real 선택 후
 *  - Start
 *  - Reset

 
        private void SetOrigin(bool v)
        {
            _IsOrgPushed = true;

            if (v == true)
            {
                CpuExtensionsModule.preManualAction(_DsSys);
            }
            else
            {
                CpuExtensionsModule.preAutoDriveAction(_DsSys);
            }


            SetVertexValue(v, BtnType.Reset);
            _DsSys.GetRealVertices()
                  .Iter(r => (r.TagManager as VertexMReal).OB.Value = v);

            if (v == true)
            {
            }

        }
 
 */