/**
 * Cytoscape 를 이용해서 webview contents 생성
 * @param connections 
 * @returns 
 */
export function getWebviewContentCytoscape(connections: {source:string, target:string, solid:boolean}[]) {
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
    <script nonce="2726c7f26c">
        var cy = cytoscape({
        container: document.getElementById('cy'),
        elements: [
            { data: { id: 'a' } },
            { data: { id: 'b' } },
            {
            data: {
                id: 'ab',
                source: 'a',
                target: 'b'
            }
            }],
            style: [
                {
                    selector: 'node',
                    style: {
                        shape: 'hexagon',
                        'background-color': 'red'
                    }
                }]      
        });

    </script>
</body>
</html>
`;
}