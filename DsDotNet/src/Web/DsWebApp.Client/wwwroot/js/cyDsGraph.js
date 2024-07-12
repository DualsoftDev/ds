//
// Graph algorithm 상에서 DAG 로 그리기 위해서 reset edge 는 일단 제외한 상태로 시작
//
var cyData = window.cyData
console.log(cyData)





// 엣지를 정규화된 방식으로 그룹화하는 함수.  source와 target을 알파벳 순서로 정렬한 후, 이를 키로 사용하여 반환.  source 와 target 순서 무시하는 방법
function normalizeEdge(edge) {
    return [edge.data.source, edge.data.target].sort().join('_');
}

// 엣지 그룹을 위한 객체
var edgeGroups = {};
// cyData.edges를 순회하면서 엣지를 그룹화
cyData.edges.forEach(edge => {
    let normalizedKey = normalizeEdge(edge);
    if (!edgeGroups[normalizedKey]) {
        edgeGroups[normalizedKey] = [];
    }
    edgeGroups[normalizedKey].push(edge);
});


var edges = [];

// 그룹화된 엣지를 처리하는 예제
Object.keys(edgeGroups).forEach(key => {
    var group = edgeGroups[key];
    if (group.length === 1) {
        edges.push(group[0]);    
    } else if (group.length > 1) {

        console.log('group before: ', JSON.stringify(group))
        // 'Start' 클래스를 가진 엣지를 맨 먼저 오게 정렬
        group.sort((a, b) => {
            if (a.classes.includes('Start') && !b.classes.includes('Start')) {
                return -1;
            } else if (!a.classes.includes('Start') && b.classes.includes('Start')) {
                return 1;
            } else {
                return 0;
            }
        });
        console.log('group after: ', JSON.stringify(group))

        var theEdge = group[0]
        console.log('The Edge: ', JSON.stringify(theEdge))
        //console.log(`The edge (${theEdge.classes}): ${theEdge.source.content} -> ${theEdge.target.content}`);
        edges.push(theEdge);


        // 그룹 내에 여러 엣지가 있는 경우 처리
        console.log(`Group ${key} has ${group.length} edges.`);
        group.slice(1).forEach(edge => {
            var c = theEdge.classes
            var isForward = edge.data.source === theEdge.data.source && edge.data.target === theEdge.data.target
            switch (edge.classes) {
                case 'Reset':
                    console.log(`updating ${theEdge.classes} += ${isForward ? ' Reset' : ' ReverseReset'}`)
                    theEdge.classes = theEdge.classes + (isForward ? ' Reset' : ' ReverseReset');
                    break;
                default:
                    console.error(`Unknown edge class: ${edge.classes}`);
                    break;
            }
            console.log(edge);
        });
    }
});

console.log('Edge groups:', edgeGroups);



//var resetEdges = cyData.edges.filter(edge => edge.classes.includes('Reset'));
//var startEdges = cyData.edges.filter(edge => edge.classes.includes('Start'));

//console.log(`Initial: resetEdges = ${resetEdges.length}, startEdges = ${startEdges.length}`)


//// 동일한 source와 target을 가지는 엣지들을 병합.  start edge 기준으로 방향이 반대인 reset edge 병합.
//resetEdges.forEach(resetEdge => {
//    let forwardStartReverseResetMatchingEdge = startEdges.find(edge => edge.data.source === resetEdge.data.target && edge.data.target === resetEdge.data.source);
//    let forwardStartForwardResetMatchingEdge = startEdges.find(edge => edge.data.source === resetEdge.data.source && edge.data.target === resetEdge.data.target);

//    if (forwardStartReverseResetMatchingEdge) {
//        // matchingEdge의 클래스를 'Start, Reset' 으로 업데이트합니다.
//        forwardStartReverseResetMatchingEdge.classes = 'Start, ReverseReset';
//    } else if (forwardStartForwardResetMatchingEdge) {
//        // matchingEdge의 클래스를 'Start, Reset' 으로 업데이트합니다.
//        console.error("--------------------------------------------------- FOUND")
//        forwardStartForwardResetMatchingEdge.classes = 'Start, Reset';
//    } else {
//        // 만약 매칭되는 엣지가 없으면, resetEdge를 유지합니다.
//        startEdges.push(resetEdge);
//    }
//});
//console.log(`Middle: resetEdges = ${resetEdges.length}, startEdges = ${startEdges.length}`)
//cyData.edges = startEdges

cyData.edges = edges
var cy = window.cy = cytoscape({
    container: document.getElementById('cy'),

    boxSelectionEnabled: false,

    /*
     * Node classes
        - category: DsSystem, Flow, Real, Call, Alias
        - Monitoring 여부
            - M: Monitoring mode
                - R,G,F,H
                - U: Unknown
            - NM: Non-Monitoring, NorMal mode
     */

    style: [
        // https://stackoverflow.com/questions/45572034/how-to-select-nodes-by-class-in-cytoscape-js
        {
            selector: 'node',
            css: {
                // 'shape': 'data(shape)',
                'shape': 'rectangle',
                'content': 'data(content)',
                'border-width': 1,
                'border-color': 'navy',
                'text-valign': 'center',
                'text-halign': 'center',
                'background-opacity': 1,
            }
        },

        { selector: 'node.Call', css: { 'shape': 'ellipse' } },
        { selector: 'node.Alias', css: { 'shape': 'diamond' } },

        { selector: 'node.Flow.NM',  css: {'background-color': 'LightCyan'} },
        { selector: 'node.Real.NM',  css: {'background-color': 'DarkSalmon'} },
        { selector: 'node.Call.NM',  css: {'background-color': 'DarkSeaGreen'} },
        { selector: 'node.Alias.NM', css: {'background-color': 'LightCyan' } },

        { selector: 'node.M.U', css: { 'background-color': 'white' } },
        { selector: 'node.M.R', css: { 'background-color': 'DarkGreen' } },
        { selector: 'node.M.G', css: { 'background-color': 'DarkGoldenrod' } },
        { selector: 'node.M.F', css: { 'background-color': 'RoyalBlue' } },
        { selector: 'node.M.H', css: { 'background-color': 'DimGray' } },


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
            css: { 'padding': 0 }
        },


        // "F:\Git\ds\DsDotNet\src\Doc\Edges.pptx" 파일 참고

        {
            selector: 'edge',
            css: {
                'line-style': 'solid',
                'curve-style': 'unbundled-bezier',
                'target-arrow-shape': 'triangle',
                'target-arrow-color': 'navy',
                'source-arrow-color': 'navy',
            }
        },
        {
            selector: 'edge.Start',
            css: {
                'target-arrow-color': 'navy',
            }
        },
        {
            selector: 'edge.Reset',
            css: {
                'line-style': 'dashed',     // 'solid', 'dotted',
            //    'line-color': 'green',
            //    'target-arrow-shape': 'circle',
            //    'target-arrow-color': 'red',
            //    'line-style': 'dashed',     // 'solid', 'dotted',
            }
        },
        {
            selector: 'edge.Start.ReverseReset',
            css: {
                'source-arrow-shape': 'circle',
                'target-arrow-shape': 'triangle',
            }
        },
        {
            selector: 'edge.Reset.ReverseReset',
            css: {
                'source-arrow-shape': 'triangle',
                'target-arrow-shape': 'triangle',
            }
        },
        {
            selector: 'edge.Start.Reset.ReverseReset',
            css: {
                'source-arrow-shape': 'circle',
                'target-arrow-shape': 'triangle',
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





