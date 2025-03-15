namespace Dual.Common.FS.LSIS.Graph

open System.IO
open System.Diagnostics
open System.Drawing
open System.Windows.Forms

open QuickGraph.Graphviz
open QuickGraph.Graphviz.Dot
open System.Runtime.CompilerServices

[<AutoOpen>]
module Graphviz =
    let graphViz dotContents =
        let tmpDot = Path.Combine(Path.GetTempPath(), "tmp.dot");
        let tmpPng = Path.Combine(Path.GetTempPath(), "tmp.png");
        File.WriteAllText(tmpDot, dotContents);
        let start = 
            ProcessStartInfo(
                FileName = @"C:\Program Files (x86)\Graphviz2.38\bin\dot.exe",
                Arguments = sprintf "-Tpng %s -o %s" tmpDot tmpPng,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true)

        use proc   = Process.Start(start)
        use stdout = proc.StandardOutput
        use stderr = proc.StandardError
        let result = stdout.ReadToEnd();

        let error = stderr.ReadToEnd();
        //if (!string.IsNullOrEmpty(error))
        //{
        //    var msg = $"Error while converting dot file {tmpDot}:\r\n{error}";
        //    Trace.WriteLine(msg);
        //}

        // Image.FromFile() locks file.
        new Bitmap(tmpPng)

    let graphVizForm dotContents =
        let formBitmap bitmap =
            let panel = new Panel(Dock = DockStyle.Fill)
            let pb    = new PictureBox(Dock = DockStyle.Fill)
            pb.Image <- bitmap
            panel.Controls.Add(pb)
            let form  = new Form(Width = 300, Height=300)
            form.Controls.Add(panel)
            form
        graphViz dotContents |> formBitmap


    let internal defaultClusterFormatter  = fun (args:FormatClusterEventArgs<'v, 'e>) -> ()
    let internal defaultVetexFormatter    = fun (args:FormatVertexEventArgs<'v>) -> ()
    let internal defaultEdgeFormatter     = fun (args:FormatEdgeEventArgs<'v, 'e>) -> ()

    let internal createDotEngine() =
        { new IDotEngine with
            member x.Run(imageType:GraphvizImageType, dot:string, outputFileName:string) =
                use writer = new StreamWriter(outputFileName)
                writer.Write(dot)
                Path.GetFileName(outputFileName) }

    // https://en.programqa.com/question/25569164/
    // https://stackoverflow.com/questions/32275246/c-sharp-drawing-a-graph-using-quickgraph-and-graphviz
    /// Graph 로부터 생성한 GraphvizAlgorithm 으로부터 dot graph text content 생성
    let getDotGraphText (ga:GraphvizAlgorithm<'v, 'e>) vertexFormatter edgeFormatter clusterFormatter =
        ga.FormatVertex.Add(vertexFormatter)
        ga.FormatEdge.Add(edgeFormatter)
        ga.FormatCluster.Add(clusterFormatter)

        ga.Generate(createDotEngine(), "Graph.dot") |> ignore
        File.ReadAllText("Graph.dot")

        (* 호출 예제
            let vetexFormatter    = fun (args:FormatVertexEventArgs<Vertex>)         -> args.VertexFormatter.Label <- args.Vertex.Name
            let edgeFormatter     = fun (args:FormatEdgeEventArgs<Vertex, GEdge>)    -> args.EdgeFormatter.Style   <- GraphvizEdgeStyle.Solid
            let clusterFormatter  = fun (args:FormatClusterEventArgs<Vertex, GEdge>) -> args.GraphFormat.Label     <- "XXXCluster"      // args.Cluster 를 실제 graph type 으로 cast 해서 정보 추출
            let ga = GraphvizAlgorithm<Vertex, GEdge>(g)
            getDotGraphText ga vetexFormatter edgeFormatter clusterFormatter
        *)
    let getDotGraphText2 (ga:GraphvizAlgorithm<'v, 'e>) vertexFormatter edgeFormatter clusterFormatter =
        ga.FormatVertex.AddHandler(vertexFormatter)
        ga.FormatEdge.AddHandler(edgeFormatter)
        ga.FormatCluster.AddHandler(clusterFormatter)

        ga.Generate(createDotEngine(), "Graph.dot") |> ignore
        File.ReadAllText("Graph.dot")

    let getDotGraphTextSimple (ga:GraphvizAlgorithm<'v, 'e>) =
        ga.Generate(createDotEngine(), "Graph.dot") |> ignore
        File.ReadAllText("Graph.dot")

open Graphviz
[<Extension>] // type GraphvizExt =
type GraphvizExt =
    [<Extension>]
    static member GetDotGraphText(ga, vertexFormatter, edgeFormatter, clusterFormatter) =
        getDotGraphText ga vertexFormatter edgeFormatter clusterFormatter

    [<Extension>]
    static member GetDotGraphText(ga, vertexFormatter, edgeFormatter) =
        getDotGraphText ga vertexFormatter edgeFormatter defaultClusterFormatter

    [<Extension>]
    static member GetDotGraphText(ga, vertexFormatter, edgeFormatter, clusterFormatter) =
        getDotGraphText2 ga vertexFormatter edgeFormatter clusterFormatter

    [<Extension>] static member GetDotGraphText ga = getDotGraphTextSimple ga

    [<Extension>] static member GetDotGraphBitmap dotText = graphViz dotText
    [<Extension>] static member GetDotGraphForm dotText   = graphVizForm dotText
