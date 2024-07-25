window.initializeMermaid = () => {
    mermaid.initialize({ startOnLoad: true });
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
                });
            svg.call(zoom);
        });
    }, 1000); // SVG가 렌더링될 시간을 기다리기 위해 지연 시간 추가
}

