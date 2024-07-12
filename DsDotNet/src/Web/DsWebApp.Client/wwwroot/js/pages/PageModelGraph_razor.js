// click event: node 및 edge 둘다 적용
// node 만 적용하려면 cy.on('click', 'node', evt => ...  의 형태로 작성
cy.on('click', evt => {
    if (evt.target.id) {
        console.log(`clicked  cy.$('#${evt.target.id()}')`);
        console.log(evt.target.json());
        // console.log('clicked ' + JSON.stringify(evt.target.json()));
    }
})

// 'node' 클래스의 모든 노드에 mouseover 이벤트 리스너를 추가
cy.on('mouseover', 'node', function (event) {
    var node = event.target;
    // 여기서 노드에 마우스가 올려졌을 때의 작업을 수행합니다.
    console.log('Mouse over on node:', node.data('fqdn'));
    node.style({
        // 'background-color': 'CornflowerBlue',
        'border-color': 'navy',
        'border-width': '5px',
        'opacity': '1',
        'color': 'red',
        'text-outline-color': 'white',
        'text-outline-width': 2,
        'label': node.data('fqdn') // 마우스 오버 시 노드 ID (FQDN)를 라벨로 표시
    });
});

// 'node' 클래스의 모든 노드에 mouseout 이벤트 리스너를 추가
cy.on('mouseout', 'node', function (event) {
    var node = event.target;
    // 여기서 노드에서 마우스가 떠났을 때의 작업을 수행합니다.
    console.log('Mouse out from node:', node.data('fqdn'));
    node.style({
        'border-color': '',
        'border-width': '',
        'opacity': '',
        // 'background-color': '', // 원래 색으로 복원
        'color': '', // 원래 색으로 복원
        'text-outline-color': '',
        'text-outline-width': '',
        'label': '' // 라벨 제거
    });
});


 window.getEdgeLabel = function (edge) {
    return `${cy.$id(edge.data('source')).data('content')} => ${cy.$id(edge.data('target')).data('content')}`;
}
// 'edge' 클래스의 모든 엣지에 mouseover 이벤트 리스너를 추가
cy.on('mouseover', 'edge', function (event) {
    var edge = event.target;
    // 여기서 엣지에 마우스가 올려졌을 때의 작업을 수행합니다.
    console.log('Mouse over on edge:', edge.id());
    edge.style({
        'line-color': 'red',
        'color': 'red',
        'width': 4, // 엣지 두께 변경
        'label': getEdgeLabel(edge)  // 마우스 오버 시 엣지 ID를 라벨로 표시
    });
});

// 'edge' 클래스의 모든 엣지에 mouseout 이벤트 리스너를 추가
cy.on('mouseout', 'edge', function (event) {
    var edge = event.target;
    // 여기서 엣지에서 마우스가 떠났을 때의 작업을 수행합니다.
    console.log('Mouse out from edge:', edge.id());
    edge.style({
        'line-color': '', // 원래 색으로 복원
        'width': '', // 원래 두께로 복원
        'color': '', // 원래 색으로 복원
        'label': '' // 라벨 제거
    });
});

window.fit = function() {
    var visibleNodes = cy.$('node.Flow').filter(node => node.visible());

    // 열의 개수를 설정합니다.
    var num_columns = 3;

    // 함수 호출
    repositionNodes(visibleNodes, num_columns);
}

// button handlers
$('#center').click(() => {
    cy.center();
    //cy.fit(cy.nodes().filter(':visible'))
});


$('#hide').click(() => {
    cy.$(':selected.Flow').style('display', 'none');
    fit();
});

$('#dxButton').click(() => { Console.WriteLine('DxButton clicked.'); });

$('#show').click(() => {
    cy.$(':hidden.Flow').style('display', 'element')
    cy.fit(cy.nodes().filter(':visible'))
    fit()
});

$('#reset_zoom').click(() => {
    // multi layout : https://stackoverflow.com/questions/52200858/cytoscape-js-multiple-layouts-different-layout-within-compound-nodes

    // 최상위 레벨 노드를 찾기
    var topLevelNodes = cy.nodes().filter(function (node) {
        return node.data('parent') === undefined && !node.hasClass('DsSystem');;
    });

    const disconnectedNodes = cy.nodes().filter(node => node.degree(false) === 0);
    disconnectedNodes.layout({
        name: 'grid',
        rows: 3 // 모든 단일 노드를 한 줄로 배치
    }).run();


    console.log('Top level nodes:');
    topLevelNodes.forEach(function (node) {
        console.log(node.data('id'));
    });

    // 최상위 레벨 노드에 grid layout 적용
    topLevelNodes.layout({
        name: 'grid',
        columns: 3,
        fit: true,
        padding: 30,
    }).run();

    cy.$('node.Flow').layout({
        name: 'dagre',
        // columns: 3,
        // fit: true,
        padding: 30,
    }).run();
});