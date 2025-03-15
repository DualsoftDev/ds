using DevExpress.XtraEditors;
using Dsu.PLCConverter.FS;
using Dsu.PLCConverter.UI;
using Dsu.Common.CS.LSIS.ExtensionMethods;
using log4net.Appender;
using static Dsu.PLCConverter.FS.CSVTypes;
using static Dsu.PLCConverter.FS.XgiSymbol;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace MelsecConverter
{
    public partial class FormAddressMapper
    : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    , IAppender
    {
        private async Task ConvertXGI()
        {
            UpdateConfig();

            if (!_PouCommentPaths.Any())
            {
                ShowMissingPouMessage();
                return;
            }

            navigationFrame.SelectedPageIndex = 0;

            using (var waitor = new SplashScreenWaitor("변환", "파일 변환 중 입니다."))
            {
                await Task.Run(() => PerformConversion());
            }
        }

        private void ShowMissingPouMessage()
        {
            XtraMessageBox.Show(
                "파일설정 > POU 가져오기가 필요합니다.",
                "IMPORT 이상",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private async Task PerformConversion()
        {
            try
            {
                int fileCount = 0;
                List<string> results = new List<string>();
                bool bDirectAddress = radioButton_Address.Checked;

                UpdateProcessDisplay("Start", 1, "");

                var (pous, comments, globalLabel) = CSVParser.parseCSVs(_PouCommentPaths);
                await Task.Delay(10);

                UpdateProcessDisplay("Convert: comments", 5, "");
                var allSymbols = XgiFile.getAllSymbols(pous, comments, globalLabel);
                _lstSymbolXGI = GetCheckedSymbols(allSymbols.Item1.ToList());

                foreach (var pou in pous)
                {
                    await ProcessPou(pou, results, allSymbols, bDirectAddress, ++fileCount, pous.Length);
                }

                FinalizeConversion(results, allSymbols, bDirectAddress);

                var successCount = _logs.Where(log => log.Result == ResultData.Success).Count();
                var warningCount = _logs.Where(log => log.Result == ResultData.Warning).Count();
                var failureCount = _logs.Where(log => log.Result == ResultData.Failure).Count();
                Logger.Info($"변환결과 : 성공 {successCount}, 경고 {warningCount}, 실패 {failureCount}");

                UpdateProcessDisplay("Ready", 0, "");
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex}");
                MsgBox.Error("에러", $"로딩에 실패하였습니다.\r\n{ex.Message}");
            }
        }

        private async Task ProcessPou(
            POUParseResult pou,
            List<string> results,
            Tuple<IEnumerable<SymbolInfo>, Dictionary<string, SymbolInfo>> allSymbols,
            bool bDirectAddress,
            int fileCount,
            int totalFiles
        )
        {
            int percent = Convert.ToInt32((double)fileCount / (totalFiles + 1) * 100);
            UpdateProcessDisplay($"Convert: {pou.Name}", percent, "");

            var (xmlPou, errLogs, warnLogs) = XgiFile.getXgiPou(pou.ToEnumerable(), allSymbols.Item1, bDirectAddress);
            results.Add(xmlPou);

            await Task.Delay(10);

            this.Do(() => LogConversionResults(pou, errLogs, warnLogs));
        }

        private void LogConversionResults(POUParseResult pou, List<string> errLogs, List<string> warnLogs)
        {
            if (errLogs.Any())
            {
                LogErrors(pou.Name, errLogs);
            }
            else
            {
                LogSuccess(pou.Name);
            }

            if (warnLogs.Any())
            {
                LogWarnings(pou.Name, warnLogs);
            }
         
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void FinalizeConversion(
            List<string> results,
            Tuple<IEnumerable<SymbolInfo>, Dictionary<string, SymbolInfo>> allSymbols,
            bool bDirectAddress
        )
        {
            this.Do(() =>
            {

                if (_LastOutputPath.IsNullOrEmpty())
                    _LastOutputPath = _PouCommentPaths.First();

                LogFinalResults(allSymbols);

                EnsureOutputDirectory();

                string fileXml = XgiFile.getXgiXMLPou(string.Join("\r\n", results), allSymbols.Item1, bDirectAddress);
                string outputPath = Path.Combine(
                    _DirOutputPath,
                    $"ConvertXGI_{DateTime.Now:yy_MM_dd HH_mm_ss}.xgwx"
                );
                File.WriteAllText(outputPath, fileXml, new UTF8Encoding());

                Logger.Info($"{outputPath} Convert OK");

                if (!Utils.FocusIfExplorerOpen(_DirOutputPath))
                {
                    // 폴더가 열려 있지 않다면 새로 엽니다.
                    Process.Start("explorer.exe", _DirOutputPath);
                }

            });
        }


        private void LogFinalResults(Tuple<IEnumerable<SymbolInfo>, Dictionary<string, SymbolInfo>> allSymbols)
        {
            allSymbols.Item2.ForEach(symbol =>
            {
                AddLog(ResultCase.Address, ResultData.Success, "", $"MELSEC: {symbol.Value.GxAddress.PadRight(20)} XGI: {symbol.Value.Address}"); 
            });

            var duplicateAddresses = _lstSymbolXGI.Where(symbol => symbol.SameAddress > 1).OrderBy(symbol => symbol.Address);
            if (duplicateAddresses.Any())
            {
                duplicateAddresses.ForEach(symbol =>
                {
                    AddLog(ResultCase.Address, ResultData.Failure, "", 
                        $"영역 충돌 MELSEC: {symbol.GxAddress.PadRight(20)} XGI: {symbol.Address}"
                    );
                });
            }
        }

        private void EnsureOutputDirectory()
        {
            DirectoryInfo di = new DirectoryInfo(_DirOutputPath);
            if (!di.Exists)
            {
                di.Create();
            }
        }
    }
}
