using DevExpress.Charts.Sankey;
using DevExpress.Utils;
using DevExpress.XtraCharts.Sankey;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace OPC.DSClient.WinForm.UserControl
{
    /// <summary>
    /// Custom layout algorithm for arranging nodes in a Sankey diagram.
    /// </summary>
    internal class MyLayoutAlgorithm : SankeyLayoutAlgorithmBase
    {

        /// <summary>
        /// Calculates and assigns the bounds (position and size) for each node in the diagram.
        /// </summary>
        /// <param name="nodes">The collection of nodes to arrange.</param>
        /// <param name="rect">The rectangle area within which to arrange the nodes.</param>
        public override void CalculateNodeBounds(IEnumerable<ISankeyNodeLayoutItem> nodes, DXRectangle bounds)
        {
            void SpreadLevelIndex(ISankeyNodeLayoutItem node, int startingLevelIndex = 0)
            {
                node.LevelIndex = startingLevelIndex;
                if (node.OutputLinks == null)
                    return;
                foreach (var outputLink in node.OutputLinks)
                    SpreadLevelIndex(outputLink.Target, startingLevelIndex + 1);
            }

            foreach (var node in nodes)
                if (node.InputLinks == null || node.InputLinks.Count == 0)
                    SpreadLevelIndex(node);
            int nodeWidth = bounds.Width / 60;
            int nodeHeight = bounds.Height / 40;
            int levelCount = nodes.Max(node => node.LevelIndex) + 1;
            int spaceBetweenLevels = (bounds.Width - nodeWidth) / (levelCount - 1);
            int maxNodeCountInALevel = nodes.GroupBy(node => node.LevelIndex).Max(group => group.Count());
            int spaceBetweenNodes = (bounds.Height - nodeHeight) / (maxNodeCountInALevel>1 ? (maxNodeCountInALevel - 1) : 1);
            Dictionary<int, int> levelNodeCountPairs = new Dictionary<int, int>();
            foreach (var node in nodes)
            {
                if (!levelNodeCountPairs.TryGetValue(node.LevelIndex, out int nodeCount))
                    levelNodeCountPairs.Add(node.LevelIndex, 0);
                levelNodeCountPairs[node.LevelIndex]++;
                node.Bounds = new DevExpress.Utils.DXRectangle(bounds.Left + node.LevelIndex * spaceBetweenLevels, bounds.Top + nodeCount * spaceBetweenNodes, nodeWidth, nodeHeight);
            }

            LinkNodePoint.Nodes.Clear();
            LinkNodePoint.Nodes.AddRange(nodes);
        }
    }
}
