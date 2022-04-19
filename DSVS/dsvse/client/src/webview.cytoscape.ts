/**
 * Cytoscape 를 이용해서 webview contents 생성
 * @param connections 
 * @returns 
 */
export function getWebviewContentCytoscape(connections: {source:string, target:string, solid:boolean}[]) {
    let elements = connections.map(c => {
        const type = c.solid ? 'resolved' : 'suit';
        return `{source: '${c.source}', target: '${c.target}', type: '${type}'}`;
    }).join(',')
    ;
    function *generateElements()
    {
        const nodes =
            connections
            .flatMap(c => [c.source, c.target])
            .filter((v, i, a) => a.indexOf(v) === i)
        ;
        yield nodes.map(n => `{ data: {id: '${n}', label: '${n}'}}`);

        yield connections.map(c =>
`{ data: {
    id: '${c.source}${c.target}',
    source: '${c.source}',
    target: '${c.target}'}}
`);
    }

    const els = Array.from(generateElements()).join(",");
    elements = `[ ${els} ]`;
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
        var cy = cytoscape({
        wheelSensitivity: 0.1,
        container: document.getElementById('cy'),
        elements: ${elements},
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
                    'text-outline-color': 'data(background_color)',
            
                    // 'text-outline-color': 'orange'
                    'text-outline-width': 2,
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
        ]});

    </script>
</body>
</html>
`;
}