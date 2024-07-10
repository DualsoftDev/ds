//
// Graph algorithm 상에서 DAG 로 그리기 위해서 reset edge 는 일단 제외한 상태로 시작
//
var cyData = window.cyData
console.log(cyData)
var resetEdges = cyData.edges.filter(edge => edge.classes.includes('Reset'));
var nonResetEdges = cyData.edges.filter(edge => !edge.classes.includes('Reset'));

console.log(`Initial: resetEdges = ${resetEdges.length}, nonResetEdges = ${nonResetEdges.length}`)


// 동일한 source와 target을 가지는 엣지들을 병합합니다.
resetEdges.forEach(resetEdge => {
    let matchingEdge = nonResetEdges.find(edge => edge.data.source === resetEdge.data.target && edge.data.target === resetEdge.data.source);
    if (matchingEdge) {
        // matchingEdge의 클래스를 'Start, Reset' 으로 업데이트합니다.
        matchingEdge.classes = 'Start, ReverseReset';
    } else {
        // 만약 매칭되는 엣지가 없으면, resetEdge를 유지합니다.
        nonResetEdges.push(resetEdge);
    }
});
console.log(`Middle: resetEdges = ${resetEdges.length}, nonResetEdges = ${nonResetEdges.length}`)


cyData.edges = nonResetEdges

var cy = window.cy = cytoscape({
    container: document.getElementById('cy'),

    boxSelectionEnabled: false,

    style: [
        // https://stackoverflow.com/questions/45572034/how-to-select-nodes-by-class-in-cytoscape-js
        {
            selector: 'node',
            css: {
                // 'shape': 'data(shape)',
                'shape': 'pentagon',
                'content': 'data(content)',
                'border-width': 1,
                'border-color': 'navy',
                'text-valign': 'center',
                'text-halign': 'center',
                'background-opacity': 1,
            }
        },
        {
            selector: 'node.Flow',
            css: {
                'shape': 'rectangle',
                // 'text-outline-width': 1,
                // 'text-outline-color': 'white',
                'border-style': 'dashed',
                'font-size': '60px',
                'color': 'skyblue',
                'background-color': 'LightCyan',


                // 'text-background-color': 'blue',
                // 'text-background-padding': 100,
            }
        },
        {
            selector: 'node.Real',
            css: {
                'shape': 'rectangle',
                'background-color': 'DarkSalmon',
            }
        },
        {
            selector: 'node.Call',
            css: {
                'shape': 'ellipse',
                'background-color': 'DarkSeaGreen',
            }
        },
        {
            selector: 'node.Alias',
            css: {
                'shape': 'diamond',
            }
        },
        {
            selector: ':parent',
            css: {
                'text-valign': 'top',
                'text-halign': 'center',
                'shape': 'round-rectangle',
                'padding': 10
            }
        },
        {
            selector: ':selected',
            css: {
                'background-color': 'cyan',
            }
        },
        {
            selector: 'not(:selected)',
            css: {
                'background-color': '',
            }
        },
        {
            selector: 'node#e',     // node 에서 id 가 e 인 요소
            css: {
                'padding': 0
            }
        },
        {
            selector: 'edge',
            css: {
                'curve-style': 'bezier',
                'target-arrow-shape': 'triangle',
                'target-arrow-color': 'navy',
            }
        },
        {
            selector: 'edge.Reset',
            css: {
                'line-color': 'green',
                'curve-style': 'bezier',
                'target-arrow-shape': 'circle',
                'target-arrow-color': 'red',
                'line-style': 'dashed',     // 'solid', 'dotted',
                // 'line-dash-offset': 24,
                // 'line-dash-pattern': [6, 3],
            }
        },
        {
            selector: 'edge.ReverseReset',
            css: {
                'line-color': 'green',
                'curve-style': 'bezier',
                'source-arrow-shape': 'circle',
                'source-arrow-color': 'red',
                'line-style': 'dashed',     // 'solid', 'dotted',
            }
        },
    ],


    elements: cyData,
    wheelSensitivity: 0.2,

    layout: {
        // name: 'preset',
        // name: 'grid',
        // name: 'circle',
        name: 'dagre',              // https://jsfiddle.net/bababalcksheep/nyt8Lupv/
        animate: true,
        padding: 5
    },
});

console.log('---------------- cytoscape loaded ----------------')
if (window.cyReady) {
    console.log('---------------- cytoscape loaded ----------------')
    //cy.ready(window.cyReady);
    cy.on('load', window.cyReady);
}


/*
cy.ready() 나 cy.on('load') : 제대로 동작하지 않는 듯..
 */
repositionNodes = function (visibleNodes, columns) {
    console.log('repositionNodes: ');
    // 각 노드의 위치를 계산하여 재배치합니다.
    var spacingX = 0;
    var spacingY = 0;

    // 노드의 최대 너비와 높이를 구합니다.
    visibleNodes.forEach(node => {
        var bb = node.boundingBox();
        spacingX = Math.max(spacingX, bb.w);
        spacingY = Math.max(spacingY, bb.h);
    });

    // 여유 간격을 더해줍니다.
    spacingX += 20; // X축 여유 간격
    spacingY += 20; // Y축 여유 간격

    visibleNodes.forEach((node, index) => {
        var col = index % columns;
        var row = Math.floor(index / columns);
        var position = {
            x: col * spacingX,
            y: row * spacingY
        };
        console.log(`node: ${node.id()}, col: ${col}, row: ${row}, x: ${position.x}, y: ${position.y}`)
        node.position(position);
    });

    // Cytoscape.js에서 노드 위치 변경을 적용합니다.
    cy.batch(function () {
        visibleNodes.forEach(node => {
            node.position(node.position());
        });
    });
    cy.layout({ name: 'preset' }).run();
}



/*
// cy.ready() 또는 cy.on('load') 이벤트 핸들러: 실제 실행은 되지만, (blazor 환경 때문?? )최초 loading 시에 반영은 되지 않는다.
cy.ready(function () {
    // 특정 클래스("Flow")를 가진 보이는 노드들을 필터링합니다.
    var visibleNodes = cy.$('node.Flow').filter(node => node.visible());

    // 열의 개수를 설정합니다.
    var num_columns = 3;

    // 함수 호출
    repositionNodes(visibleNodes, num_columns);

});
*/


/*   
Shapes: 
   ellipse
   triangle
   round-triangle
   rectangle
   round-rectangle
   bottom-round-rectangle
   cut-rectangle
   barrel
   rhomboid
   right-rhomboid
   diamond
   round-diamond
   pentagon
   round-pentagon
   hexagon
   round-hexagon
   concave-hexagon
   heptagon
   round-heptagon
   octagon
   round-octagon
   star
   tag
   round-tag
   vee
   polygon (custom polygon specified via shape-polygon-points).
*/





