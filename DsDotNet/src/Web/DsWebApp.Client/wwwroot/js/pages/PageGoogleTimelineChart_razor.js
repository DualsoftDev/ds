google.charts.load('current', { 'packages': ['timeline'] });
google.charts.setOnLoadCallback(drawChart);


var chart;
var data;
var options;
var viewWindowStart;
var viewWindowEnd;
var zoomTimeout;

function drawChart() {
    console.log('Drawing GOOGLE chart')
    var container = document.getElementById('timeline');
    chart = new google.visualization.Timeline(container);
    data = new google.visualization.DataTable();
    data.addColumn({ type: 'string', id: 'Room' });
    data.addColumn({ type: 'string', id: 'Name' });
    data.addColumn({ type: 'date', id: 'Start' });
    data.addColumn({ type: 'date', id: 'End' });
    data.addRows([
        ['Magnolia Room', 'CSS Fundamentals', new Date(0, 0, 0, 12, 0, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
        ['Magnolia Room', 'Intro JavaScript', new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
        ['Magnolia Room', 'Advanced JavaScript', new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 19, 0, 0, 100)],
        ['Gladiolus Room', 'Intermediate Perl', new Date(0, 0, 0, 12, 30, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
        ['Gladiolus Room', 'Advanced Perl', new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
        ['Gladiolus Room', 'Applied Perl', new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 18, 0, 0, 100)],
        ['Petunia Room', 'Google Charts', new Date(0, 0, 0, 12, 30, 0, 1), new Date(0, 0, 0, 14, 0, 0, 100)],
        ['Petunia Room', 'Closure', new Date(0, 0, 0, 14, 30, 0, 1), new Date(0, 0, 0, 16, 0, 0, 100)],
        ['Petunia Room', 'App Engine', new Date(0, 0, 0, 16, 30, 0, 1), new Date(0, 0, 0, 18, 30, 0, 100)]]);

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

    chart.draw(data, options);
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
    chart.draw(data, options);

    // Re-enable tooltips after zooming is complete
    zoomTimeout = setTimeout(function () {
        options.tooltip.trigger = 'focus';
        chart.draw(data, options);
    }, 500); // Adjust the timeout as needed
});

