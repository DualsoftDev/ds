using DevExpress.Office.Utils;
using DevExpress.Utils.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OPC.DSClient.WinForm.UserControl
{
    public static class CommonUIManagerUtils
    {
        public static Dictionary<string, DsUnit> GetPathMap(OpcTagManager opcTagManager, List<DsUnit> dsUnits)
        {
            // OPC 데이터 매니저의 폴더 태그를 DsUnit로 매핑
            var dicPathMap = opcTagManager.OpcFolderTags.ToDictionary(
                folder => folder.Path,
                folder => new DsUnit
                {
                    Label = folder.Name.TrimStart('[').TrimEnd(']'),
                    OpcDsTag = folder,
                    Color = Color.Gray,
                    DsUnits = new List<DsUnit>(),
                    Level = CalculateLevel(folder.Path) // 레벨 계산 및 설정
                });

            var flowList = new List<DsUnit>();

            foreach (var folder in opcTagManager.OpcFolderTags)
            {
                // 시스템 키를 포함하는 폴더는 건너뜀
                var dsSystemKey = " tags";
                if (folder.Name.EndsWith(dsSystemKey)) continue;

                // 폴더 노드 추가
                if (dicPathMap.TryGetValue(folder.Path, out var current))
                {
                    if (dicPathMap.TryGetValue(folder.ParentPath, out var parent))
                        parent.DsUnits.Add(current);
                    else
                        flowList.Add(current);
                }
            }
          
            dsUnits.AddRange(flowList);

            return dicPathMap;
        }

        public static Dictionary<string, DsUnit> GetPathMapWithTags(OpcTagManager opcTagManager, List<DsUnit> dsUnits)
        {
            var flowList = new List<DsUnit>();
            // OPC 데이터 매니저의 폴더 태그를 DsUnit로 매핑
            var dicPathMap = GetPathMap(opcTagManager, dsUnits);

            // 자식이 없는 최종 노드를 처리
            var endNodePaths = dicPathMap.Where(w => w.Value.DsUnits.Any()) // 자식이 있는 노드
                                         .Where(w => !w.Value.DsUnits.SelectMany(s => s.DsUnits).Any()) // 자식의 자식이 없는 노드
                                         .Select(s => s.Key).ToList();

            foreach (var endNodePath in endNodePaths)
            {
                var opcItems = opcTagManager.OpcTags.Where(w => w.ParentPath == endNodePath).ToList();
                var lstDsUnits = opcItems.Select(s => new DsUnit
                {
                    Label = s.Name,
                    OpcDsTag = s,
                    Color = Color.Gray,
                    DsUnits = new List<DsUnit>(),
                    Level = CalculateLevel(endNodePath) + 1 // 레벨 계산 및 설정
                }).ToArray();

                dicPathMap[endNodePath].DsUnits.AddRange(lstDsUnits);
                lstDsUnits.ForEach(f => dicPathMap.Add(f.Label, f));
            }

            // 자식 노드의 수를 Value로 설정
            dicPathMap.Where(w => w.Value.DsUnits.Count > 0)
                      .ForEach(f => f.Value.Area = f.Value.DsUnits.Count);

            dsUnits.AddRange(flowList);

            return dicPathMap;
        }

        /// <summary>
        /// 경로를 기반으로 레벨을 계산합니다.
        /// </summary>
        /// <param name="path">현재 노드의 경로</param>
        /// <returns>계산된 레벨 값</returns>
        private static int CalculateLevel(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;

            return path.Count(c => c == '/'); // 경로의 '/' 개수를 세어 레벨로 사용
        }

        public static void FilterNoChildFolderUnits(List<DsUnit> units)
        {
            if (units == null || units.Count == 0) return;

            // 자식들부터 먼저 처리
            foreach (var unit in units)
            {
                if (unit.DsUnits != null && unit.DsUnits.Count > 0)
                {
                    FilterNoChildFolderUnits(unit.DsUnits);
                }
            }

            // 현재 리스트에서 조건을 만족하는 항목 제거
            units.RemoveAll(r => 
                    (r.Level == 2 && r.DsUnits.Count == 0)  //flow/work
                    || r.Level == 4                         //flow/work/call/taskDev
                    );
        }
    }
}
