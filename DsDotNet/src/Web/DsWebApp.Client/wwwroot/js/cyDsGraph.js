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
                'border-width': 4,
                'border-color': 'navy',
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













/*
      
cy.nodes()
cy.edges()
cy.$(':selected')
cy.$(':visible')
cy.$(':hidden')
cy.$(':parent')
cy.$('node.Flow')
cy.$(':selected').position()
cy.$(':selected').position({x:2000, y:10})
cy.$('#SIDE.S200_CARTYPE_MOVE.S204_END__SIDE.S200_CARTYPE_MOVE.S205_RBT1')
cy.$id('SIDE.S200_CARTYPE_MOVE.S204_END__SIDE.S200_CARTYPE_MOVE.S205_RBT1') // cy.$('#some-id') 와 동일
cy.$id('SIDE.S200_CARTYPE_MOVE.S204_END__SIDE.S200_CARTYPE_MOVE.S205_RBT1').data()
cy.$id('SIDE.S200_CARTYPE_MOVE.S204_END__SIDE.S200_CARTYPE_MOVE.S205_RBT1').classes()
cy.edges().filter(e => e.classes().includes("Reset")).length
cy.edges().filter(e => e.classes().includes("Reset"))[0].data()
cy.edges().filter(e => e.classes().includes("Reset"))[0].classes()
cy.edges().filter(e => e.classes().includes("Reset")).json()

cy.edges().filter(e => e.data().source === 'SIDE.MES.S201_RBT1' ).json()
cy.nodes().map(n => n.data().id)

cy.nodes().filter(n => n.data().id === "SIDE").json()
cy.$id('SIDE').json()
cy.$('#SIDE').json()




     // elements: {
     //   nodes: [
     //     { data: { id: 'a', parent: 'b' }, position: { x: 215, y: 85 } },
     //     { data: { id: 'b' } },
     //     { data: { id: 'c', parent: 'b' }, position: { x: 300, y: 85 } },
     //     { data: { id: 'd' }, position: { x: 215, y: 175 } },
     //     { data: { id: 'e' } },
     //     { data: { id: 'f', parent: 'e' }, position: { x: 300, y: 175 } }
     //   ],
     //   edges: [
     //     { data: { id: 'ad', source: 'a', target: 'd' } },
     //     { data: { id: 'eb', source: 'e', target: 'b' } }
   
     //   ]
     // },
   
   
   // elements: {
   //     'nodes': [
   //         { data: { id: 'a', parent: 'p' } },
   //         { data: { id: 'b', parent: 'p' } },
   //         { data: { id: 'c', parent: 'p' } },
   //         { data: { id: 'd', parent: 'p' } },
   //         { data: { id: 'e', parent: 'p' } },
   //         { data: { id: 'f', parent: 'p' } },
   //         { data: { id: 'g', parent: 'p' } },
   //         { data: { id: 'h', parent: 'p' } },
   //         { data: { id: 'p' } }
   //     ],
   //     'edges': [
   //         { data: { id: 'e1', source: 'a', target: 'b' } },
   //         { data: { id: 'e2', source: 'b', target: 'c' } },
   //         { data: { id: 'e3', source: 'b', target: 'd' } },
   //         { data: { id: 'e4', source: 'e', target: 'f' } },
   //         { data: { id: 'e5', source: 'f', target: 'g' } },
   //         { data: { id: 'e6', source: 'f', target: 'h' } }
   //     ]
   // },
   
   
   // elements: {
   //     nodes: [
   //         { data: { id: 'a', parent: 'p' }, position: { x: 215, y: 85 } },
   //         { data: { id: 'b', parent: 'p' }, position: { x: 300, y: 85 } },
   //         { data: { id: 'p' } }
   //     ],
   //     edges: [
   //         { data: { id: 'e1', source: 'a', target: 'b' } }
   //     ]
   // },
   
   
   
   // // https://stackoverflow.com/questions/27280708/how-do-i-make-classes-work-in-cytoscape-js
   // var cy = cytoscape({
   //     container: document.getElementById('cy'),
   //     style: [
   //         {
   //             selector: 'node',
   //             style: {
   //                 'label': 'data(id)'
   //             }
   //         },

   //         {
   //             selector: '.ClassName1',
   //             style: {
   //                 'width': 8,
   //                 'height': 8,
   //                 'label': ''
   //             }
   //         }
   //     ],
   //     elements: {
   //         nodes: [
   //               { data: { id: 'explore'}, classes: 'ClassName1'},
   //               { data: { id: 'discover' } }
   //         ],
   //         edges: [
   //               { data: { source: 'explore', target: 'discover' } }
   //         ]
   //    },
   // });
   
   
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





