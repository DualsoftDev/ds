using DevExpress.Charts.Sankey;
using DevExpress.Office.Utils;
using DevExpress.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OPC.DSClient.WinForm.UserControl
{
    /// <summary>
    /// Sankey 다이어그램의 데이터 모델
    /// </summary>
    public class SankeyDsLink
    {
        public OpcDsTag? Source { get; set; } // 출발 노드
        public OpcDsTag? Target { get; set; } // 도착 노드
        public double Weight { get; set; } = 1; // 링크 가중치
    }

    /// <summary>
    /// Sankey Node Layout Utility
    /// </summary>
    public static class LinkNodePoint
    {
        public static List<ISankeyNodeLayoutItem> Nodes { get; set; } = new();

        public static ISankeyNodeLayoutItem? GetNode(Point pt)
        {
            return Nodes.FirstOrDefault(f => f.Bounds.Contains(new DXPoint { X = pt.X, Y = pt.Y }));
        }
    }

    /// <summary>
    /// Sankey Data Utility 클래스
    /// </summary>
    public static class SankeyDataUtil
    {
        /// <summary>
        /// OPC 태그 매니저 데이터를 기반으로 Sankey 다이어그램 데이터를 생성합니다.
        /// </summary>
        /// <param name="opcTagManager">OPC 태그 매니저</param>
        /// <param name="selectTags">선택한 태그 리스트</param>
        /// <returns>SankeyLink 리스트</returns>
        public static List<SankeyDsLink> CreateSankeyData(OpcTagManager opcTagManager, List<OpcDsTag> selectTags)
        {
            if (opcTagManager == null)
                throw new ArgumentNullException(nameof(opcTagManager));

            if (string.IsNullOrWhiteSpace(opcTagManager.OpcJsonText))
                throw new InvalidOperationException("OPC JSON text is empty.");

            var tagDictionary = opcTagManager.OpcFolderTags.ToDictionary(
                folder => folder.QualifiedName,
                folder => folder
            );

            string jsonText = opcTagManager.OpcJsonText;
            var jsonDsSystem = JsonConvert.DeserializeObject<JsonDsSystem>(jsonText);
            if (jsonDsSystem?.Flows == null)
                throw new InvalidOperationException("Invalid OPC data format.");

            var sankeyData = new List<SankeyDsLink>();
            var selectTagNames = selectTags.Select(tag => tag.QualifiedName).ToHashSet();

            foreach (var flow in jsonDsSystem.Flows)
            {
                // Flow의 Edges 처리
                ProcessEdges(flow.Edges, sankeyData, tagDictionary, selectTagNames);

                if (selectTagNames.Any())  //미선택시 Real만 그림
                {
                    foreach (var vertex in flow.Vertices ?? Enumerable.Empty<Vertex>())
                    {
                        if (vertex.Type == "Real")
                        {
                            ProcessEdges(vertex.Edges, sankeyData, tagDictionary, selectTagNames);
                        }
                    }
                }
            }

            return sankeyData;
        }

        /// <summary>
        /// Edge 데이터를 Sankey 데이터로 변환하여 추가합니다.
        /// </summary>
        /// <param name="edges">Edge 리스트</param>
        /// <param name="sankeyData">SankeyLink 리스트</param>
        /// <param name="tagDictionary">태그 사전</param>
        /// <param name="selectTagNames">선택한 태그 이름 리스트</param>
        private static void ProcessEdges(
            IEnumerable<Edge>? edges,
            List<SankeyDsLink> sankeyData,
            Dictionary<string, OpcDsTag> tagDictionary,
            HashSet<string> selectTagNames)
        {
            if (edges == null) return;

            foreach (var edge in edges)
            {
                // 선택된 태그가 없거나, Edge의 Source/Target이 선택된 태그에 포함된 경우 처리
                if (!selectTagNames.Any() || selectTagNames.Contains(edge.Source) || selectTagNames.Contains(edge.Target))
                {
                    if (tagDictionary.TryGetValue(edge.Source, out var sourceTag) &&
                        tagDictionary.TryGetValue(edge.Target, out var targetTag))
                    {
                        sankeyData.Add(new SankeyDsLink
                        {
                            Source = sourceTag,
                            Target = targetTag,
                            Weight = 1
                        });
                    }
                    else
                    {
                        // Edge의 Source 또는 Target 태그를 찾을 수 없을 경우 로그 처리
                        Console.WriteLine($"Warning: Edge source or target not found. Source: {edge.Source}, Target: {edge.Target}");
                    }
                }
            }
        }
    }
}
