using System;
using System.Collections.Generic;
using System.Linq;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Import.Office.ViewModule;

namespace Diagram.View.MSAGL;

public class ViewVertex
{
    private List<ViewNode> _nodes;
    public Vertex Vertex { get; set; }
    public void SetViewNodes(List<ViewNode> nodes) => _nodes = nodes;

    public IEnumerable<ViewNode> DisplayNodes =>
        _nodes.Where(w => w.CoreVertex.Value is not Alias
                          || w.CoreVertex.Value == Vertex);

    public ViewNode FlowNode { get; set; } //UcViewNode
    public Status4 Status { get; set; }
    public List<DsTask> DsTasks { get; set; }
    public bool IsError { get; set; }
    public bool LampInput { get; set; }
    public bool LampOutput { get; set; }
    public string ErrorText { get; set; }
}