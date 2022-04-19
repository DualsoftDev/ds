// <script src="https://d3js.org/d3.v4.js"></script>


/**
 * D3 를 이용해서 webview contents 생성
 * @param connections 
        const edge = (causal.op == '>') ? 'resolved' : 'suit';
        return `{source: "${causal.left}", target: "${causal.right}", type: "${edge}"}`;
 * @returns 
 */
export function getWebviewContentD3(connections: {source:string, target:string, solid:boolean}[]) {
    let text = connections.map(c => {
        const type = c.solid ? 'resolved' : 'suit';
        return `{source: '${c.source}', target: '${c.target}', type: '${type}'}`})
        .join(',')
    ;
    text = `[ ${text} ]`;
    console.log('text=', text);
    
    return `<!DOCTYPE html>
<html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>D3</title>
    </head>
<meta charset="utf-8">
<style>

.link {
  fill: none;
  stroke: #666;
  stroke-width: 1.5px;
}

#licensing {
  fill: green;
}

.link.licensing {
  stroke: green;
}

.link.resolved {
  stroke-dasharray: 0,2 1;
}

circle {
  fill: #ccc;
  stroke: #333;
  stroke-width: 1.5px;
}

text {
  font: 10px sans-serif;
  pointer-events: none;
  text-shadow: 0 1px 0 #fff, 1px 0 0 #fff, 0 -1px 0 #fff, -1px 0 0 #fff;
}

</style>
<body style="background-color:white;">
    <script src="https://d3js.org/d3.v3.min.js"></script>
    <script>
        console.log('I m d3');
        // http://blog.thomsonreuters.com/index.php/mobile-patent-suits-graphic-of-the-day/
        var links = ${text}
        var nodes = {};

        // Compute the distinct nodes from the links.
        links.forEach(function(link) {
        link.source = nodes[link.source] || (nodes[link.source] = {name: link.source});
        link.target = nodes[link.target] || (nodes[link.target] = {name: link.target});
        });

        var width = 960,
            height = 500;

        var force = d3.layout.force()
            .nodes(d3.values(nodes))
            .links(links)
            .size([width, height])
            .linkDistance(60)
            .charge(-300)
            .on("tick", tick)
            .start();

        var svg = d3.select("body").append("svg")
            .attr("width", width)
            .attr("height", height);

        // Per-type markers, as they don't inherit styles.
        svg.append("defs").selectAll("marker")
            .data(["suit", "licensing", "resolved"])
        .enter().append("marker")
            .attr("id", function(d) { return d; })
            .attr("viewBox", "0 -5 10 10")
            .attr("refX", 15)
            .attr("refY", -1.5)
            .attr("markerWidth", 6)
            .attr("markerHeight", 6)
            .attr("orient", "auto")
        .append("path")
            .attr("d", "M0,-5L10,0L0,5");

        var path = svg.append("g").selectAll("path")
            .data(force.links())
        .enter().append("path")
            .attr("class", function(d) { return "link " + d.type; })
            .attr("marker-end", function(d) { return "url(#" + d.type + ")"; });

        var circle = svg.append("g").selectAll("circle")
            .data(force.nodes())
        .enter().append("circle")
            .attr("r", 10)    // 6
            .call(force.drag);

        var text = svg.append("g").selectAll("text")
            .data(force.nodes())
        .enter().append("text")
            .attr("x", 8)
            .attr("y", ".31em")
            .text(function(d) { return d.name; });

        // Use elliptical arc path segments to doubly-encode directionality.
        function tick() {
        path.attr("d", linkArc);
        circle.attr("transform", transform);
        text.attr("transform", transform);
        }

        function linkArc(d) {
        var dx = d.target.x - d.source.x,
            dy = d.target.y - d.source.y,
            dr = Math.sqrt(dx * dx + dy * dy);
        return "M" + d.source.x + "," + d.source.y + "A" + dr + "," + dr + " 0 0,1 " + d.target.x + "," + d.target.y;
        }

        function transform(d) {
        return "translate(" + d.x + "," + d.y + ")";
        }

    </script>

</body>

</html>
`;
}