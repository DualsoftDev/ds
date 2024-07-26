window.initializeMermaid = () => {
    mermaid.initialize({ startOnLoad: true, securityLevel: 'loose' });
};
window.renderMermaid = (zoomMin, zoomMax) => {
    mermaid.contentLoaded();
    handleZoomAndDrag(zoomMin, zoomMax);
};



// https://jsfiddle.net/zu_min_com/2agy5ehm/26/
// https://observablehq.com/@d3/drag-zoom
function handleZoomAndDrag(zoomMin, zoomMax) {  // e.g: (zoomMin, zoomMax) = (0.5, 20)
    setTimeout(() => {
        var svgs = d3.selectAll(".mermaid svg");
        svgs.each(function () {
            var svg = d3.select(this);
            svg.html("<g>" + svg.html() + "</g>");
            var inner = svg.select("g");
            var zoom = d3.zoom()
                .scaleExtent([zoomMin, zoomMax]) // 줌 인/아웃의 한계 설정
                .on("zoom", function (event) {
                    inner.attr("transform", event.transform);
                })
                //.on("dblclick.zoom", null) // 더블 클릭 줌 비활성화
                ;
            //svg.call(zoom);
            // 더블 클릭 줌 비활성화
            svg.call(zoom).on("dblclick.zoom", null);

            // Mermaid 클릭 이벤트와 충돌을 피하기 위한 설정
            svg.on('click', function (event) {
                if (event.defaultPrevented) return; // Zoom or drag prevented
                var target = d3.select(event.target);
                if (target.classed('node') || target.classed('task')) {
                    // Task or node clicked
                    console.log('Task or node clicked', target.attr('id'));
                }
            });
        });
    }, 1000); // SVG가 렌더링될 시간을 기다리기 위해 지연 시간 추가
}

