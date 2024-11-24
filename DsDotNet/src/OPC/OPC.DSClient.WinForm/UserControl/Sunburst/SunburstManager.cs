using DevExpress.Utils.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OPC.DSClient.WinForm.UserControl
{
    public static class SunburstManager
    {
        public static Dictionary<string, SunInfo> GetPathMap(OpcTagManager opcTagManager, List<SunInfo> SunInfos)
        {
            // OPC 데이터 매니저의 폴더 태그를 SunInfo로 매핑
            var dicPathMap = opcTagManager.OpcFolderTags.ToDictionary(
                folder => folder.Path,
                folder => new SunInfo
                {
                    Label = folder.Name.TrimStart('[').TrimEnd(']'),
                    Value = 1,
                    Color = Color.Gray,
                    SunItems = new List<SunInfo>()
                });

            var flowList = new List<SunInfo>();

            foreach (var folder in opcTagManager.OpcFolderTags)
            {
                // 시스템 키를 포함하는 폴더는 건너뜀
                var dsSystemKey = " tags";
                if (folder.Name.EndsWith(dsSystemKey)) continue;

                // 폴더 노드 추가
                if (dicPathMap.TryGetValue(folder.Path, out var current))
                {
                    if (dicPathMap.TryGetValue(folder.ParentPath, out var parent))
                        parent.SunItems.Add(current);
                    else
                        flowList.Add(current);
                }
            }

            // 자식이 없는 최종 노드를 처리
            var endNodePaths = dicPathMap.Where(w => w.Value.SunItems.Any()) // 자식이 있는 노드
                                         .Where(w => !w.Value.SunItems.SelectMany(s => s.SunItems).Any()) // 자식의 자식이 없는 노드
                                         .Select(s => s.Key).ToList();

            foreach (var endNodePath in endNodePaths)
            {
                var opcItems = opcTagManager.OpcTags.Where(w => w.ParentPath == endNodePath).ToList();
                var sunItems = opcItems.Select(s => new SunInfo
                {
                    Label = s.Name,
                    Value = 1,
                    Color = Color.Gray,
                    SunItems = new List<SunInfo>()
                }).ToArray();

                dicPathMap[endNodePath].SunItems.AddRange(sunItems);
                sunItems.ForEach(f => dicPathMap.Add(f.Label, f));
            }

            // 자식 노드의 수를 Value로 설정
            dicPathMap.Where(w => w.Value.SunItems.Count > 0)
                      .ForEach(f => f.Value.Value = f.Value.SunItems.Count);

            SunInfos.AddRange(flowList);

            return dicPathMap;
        }
    }
}
