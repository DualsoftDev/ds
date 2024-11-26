using DevExpress.XtraCharts.Sankey;
using System.Drawing;

namespace OPC.DSClient.WinForm.UserControl
{
    /// <summary>
    /// Custom colorizer for Sankey diagrams.
    /// </summary>
    public class UcColorizer : ISankeyColorizer
    {
        /// <summary>
        /// Determines the color of the source part of a Sankey link.
        /// </summary>
        /// <param name="link">The Sankey link to colorize.</param>
        /// <returns>The color for the source part of the link.</returns>
        public Color GetLinkSourceColor(SankeyLink link)
        {
            SankeyNode s = link.SourceNode;

            if (s.Tag is OpcDsTag t && t.Value is bool on)
            {
                return getLinkColor(on);
            }
            else
            {
                return getLinkColor(false);
            }
        }

        /// <summary>
        /// Determines the color of the target part of a Sankey link.
        /// </summary>
        /// <param name="link">The Sankey link to colorize.</param>
        /// <returns>The color for the target part of the link.</returns>
        public Color GetLinkTargetColor(SankeyLink link)
        {
            SankeyNode s = link.TargetNode;

            if (s.Tag is OpcDsTag t && t.Value is bool on)
            {
                return getLinkColor(on);
            }
            else
            {
                return getLinkColor(false);
            }
        } 

        /// <summary>
        /// Determines the color of a Sankey node.
        /// </summary>
        /// <param name="info">The Sankey node to colorize.</param>
        /// <returns>The color for the node.</returns>
        public Color GetNodeColor(SankeyNode info)
        {
            if(info.Tag is OpcDsTag t && t.Value is bool on)
            {
                return getNodeColor(on);
            }
            else
            {
                return getNodeColor(false);
            }
        }

        /// <summary>
        /// Provides a color based on the 'isEnable' flag for links.
        /// </summary>
        /// <param name="isEnable">Indicates if the link is enabled.</param>
        /// <returns>Red if enabled, otherwise Green.</returns>
        private Color getLinkColor(bool isError)
        {
            return isError ? SankeyConfig.LinkColorError : SankeyConfig.LinkColorNormal;
        }

        Color ISankeyColorizer.GetLinkSourceColor(DevExpress.XtraCharts.Sankey.SankeyLink link)
        {
            return  SankeyConfig.LinkColorNormal;
        }

        Color ISankeyColorizer.GetLinkTargetColor(DevExpress.XtraCharts.Sankey.SankeyLink link)
        {
            return  SankeyConfig.LinkColorError;
        }

        /// <summary>
        /// Provides a color based on the 'isOn' flag for nodes.
        /// </summary>
        /// <param name="isOn">Indicates if the node is on.</param>
        /// <returns>Blue if on, otherwise DarkGray.</returns>
        private Color getNodeColor(bool isOn)
        {
            return isOn ? SankeyConfig.NodeOn : SankeyConfig.NodeOff;
        }
    }


    public static class SankeyConfig
    {
        /// <summary>
        /// The color used for indicating error links.
        /// </summary>
        public static Color LinkColorError = Color.Red;

        /// <summary>
        /// The color used for indicating normal links.
        /// </summary>
        public static Color LinkColorNormal = Color.Green;

        /// <summary>
        /// The color used for nodes that are off.
        /// </summary>
        public static Color NodeOff = Color.DarkGray;

        /// <summary>
        /// The color used for nodes that are on.
        /// </summary>
        public static Color NodeOn = Color.Blue;

        /// <summary>
        /// The color used for input labels.
        /// </summary>
        public static Color LabelInput = Color.SteelBlue;

        /// <summary>
        /// The color used for output labels.
        /// </summary>
        public static Color LabelOutput = Color.IndianRed;

        /// <summary>
        /// The color used for labels that are both input and output.
        /// </summary>
        public static Color LabelInputOutput = Color.DarkGreen;

        /// <summary>
        /// The font used for labels.
        /// </summary>
        public static Font LabelFont = new Font("Tahoma", 10);
    }
}
