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
        public object Sensor => OpcDsTag?.Value;
        public int Count => OpcDsTag?.Count ?? 0;
        public float MovingAVG => OpcDsTag?.MovingAVG ?? 0;
        public float MovingSTD => OpcDsTag?.MovingSTD ?? 0;
        public float ActiveTime => OpcDsTag?.ActiveTime ?? 0;
        public float MovingTime => OpcDsTag?.MovingTime ?? 0;
        public float WaitingTime => OpcDsTag?.WaitingTime ?? 0;

        // 비율 계산: WaitingTime / ActiveTime
        public float Ratio => ActiveTime > 0 ? (WaitingTime / ActiveTime) * 100 : 0;


        public OpcDsTag OpcDsTag { get; set; }
    }

    public static class DataGridUtil
    {
        public static BindingList<GridItem> GetDataSource(OpcTagManager opcTagManager)
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
    }
}
