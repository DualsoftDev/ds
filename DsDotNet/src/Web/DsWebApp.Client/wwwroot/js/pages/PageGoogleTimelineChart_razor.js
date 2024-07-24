window.initializeChart = (data) => {
    google.charts.load('current', { 'packages': ['timeline'] });
    google.charts.setOnLoadCallback(() => drawChart(data));
};


var options;
var viewWindowStart;
var viewWindowEnd;
var zoomTimeout;

function drawChart(data) {
    console.log('Drawing GOOGLE chart')
    var container = document.getElementById('timeline');
    window.chart = new google.visualization.Timeline(container);
    window.dataTable = new google.visualization.DataTable();
    dataTable.addColumn({ type: 'string', id: 'Room' });
    dataTable.addColumn({ type: 'string', id: 'Name' });
    dataTable.addColumn({ type: 'string', id: 'style', role: 'style' });
    dataTable.addColumn({ type: 'string', role: 'tooltip' });
    dataTable.addColumn({ type: 'date', id: 'Start' });
    dataTable.addColumn({ type: 'date', id: 'End' });

    window.colDateS = 4
    window.colDateE = 5
    /* https://developers.google.com/chart/interactive/docs/gallery/timeline
     * When there are four columns in a timeline dataTable,
     *  - the first is interpreted as the row label,
     *  - the second as the bar label,
     *  - and the third and fourth as start and end.
     * 
     * 5개 이상 column 이면 style column, tooltip column 등 이 추가된 것으로 해석
     *  - tooltip column 이 null 이면 default tooltip 으로 보여진다.
     */
    data.forEach(row => dataTable.addRow([row[0], row[1], row[2], row[3], new Date(row[colDateS]), new Date(row[colDateE])]));

    //dataTable.addRows([
    //    ['Magnolia Room',   'CSS Fundamentals',     'yellow',       null,    new Date(0, 0, 0, 12,  0, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
    //    ['Magnolia Room',   'Intro JavaScript',     '#cbb69d',      '2',     new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
    //    ['Magnolia Room',   'Advanced JavaScript',  '#cbb69d',      '3',     new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 19, 0, 0, 100)],
    //    ['Gladiolus Room',  'Intermediate Perl',    '#cbb69d',      '4',     new Date(0, 0, 0, 12, 30, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
    //    ['Gladiolus Room',  'Advanced Perl',        'green',        null,    new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
    //    ['Gladiolus Room',  'Applied Perl',         '#cbb69d',      '6',     new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 18, 0, 0, 100)],
    //    ['Petunia Room',    'Google Charts',        '#cbb69d',      '7',     new Date(0, 0, 0, 12, 30, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
    //    ['Petunia Room',    'Closure',              '#cbb69d',      '8',     new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
    //    ['Petunia Room',    'App Engine',           '#cbb69d',      '9',     new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 18, 30, 0, 100)]]);

    var minDate = dataTable.getColumnRange(colDateS).min;
    var maxDate = dataTable.getColumnRange(colDateE).max;
    var timeRange = maxDate - minDate;

    viewWindowStart = new Date(minDate.getTime() - timeRange * 0.1);
    viewWindowEnd = new Date(maxDate.getTime() + timeRange * 0.1);

    options = {
        timeline: { showRowLabels: true },
        hAxis: {
            minValue: viewWindowStart,
            maxValue: viewWindowEnd
        },
        tooltip: { trigger: 'focus' } // Enable tooltips initially
    };

    chart.draw(dataTable, options);

    // 이벤트 리스너 추가
    google.visualization.events.addListener(chart, 'select', () => {
        var selection = chart.getSelection();
        if (selection.length > 0) {
            var row = selection[0].row;
            var room = dataTable.getValue(row, 0);
            var name = dataTable.getValue(row, 1);
            var start = dataTable.getValue(row, colDateS);
            var end = dataTable.getValue(row, colDateE);

            console.log('Selected event:', room, name, start, end);
        }
    });

    // 일정 시간 후 데이터 수정 (예: 5초 후)
    setTimeout(modifyData, 5000);

}

function modifyData() {
    // 오늘 날짜 가져오기
    var today = new Date();
    var year = today.getFullYear();
    var month = today.getMonth();
    var date = today.getDate();

    // 예시: 첫 번째 행의 세 번째 열의 값을 수정 (새로운 시작 시간)
    dataTable.setValue(0, 4, new Date(year, month, date, 13, 0, 0, 1)); // 'CSS Fundamentals'의 시작 시간을 오후 1시로 변경
    dataTable.setValue(0, 5, new Date(year, month, date, 15, 0, 0, 100)); // 'CSS Fundamentals'의 종료 시간을 오후 3시로 변경

    // 차트 다시 그리기
    chart.draw(dataTable, options);
}


function hideAllTooltips() {
    var tooltips = document.querySelectorAll('.google-visualization-tooltip');
    tooltips.forEach(function (tooltip) {
        tooltip.style.display = 'none';
    });
}

window.addEventListener('wheel', function (event) {
    // Clear the previous timeout
    if (zoomTimeout) {
        clearTimeout(zoomTimeout);
    }

    // Hide all tooltips during zoom
    hideAllTooltips();

    var viewWindowRange = viewWindowEnd - viewWindowStart;
    var zoomLevel = viewWindowRange * 0.05;

    if (event.deltaY < 0) {
        // Zoom in
        viewWindowStart = new Date(viewWindowStart.getTime() + zoomLevel);
        viewWindowEnd = new Date(viewWindowEnd.getTime() - zoomLevel);
    } else {
        // Zoom out
        viewWindowStart = new Date(viewWindowStart.getTime() - zoomLevel);
        viewWindowEnd = new Date(viewWindowEnd.getTime() + zoomLevel);
    }

    options.hAxis.minValue = viewWindowStart;
    options.hAxis.maxValue = viewWindowEnd;
    chart.draw(dataTable, options);

    // Re-enable tooltips after zooming is complete
    zoomTimeout = setTimeout(function () {
        options.tooltip.trigger = 'focus';
        chart.draw(dataTable, options);
    }, 500); // Adjust the timeout as needed
});

