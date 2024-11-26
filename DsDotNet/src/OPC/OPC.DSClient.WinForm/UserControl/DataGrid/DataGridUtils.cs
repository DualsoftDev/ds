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
        public object Sensor => OpcDsTag.Value;
        public double Mean => OpcDsTag.Mean;
        public double Variance => OpcDsTag.Variance;
        public int Count => OpcDsTag.Count;
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
