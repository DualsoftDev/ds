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

    /*
      <script src="https://ivis-at-bilkent.github.io/cytoscape.js-undo-redo/cytoscape-undo-redo.js"></script>
      <script src="cytoscape-undo-redo.js"></script>
    */

    const els = Array.from(generateElements()).join(",");
    const elements = `[ ${els} ]`;
    console.log('TEXT=', els);

    return `<!DOCTYPE html>
    <html lang="en">
    
    <head>
      <meta charset="UTF-8">
      <meta name="viewport" content="width=device-width, user-scalable=no, initial-scale=1, maximum-scale=1">
      <meta http-equiv="Content-Security-Policy" content="default-src self; img-src vscode-resource:; script-src * 'self' 'unsafe-inline'; style-src vscode-resource: 'self' 'unsafe-inline'; "/>
    
      <!-- 로컬 스크립트 로딩이 안됨 -->
      <script src="https://unpkg.com/cytoscape/dist/cytoscape.min.js"></script>
      <script src="https://ivis-at-bilkent.github.io/cytoscape.js-undo-redo/cytoscape-undo-redo.js"></script>
      <title>Document</title>
    
      <style>
        body {
          font-family: helvetica neue, helvetica, liberation sans, arial, sans-serif;
          font-size: 14px;
        }
        #cy {
          z-index: 999;
          width: 65%;
          height: 65%;
          position: absolute;
          float: left;
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
    
      <script>
        document.addEventListener('DOMContentLoaded', function () {
          let cy = cytoscape({
            container: document.getElementById('cy'),
            wheelSensitivity: 0.1,
            elements: ${elements},
            layout: {
              name: 'cose',   //circle, cose, grid
            },
            style: [
              {
                selector: 'node',
                style: {
                  'shape': 'round-rectangle',
                  // 'width': 'data(width)',
                  // 'height': 'data(height)',
                  'color': "white",        // text color
    
                  "border-width": 2,
                  "border-color": "white",
                  "border-style": "solid",   //"dotted",
    
    
                  // 'background-color': 'data(background_color)',
                  //'text-outline-color': 'data(background_color)',
    
                  // 'text-outline-color': 'orange'
                  'text-outline-width': 2,
                  'text-opacity': 0.5,
                  'label': 'data(label)',
                  //'font-size' : '25px',
    
                  // todo : 한번 color 정하면, selection color 변경할 수 있는 방법을 찾아야 함.
                  'background-color': 'green'
                }
              },
              {
                selector: 'edge',
                style: {
                  'curve-style': 'bezier',
                  'line-color': 'cyan',
                  'width': 3,
                  'target-arrow-shape': 'triangle',
                  // 'source-arrow-shape': 'circle',
                }
              },
              {
                selector: ':selected',
                style: {
                  "border-color": "red",
                  "border-width": 4,
                }
              }
    
            ]
          });
    
    
          var ur = cy.undoRedo({
            isDebug: true
          });
    
          cy.on("afterUndo", function (e, name) {
            document.getElementById("undos").innerHTML += "<span style='color: darkred; font-weight: bold'>Undo: </span> " + name + "</br>";
          });
    
          cy.on("afterRedo", function (e, name) {
            document.getElementById("undos").innerHTML += "<span style='color: darkblue; font-weight: bold'>Redo: </span>" + name + "</br>";
          });
    
          cy.on("afterDo", function (e, name) {
            document.getElementById("undos").innerHTML += "<span style='color: darkmagenta; font-weight: bold'>Do: </span>" + name + "</br>";
          });
    
          ur.do("add", {
            group: "nodes",
            data: { weight: 75, name: "New Node", id: "New Node", label:"New Node" },
            position: { x: 50, y: 50 },
            style: {
              "background-color": "darkred"
            }
          });
          document.addEventListener("keydown", function (e) {
            if (e.which === 46) {
              var selecteds = cy.$(":selected");
              if (selecteds.length > 0)
                ur.do("remove", selecteds);
            }
            else if (e.ctrlKey && e.target.nodeName === 'BODY')
              if (e.which === 90)
                ur.undo();
              else if (e.which === 89)
                ur.redo();
    
          });
    
          document.getElementById('batchButton').addEventListener("click", function (e) {
            actions = [];
            actions.push({
              name: "add",
              param: {
                group: "nodes",
                data: { weight: 75, name: "Bob", id: "Bob", label:"Bob" },
                position: { x: 50, y: 50 },
                style: { "background-color": "darkgreen" }
              }
            });
            actions.push({
              name: "remove",
              param: cy.$(":selected")
            });
            ur.do("batch", actions);
          });
    
          cy.edges()
            .filter((e, i) => e.isEdge() && e.data('line-style') == 'dashed')
            .style('line-style', 'dashed')
            //.update()
            ;
        });
      </script>
    </head>
    
    <body>
      <h1>cytoscape.js drawing/undo-redo demo</h1>
    
      <div id="myDiv">
        <input id="batchButton" type="button" value="Add 'Bob' and delete selected" />
        <b>DEL</b> to delete selected, <b>CTRL+Z</b> to undo, <b>CTRL+Y</b> to redo <br />
      </div>
      <!-- <p>
        <b>DEL</b> to delete selected, <b>CTRL+Z</b> to undo, <b>CTRL+Y</b> to redo <br />
        Test batch of actions: <input id="batchButton" type="button" value="Add 'Bob' and delete selected" />
      </p> -->
    
      <div id="cy"></div>
      <div id="undoRedoList">
        <span style="color: darkslateblue; font-weight: bold;">Log</span>
        <div id="undos" style="padding-bottom: 20px;"></div>
      </div>
    
    </body>
    
    </html>`;
}