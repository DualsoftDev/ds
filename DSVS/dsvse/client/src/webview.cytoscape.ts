/*
 * Issues
  1. extension <--> webview 간 통신 (message passing)
  1. webview 생성시의 security policy.
    - local resource 및 script 를 막아 놓았으므로, 이를 풀고 수행하여야 한다.
      - local script loading 을 허용하는 경우(nonce 사용), inline script 등은 사용할 수 없으므로 argument 를 pass 하여야 한다.
      - 일반적으로 사용하는 script 호출의 argument 방식은 동작하지 않는다.  e.g http://....prog?arg=value
        - HTML document 생성이전에 <header> 에서 script 를 loading 하므로, html 요소 중에 이용가능한 것은 HTML 의 title 밖에 없다.
        - 인자를 title 에 encoding 해서 보낸다.
 */

import * as vscode from 'vscode';
import { CausalLink } from './clientParser';
import { SystemGraphInfo } from './clientVisitor';

/**
 * Cytoscape 를 이용해서 webview contents 생성
 * @param connections 
 * @returns 
 */
export function getWebviewContentCytoscape(filePath:string, extensionUri: vscode.Uri,
    webview:vscode.Webview, systemInfos:SystemGraphInfo[], connections: CausalLink[])
{
    const systemNames:string[] = systemInfos.map(si => si.name);
    
    console.log('getWebviewContentCytoscape');
    /** Cytoscape 용 Elements 생성 : Node or Edge 정보 반환 */
    function *generateElements()
    {
        // system compound container box
        yield*
          systemNames
          .map(sn => {
            return {"data": { "id": sn, "label": sn, "background_color": "gray" }};
          });

        // call segment node 생성
        yield*
          systemInfos.flatMap(si =>
            si.calls.map(c => {
              const id = `${si.name}.${c.name}`;
              const label = `${c.name}\n${c.detail}`;
              return {"data": { id, label, "background_color": "blue", parent: si.name} };
            }));

        // listing node (unreferenced) 생성
        yield*
          systemInfos.flatMap(si =>
            si.segmentListings.map(l => {
              const id = `${si.name}.${l}`;
              const label = l;
              return {"data": { id, label, "background_color": "green", parent: si.name} };
            }));
        
        // connection (edge) 로부터 node 생성
        const nodes =
            connections
            .flatMap(c => [c.l, c.r])
            .filter((v, i, a) => a.indexOf(v) === i)  // filter unique : https://stackoverflow.com/questions/1960473/get-all-unique-values-in-a-javascript-array-remove-duplicates
            .map(n => {
              let bg = 'green';
              let parent = null;
              let style = null;   // style override
              const classes = [n.type];
              switch(n.type)
              {
                case 'func': bg = 'springgreen'; style = {"shape": "rectangle"}; break;
                case 'proc': bg = 'lightgreen'; break;
                case 'system': bg = 'grey'; break;
                case 'segment': parent = n.parentId; break;
                case 'conjunction': bg = 'beige'; break;
              }
              return { "data": {"id": n.id, "label": n.label, "background_color" : bg, parent }, style, classes };
            })
        ;

        yield* nodes;

        yield* connections.map(c => {
            const dashStyle = c.op.includes('|') ? 'dashed' : 'solid';
            return { "data": {
                "id": `${c.l.id}${c.op}${c.r.id}`,
                "source": c.l.id,
                "target": c.r.id,
                "line-style": dashStyle}};
        });
    }

    function toWebviewUri(fileName:string)
    {
      const path = vscode.Uri.joinPath(extensionUri, 'media', fileName);
      return webview.asWebviewUri(path);
    }

    /*
     * HTML document 에서 loading 할 local script files.  .../media/*.js 위치에 있음.
     */
		// Local path to main script run in the webview
		// And the uri we use to load this script in the webview
    const scriptCytoscapeMin      = toWebviewUri('cytoscape.min.js');
    const scriptCytoscapeUndoRedo = toWebviewUri('cytoscape-undo-redo.js');
    const scriptCytoscapeDS       = toWebviewUri('cytoscape.ds.js');
    const scriptCytoscapeLayoutCise = toWebviewUri('cytoscape-cise.js');
    const cssCytoscape            = toWebviewUri('cytoscape.css');
    const cssUI                   = toWebviewUri('ui.css');

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
    // console.log('TEXT=', els.map(e => JSON.stringify(e)).join('\n'));

    return `<!DOCTYPE html>
    <html lang="en">
    
    <head>
      <meta charset="UTF-8">
      <meta name="viewport" content="width=device-width">

      <link href="${cssUI}" rel="stylesheet" />
      <link href="${cssCytoscape}" rel="stylesheet" />
      
      <script nonce="${getNonce()}" src="${scriptCytoscapeMin}"></script>
      <script nonce="${getNonce()}" src="${scriptCytoscapeUndoRedo}"></script>

      <!--
      <script nonce="${getNonce()}" src="${scriptCytoscapeLayoutCise}"></script>
      -->

      <!-- Working!!
      <script nonce="${getNonce()}" src="https://unpkg.com/cytoscape/dist/cytoscape.min.js"></script>
      <script nonce="${getNonce()}" src="https://ivis-at-bilkent.github.io/cytoscape.js-undo-redo/cytoscape-undo-redo.js"></script>
      <script nonce="${getNonce()}" type="module">
        import ext as cise from 'cytoscape-cise';
        cytoscape.use( cise );
      </script>
      -->

      <script nonce="${getNonce()}" src="https://cdn.rawgit.com/cpettitt/dagre/v0.7.4/dist/dagre.min.js"></script>
      <script nonce="${getNonce()}" src="https://cdn.rawgit.com/cytoscape/cytoscape.js-dagre/1.5.0/cytoscape-dagre.js"></script>
      


      <title>${filePath}</title>

      <style>
        body {
          font-family: helvetica neue, helvetica, liberation sans, arial, sans-serif;
          /* font-size: 14px; */
        }    
        h1 {
          opacity: 0.5;
          font-size: 1em;
          font-weight: bold;
        }
      </style>

      <script nonce="${getNonce()}" src="${scriptCytoscapeDS}"></script>

      
    </head>
    
    <body>
      <h1>DS demo: ${filePath}</h1>
    
      <div id="myDiv" style="display:none">
        <input id="batchButton" type="button" value="Add 'Bob' and delete selected" />
        <b>DEL</b> to delete selected, <b>CTRL+Z</b> to undo, <b>CTRL+Y</b> to redo <br />
      </div>
    
      <div class="dropdown">
        <button onclick="layout('cose')" class="dropbtn">cose</button>
        <button onclick="layout('breadthfirst')" class="dropbtn">breadthfirst</button>
        <button onclick="layout('dagre')" class="dropbtn">dagre</button>
        <button onclick="layout('circle')" class="dropbtn">circle</button>
        <button onclick="layout('grid')" class="dropbtn">grid</button>
        <button onclick="layout('concentric')" class="dropbtn">concentric</button>
        <button onclick="layout('random')" class="dropbtn">random</button>
        


        <!--
        <button onclick="dropdown()" class="dropbtn">dropdown</button>

        <button onclick="layout('cise')" class="dropbtn">cise</button>    <<<<<<<<<< 그나마 괜찮은 듯..
        <button onclick="layout('klay')" class="dropbtn">klay</button>
        <button onclick="layout('elk')" class="dropbtn">elk</button>
        <button onclick="layout('cose-bilkent')" class="dropbtn">cose-bilkent</button>
        <button onclick="layout('dagre-bilkent')" class="dropbtn">dagre-bilkent</button>
        <button onclick="layout('cola')" class="dropbtn">cola</button>
        -->

      </div> 
      
      <div id="cy"></div>


      <div id="undoRedoList"  style="display:none">
        <span style="color: darkslateblue; font-weight: bold;">Log</span>
        <div id="undos" style="padding-bottom: 20px;"></div>
      </div>


      <script nonce="${getNonce()}">
        let cy = fillCytoscape(${elements});
      </script>



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

