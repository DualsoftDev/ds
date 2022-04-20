/**
 * Cytoscape 를 이용해서 webview contents 생성
 * @param connections 
 * @returns 
 */
export function getWebviewContentCytoscape(connections: {source:string, target:string, solid:boolean}[]) {
    function *generateElements()
    {
        const nodes =
            connections
            .flatMap(c => [c.source, c.target])
            .filter((v, i, a) => a.indexOf(v) === i)
        ;
        yield nodes.map(n => `{ data: {id: '${n}', label: '${n}'}}`);

        yield connections.map(c => {
            const dashStyle = c.solid ? 'solid' : 'dashed';
            return `{ data: {
                id: '${c.source}${c.target}${c.solid}',
                source: '${c.source}',
                target: '${c.target}',
                'line-style': '${dashStyle}' }}
            `;});
    }

    const els = Array.from(generateElements()).join(",");
    const elements = `[ ${els} ]`;
    console.log('TEXT=', els);

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">

    <script src="https://unpkg.com/cytoscape/dist/cytoscape.min.js"></script>
    <title>Document</title>
</head>
<style>
    #cy {
        width: 100%;
        height: 100%;
        position: absolute;
        top: 0px;
        left: 0px;
    }
</style>

<body>
    <div id="cy"></div>
    <script>
        let cy = cytoscape({
            wheelSensitivity: 0.1,
            container: document.getElementById('cy'),
            elements: ${elements},
            layout: {
                name: 'cose',   //circle, cose, grid
            },
            style: [
                {
                    selector: 'node',
                    style: {
                        'shape': 'round-rectangle',
                        'color':"white",        // text color
                
                        "border-width": 2,
                        "border-color": "white",
                        "border-style": "solid",   //"dotted",
                        
                        
                        // 'background-color': 'data(background_color)',
                        //'text-outline-color': 'data(background_color)',
                
                        // 'text-outline-color': 'orange'
                        'text-outline-width': 2,
                        'text-opacity': 0.5,
                        'label': 'data(id)',
                        'font-size' : '25px',
                        'background-color': 'green'
                    }
                },
                {
                    selector: 'edge',
                    style: {
                        'curve-style': 'bezier',
                        'target-arrow-shape': 'triangle'
                    }
                }
            ]
        });

        cy.edges()
            .filter((e, i) => e.isEdge() && e.data('line-style') == 'dashed')
            .style('line-style', 'dashed')
            .update()
            ;

        // let options = {
        //     name: 'circle',
            
        //     fit: true, // whether to fit the viewport to the graph
        //     padding: 30, // the padding on fit
        //     boundingBox: undefined, // constrain layout bounds; { x1, y1, x2, y2 } or { x1, y1, w, h }
        //     avoidOverlap: true, // prevents node overlap, may overflow boundingBox and radius if not enough space
        //     nodeDimensionsIncludeLabels: false, // Excludes the label when calculating node bounding boxes for the layout algorithm
        //     spacingFactor: undefined, // Applies a multiplicative factor (>0) to expand or compress the overall area that the nodes take up
        //     radius: undefined, // the radius of the circle
        //     startAngle: 3 / 2 * Math.PI, // where nodes start in radians
        //     sweep: undefined, // how many radians should be between the first and last node (defaults to full circle)
        //     clockwise: true, // whether the layout should go clockwise (true) or counterclockwise/anticlockwise (false)
        //     sort: undefined, // a sorting function to order the nodes; e.g. function(a, b){ return a.data('weight') - b.data('weight') }
        //     animate: false, // whether to transition the node positions
        //     animationDuration: 500, // duration of animation in ms if enabled
        //     animationEasing: undefined, // easing of animation if enabled
        //     animateFilter: function ( node, i ){ return true; }, // a function that determines whether the node should be animated.  All nodes animated by default on animate enabled.  Non-animated nodes are positioned immediately when the layout starts
        //     ready: undefined, // callback on layoutready
        //     stop: undefined, // callback on layoutstop
        //     transform: function (node, position ){ return position; } // transform a given node position. Useful for changing flow direction in discrete layouts 
            
        //     };
            
        // cy.layout( options );


    </script>
</body>
</html>
`;
}