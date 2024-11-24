using DevExpress.Office.Utils;
using DevExpress.Utils.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OPC.DSClient.WinForm.UserControl
{
    public static class CommonUIManager
    {
        public static Dictionary<string, DsUnit> GetPathMap(OpcTagManager opcTagManager, List<DsUnit> dsUnits)
        {
            // OPC 데이터 매니저의 폴더 태그를 DsUnit로 매핑
            var dicPathMap = opcTagManager.OpcFolderTags.ToDictionary(
                folder => folder.Path,
                folder => new DsUnit
                {
                    Label = folder.Name.TrimStart('[').TrimEnd(']'),
                    Value = 1,
                    Color = Color.Gray,
                    DsUnits = new List<DsUnit>()
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
            // 자식 노드의 수를 Value로 설정
            dicPathMap.Where(w => w.Value.DsUnits.Count > 0)
                      .ForEach(f => f.Value.Value = f.Value.DsUnits.Count);

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
                    Value = 1,
                    Color = Color.Gray,
                    DsUnits = new List<DsUnit>()
                }).ToArray();

                dicPathMap[endNodePath].DsUnits.AddRange(lstDsUnits);
                lstDsUnits.ForEach(f => dicPathMap.Add(f.Label, f));
            }

            // 자식 노드의 수를 Value로 설정
            dicPathMap.Where(w => w.Value.DsUnits.Count > 0)
                      .ForEach(f => f.Value.Value = f.Value.DsUnits.Count);

            dsUnits.AddRange(flowList);

            return dicPathMap;
        }
    }
}
