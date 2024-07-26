window.initializeMermaid = () => {
    mermaid.initialize({ startOnLoad: true, securityLevel: 'loose' });
};
window.renderMermaid = (jsonOption) => {
    mermaid.contentLoaded();
    var option = JSON.parse(jsonOption)
    handleZoomAndDrag(option);
};


function getTaskInfo(event) {
    if (!event.defaultPrevented) {  // Zoom or drag prevented
        var target = d3.select(event.target);
        //console.log(target)
        return {
            taskId:  target.attr('id'),
            classes: target.node().classList
        }
    }

    return null;
}

// https://jsfiddle.net/zu_min_com/2agy5ehm/26/
// https://observablehq.com/@d3/drag-zoom
function handleZoomAndDrag({ zoomMin, zoomMax, enableConsoleLog }) {  // e.g: (zoomMin, zoomMax) = (0.5, 20)
    setTimeout(() => {
        if (enableConsoleLog)
            console.log(`zoomMin=${zoomMin}, zoomMax=${zoomMax}`)

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
                var ti = getTaskInfo(event)
                if (ti && enableConsoleLog)
                    console.log(`Clicked: ${ti.taskId}, classes=${ti.classes}`);
            })
            .on('mouseover', function (event) {
                var ti = getTaskInfo(event)
                if (ti && enableConsoleLog)
                    console.log(`Hovering over task: ${ti.taskId}`);
            })
            .on('mouseout', function (event) {
                var ti = getTaskInfo(event)
                if (ti && enableConsoleLog)
                    console.log(`Mouse out from task: ${ti.taskId}`);
            });
        });
    }, 1000); // SVG가 렌더링될 시간을 기다리기 위해 지연 시간 추가
}
