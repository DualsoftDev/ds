import * as vscode from 'vscode';

/**
 * Cytoscape 를 이용해서 webview contents 생성
 * @param connections 
 * @returns 
 */
export function getWebviewContentCytoscape(extensionUri: vscode.Uri, webview:vscode.Webview, connections: {source:string, target:string, solid:boolean}[]) {
    console.log('hello');
    function *generateElements()
    {
        const nodes =
            connections
            .flatMap(c => [c.source, c.target])
            .filter((v, i, a) => a.indexOf(v) === i)
            .map(n => {
              return { "data": {"id": n, "label": n}};
            })
        ;

        yield* nodes;

        yield* connections.map(c => {
            const dashStyle = c.solid ? 'solid' : 'dashed';
            return { "data": {
                "id": `${c.source}${c.target}${c.solid}`,
                "source": c.source,
                "target": c.target,
                "line-style": dashStyle}}
            ;});
    }

		// Local path to main script run in the webview
		// And the uri we use to load this script in the webview
    const scriptUris =
      Array.from(
        [ 'cytoscape.min.js', 'cytoscape-undo-redo.js', 'cytoscape.ds.js']
          .map(f => vscode.Uri.joinPath(extensionUri, 'media', f))
          .map(f => webview.asWebviewUri(f)))
      ;

		// Use a nonce to only allow specific scripts to be run
		const nonce = getNonce();

    /*
      <meta http-equiv="Content-Security-Policy" content="default-src self; img-src vscode-resource:; script-src * 'self' 'unsafe-inline' vscode-resource; style-src vscode-resource: 'self' 'unsafe-inline'; "/>
      <meta http-equiv="Content-Security-Policy"
        content="
          default-src 'none';
          img-src vscode-resource: ${webview.cspSource} https:;
          script-src * 'self' 'unsafe-inline' vscode-resource ${webview.cspSource};
          style-src ${webview.cspSource} 'self' 'unsafe-inline';"
      />    
      <meta http-equiv="Content-Security-Policy" content="
        default-src 'none';
        style-src ${webview.cspSource} https: 'self' 'unsafe-inline';
        img-src ${webview.cspSource} https:;
        script-src 'nonce-${nonce}';
      ">

      https://stackoverflow.com/questions/59233387/why-is-js-css-not-loaded-in-my-vsc-extension-webview

      <script src="https://unpkg.com/cytoscape/dist/cytoscape.min.js"></script>
      <script src="https://ivis-at-bilkent.github.io/cytoscape.js-undo-redo/cytoscape-undo-redo.js"></script>

      <script type="module" src="vscode-resource:/src/cytoscape-undo-redo.js"></script>
      <script type="module" src="vscode-resource:/src/cytoscape.min.js"></script>

      <script src="cytoscape-undo-redo.js"></script>

      <!-- 로컬 스크립트 로딩이 안됨 -->
      <script nonce="${nonce}" src="${scriptUris[0]}"></script>
      <script nonce="${nonce}" src="${scriptUris[1]}"></script>

    */

    const els = Array.from(generateElements());
    const elements = JSON.stringify(els);
    console.log('TEXT=', els.join(','));

    return `<!DOCTYPE html>
    <html lang="en">
    
    <head>
      <meta charset="UTF-8">
      <meta name="viewport" content="width=device-width">
      
      <script nonce="${nonce}" src="${scriptUris[0]}"></script>
      <script nonce="${nonce}" src="${scriptUris[1]}"></script>
      
      <title>${elements}</title>

      <style>
        body {
          font-family: helvetica neue, helvetica, liberation sans, arial, sans-serif;
          // font-size: 14px;
        }
        #cy {
          // z-index: 999;
          width: 85%;
          height: 95%;
          position: absolute;
          //float: left;
          /* top: 0px;
          left: 0px; */
        }
    
        /* #cy {
                z-index: 999;
                width: 85%;
                height: 85%;
                float: left;
            } */
    
    
        h1 {
          opacity: 0.5;
          font-size: 1em;
          font-weight: bold;
        }
    
      </style>

      <script nonce="${nonce}" src="${scriptUris[2]}"></script>
      
    </head>
    
    <body>
      <h1>DS demo</h1>
    
      <div id="myDiv">
        <input id="batchButton" type="button" value="Add 'Bob' and delete selected" />
        <b>DEL</b> to delete selected, <b>CTRL+Z</b> to undo, <b>CTRL+Y</b> to redo <br />
      </div>
    
      <div id="cy"></div>
      <div id="undoRedoList">
        <span style="color: darkslateblue; font-weight: bold;">Log</span>
        <div id="undos" style="padding-bottom: 20px;"></div>
      </div>

    </body>
    
    </html>`;
}


function getNonce() {
	let text = '';
	const possible = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
	for (let i = 0; i < 32; i++) {
		text += possible.charAt(Math.floor(Math.random() * possible.length));
	}
	return text;
}

