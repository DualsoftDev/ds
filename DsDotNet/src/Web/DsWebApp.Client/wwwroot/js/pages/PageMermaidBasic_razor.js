window.initializeMermaid = () => {
    mermaid.initialize({ startOnLoad: true });
};
window.renderMermaid = () => {
    mermaid.contentLoaded();
    addInteractivity();
};

function addInteractivity() {
    // ensure the SVG is rendered before applying interactivity
    setTimeout(() => {
        var svg = d3.select('.mermaid svg');

        // Check if the svg element exists
        if (svg.empty()) {
            console.error('SVG element not found!');
            return;
        }

        //// Check the SVG structure for debugging purposes
        //console.log(`SVG Node: ${svg.node()}`);
        
        var zoom = d3.zoom()
            .scaleExtent([0.5, 5]) // 줌 인/아웃의 한계 설정
            .on('zoom', function (event) {
                //console.log('Zoom event', event); // 콘솔 로그 추가
                svg.attr('transform', event.transform);
            });

        svg.call(zoom);

        console.log('Zoom interactivity added');
    }, 1000); // SVG가 렌더링될 시간을 기다리기 위해 지연 시간 추가
}
