using DevExpress.Charts.Sankey;
using DevExpress.Office.Utils;
using DevExpress.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace OPC.DSClient.WinForm.UserControl
{
    public class GridItem
    {
        public string Name { get; set; } = string.Empty;
        public object Finish => OpcDsTag?.Value;
        public int Count => OpcDsTag?.Count ?? 0;
        public float MovingAVG => OpcDsTag?.MovingAVG ?? 0;
        public float MovingSTD => OpcDsTag?.MovingSTD ?? 0;
        public uint ActiveTime => OpcDsTag?.ActiveDuration ?? 0;
        public uint MovingTime => OpcDsTag?.MovingDuration ?? 0;
        public uint WaitingTime => OpcDsTag?.WaitingDuration ?? 0;
        public List<uint> MovingTimes => OpcDsTag.MovingTimes;

        // 비율 계산: WaitingTime / ActiveTime
        public float Ratio => ActiveTime > 0 ? Convert.ToSingle(WaitingTime) / ActiveTime * 100.0f  : 0.0f; 

        public OpcDsTag OpcDsTag { get; set; }
    }

    public static class DataGridUtil
    {
        public static BindingList<GridItem> GetDataSourceIO(OpcTagManager opcTagManager)
        {
            var gridItems = opcTagManager.OpcTags
                .Where(tag => tag.TagKindDefinition == "actionIn" || tag.TagKindDefinition == "actionOut")
                .Select(tag => new GridItem
                {
                    Name = tag.Name, 
                    OpcDsTag = tag,
                }).ToList();

            return new BindingList<GridItem>(gridItems);
        }
        public static BindingList<GridItem> GetDataSourceFlow(OpcTagManager opcTagManager)
        {
            var gridItems = opcTagManager.DsSystemJson.Flows
                .SelectMany(flow => flow.Vertices
                                        .Where(w => w.Type == "Real")
                                        .Select(s=>s.OpcDsTag))
                .Select(tag => new GridItem
                {
                    Name = tag.QualifiedName,
                    OpcDsTag = tag,
                }).ToList();

            return new BindingList<GridItem>(gridItems);
        }
    }
}
