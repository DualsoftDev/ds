using DevExpress.Mvvm.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OPC.DSClient.WinForm
{
    public abstract class DsJsonBase
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public OpcDsTag OpcDsTag { get; set; } = new();
        public List<OpcDsTag> SubOpcDsTags { get; set; } = new();
    }

    public class DsSystemJson : DsJsonBase
    {
        public List<FlowJson> Flows { get; set; } = new();
    }

    public class FlowJson : DsJsonBase
    {
        public List<VertexJson> Vertices { get; set; } = new();
        public List<EdgeJson> Edges { get; set; } = new();
        public List<AliasJson> Aliases { get; set; } = new();
    }

    public class VertexJson : DsJsonBase
    {
        public List<VertexJson> Vertices { get; set; } = new();
        public List<TaskDevJson> TaskDevs { get; set; } = new();
        public List<EdgeJson> Edges { get; set; } = new();
    }

    public class TaskDevJson : DsJsonBase { }

    public class EdgeJson
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }

    public class AliasJson
    {
        public string AliasKey { get; set; } = string.Empty;
        public List<string> Texts { get; set; } = new();
    }

    public static class DsSystemJsonUtils
    {
        /// <summary>
        /// JSON 텍스트를 DsSystemJson 객체로 로드
        /// </summary>
        public static DsSystemJson LoadJson(string jsonText) =>
            JsonConvert.DeserializeObject<DsSystemJson>(jsonText)
            ?? throw new InvalidOperationException("Invalid OPC data format.");

        /// <summary>
        /// OPC 태그를 DsJsonBase의 OpcDsTags에 추가
        /// </summary>
        private static void MapOpcTags(DsJsonBase jsonBase, Dictionary<string, OpcDsTag> tagDictionary, IEnumerable<OpcDsTag> opcTags, IEnumerable<OpcDsTag> opcFolders)
        {
            if (tagDictionary.TryGetValue(jsonBase.Name, out var folderTag))
            {
                var matchedSubTags = opcTags.Where(tag => tag.ParentPath == folderTag.Path);
                jsonBase.SubOpcDsTags.AddRange(matchedSubTags); 
                var matchedTag = opcFolders.First(tag => tag.Path == folderTag.Path);
                jsonBase.OpcDsTag = matchedTag;
            }
        }

        /// <summary>
        /// OPC DS 태그 데이터를 JSON 구조와 동기화
        /// </summary>
        public static void UpdateOpcDSTags(OpcTagManager opcTagManager)
        {
            if (opcTagManager?.DsSystemJson == null)
                throw new ArgumentNullException(nameof(opcTagManager), "DsSystemJson cannot be null.");

            var tagDictionary = opcTagManager.OpcFolderTags.ToDictionary(tag => tag.QualifiedName);
            var opcTags = opcTagManager.OpcTags;
            var opcFolders = opcTagManager.OpcFolderTags;
            var systemJson = opcTagManager.DsSystemJson;

            // 시스템 레벨 태그 매핑
            MapOpcTags(systemJson, tagDictionary, opcTags, opcFolders);

            foreach (var flow in systemJson.Flows)
            {
                // 플로우 레벨 태그 매핑
                MapOpcTags(flow, tagDictionary, opcTags, opcFolders);


                foreach (var vertex in flow.Vertices)
                {
                    // Vertex 레벨 태그 매핑
                    MapOpcTags(vertex, tagDictionary, opcTags, opcFolders);

                    foreach (var subVertex in vertex.Vertices)
                    {
                        // Sub-Vertex 태그 매핑
                        MapOpcTags(subVertex, tagDictionary, opcTags, opcFolders);

                        foreach (var taskDev in subVertex.TaskDevs)
                        {
                            // TaskDev 태그 매핑
                            MapOpcTags(taskDev, tagDictionary, opcTags, opcFolders);
                        } 
                    }
                }
            }
        }

        public static List<DsJsonBase> GetAllDsJsons(DsSystemJson dsSystemJson)
        {
            var dsJsons = new List<DsJsonBase> { dsSystemJson };
            dsJsons.AddRange(dsSystemJson.Flows);
            foreach (var flow in dsSystemJson.Flows)
            {
                dsJsons.AddRange(flow.Vertices);
                foreach (var vertex in flow.Vertices)
                {
                    dsJsons.AddRange(vertex.Vertices);
                    foreach (var vertexSub in vertex.Vertices)
                    {
                        dsJsons.AddRange(vertexSub.TaskDevs);
                    }
                }
            }
            return dsJsons;
        }
    }
}
