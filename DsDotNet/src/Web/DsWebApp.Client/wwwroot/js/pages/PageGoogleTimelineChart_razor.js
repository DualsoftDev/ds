google.charts.load('current', { 'packages': ['timeline'] });
google.charts.setOnLoadCallback(drawChart);


var chart;
var dataTable;
var options;
var viewWindowStart;
var viewWindowEnd;
var zoomTimeout;

function drawChart() {
    console.log('Drawing GOOGLE chart')
    var container = document.getElementById('timeline');
    chart = new google.visualization.Timeline(container);
    dataTable = new google.visualization.DataTable();
    dataTable.addColumn({ type: 'string', id: 'Room' });
    dataTable.addColumn({ type: 'string', id: 'Name' });
    dataTable.addColumn({ type: 'string', id: 'style', role: 'style' });
    dataTable.addColumn({ type: 'string', role: 'tooltip' });
    dataTable.addColumn({ type: 'date', id: 'Start' });
    dataTable.addColumn({ type: 'date', id: 'End' });

    /* https://developers.google.com/chart/interactive/docs/gallery/timeline
     * When there are four columns in a timeline dataTable,
     *  - the first is interpreted as the row label,
     *  - the second as the bar label,
     *  - and the third and fourth as start and end.
     * 
     * 5개 이상 column 이면 style column, tooltip column 등 이 추가된 것으로 해석
     *  - tooltip column 이 null 이면 default tooltip 으로 보여진다.
     */
    dataTable.addRows([
        ['Magnolia Room',   'CSS Fundamentals',     'yellow',       null,    new Date(0, 0, 0, 12,  0, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
        ['Magnolia Room',   'Intro JavaScript',     '#cbb69d',      '2',     new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
        ['Magnolia Room',   'Advanced JavaScript',  '#cbb69d',      '3',     new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 19, 0, 0, 100)],
        ['Gladiolus Room',  'Intermediate Perl',    '#cbb69d',      '4',     new Date(0, 0, 0, 12, 30, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
        ['Gladiolus Room',  'Advanced Perl',        'green',        null,    new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
        ['Gladiolus Room',  'Applied Perl',         '#cbb69d',      '6',     new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 18, 0, 0, 100)],
        ['Petunia Room',    'Google Charts',        '#cbb69d',      '7',     new Date(0, 0, 0, 12, 30, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
        ['Petunia Room',    'Closure',              '#cbb69d',      '8',     new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
        ['Petunia Room',    'App Engine',           '#cbb69d',      '9',     new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 18, 30, 0, 100)]]);

    var originalStart = new Date(0, 0, 0, 12);
    var originalEnd = new Date(0, 0, 0, 18);
    var timeRange = originalEnd - originalStart;

    viewWindowStart = new Date(originalStart.getTime() - timeRange * 0.1);
    viewWindowEnd = new Date(originalEnd.getTime() + timeRange * 0.1);

    options = {
        timeline: { showRowLabels: true },
        hAxis: {
            minValue: viewWindowStart,
            maxValue: viewWindowEnd
        },
        tooltip: { trigger: 'focus' } // Enable tooltips initially
    };

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

