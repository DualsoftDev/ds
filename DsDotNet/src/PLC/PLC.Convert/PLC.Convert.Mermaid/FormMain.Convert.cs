using Dsu.PLCConverter.FS;
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


namespace PLC.Convert.Mermaid
{
    public partial class FormMermaid : Form
    {

        string _DirOutputPath = "";
        private async Task ConvertXGI(string[] paths)
        {
            int fileCount = 0;
            List<string> results = new List<string>();
            bool bDirectAddress = false;

            _DirOutputPath = Path.GetDirectoryName(paths.First());

            var (pous, comments, globalLabel) = CSVParser.parseCSVs(paths);
            await Task.Delay(10);

            var allSymbols = XgiFile.getAllSymbols(pous, comments, globalLabel);
            _lstSymbolXGI = GetCheckedSymbols(allSymbols.Item1.ToList());

            foreach (var pou in pous)
            {
                await ProcessPou(pou, results, allSymbols, bDirectAddress, ++fileCount, pous.Length);
            }

            FinalizeConversion(results, allSymbols, bDirectAddress);
        }

        List<SymbolInfo> _lstSymbolXGI = new List<SymbolInfo>();
        private List<SymbolInfo> GetCheckedSymbols(List<SymbolInfo> allSymbols)
        {
            _lstSymbolXGI = allSymbols;
            Dictionary<string, int> dic = new Dictionary<string, int>();
            _lstSymbolXGI.ForEach(s =>
            {
                if (s.Address != "")
                {
                    if (!dic.ContainsKey(s.Address))
                        dic.Add(s.Address, 1);
                    else
                        dic[s.Address] = dic[s.Address] + 1;
                }
            });
            foreach (var f in _lstSymbolXGI.AsParallel())
            {
                if (f.Address != "") f.SameAddress = dic[f.Address];
            }
            return _lstSymbolXGI;
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

            var (xmlPou, errLogs, warnLogs) = XgiFile.getXgiPou(pou.ToEnumerable(), allSymbols.Item1, bDirectAddress);
            results.Add(xmlPou);

            await Task.Delay(10);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void FinalizeConversion(
            List<string> results,
            Tuple<IEnumerable<SymbolInfo>, Dictionary<string, SymbolInfo>> allSymbols,
            bool bDirectAddress
        )
        {


            LogFinalResults(allSymbols);

            EnsureOutputDirectory();

            string fileXml = XgiFile.getXgiXMLPou(string.Join("\r\n", results), allSymbols.Item1, bDirectAddress);
            string outputPath = Path.Combine(
                _DirOutputPath,
                $"ConvertXGI_{DateTime.Now:yy_MM_dd HH_mm_ss}.xgwx"
            );
            File.WriteAllText(outputPath, fileXml, new UTF8Encoding());

        }


        private void LogFinalResults(Tuple<IEnumerable<SymbolInfo>, Dictionary<string, SymbolInfo>> allSymbols)
        {
            //var duplicateAddresses = _lstSymbolXGI.Where(symbol => symbol.SameAddress > 1).OrderBy(symbol => symbol.Address);
            //if (duplicateAddresses.Any())
            //{
            //    duplicateAddresses.ForEach(symbol =>
            //    {
            //        AddLog(ResultCase.Address, ResultData.Failure, "", 
            //            $"영역 충돌 MELSEC: {symbol.GxAddress.PadRight(20)} XGI: {symbol.Address}"
            //        );
            //    });
            //}
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
