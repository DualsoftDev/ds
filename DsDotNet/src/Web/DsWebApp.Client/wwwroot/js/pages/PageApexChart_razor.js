function formatDate(value) {
    const date = new Date(value);
    if (isNaN(date.getTime())) {
        console.log(`Invalid date value: ${value}`);
        return value; // Invalid date일 경우 원래 값을 반환
    }

    const options = {};
    if (document.getElementById('showDate').checked) {
        options.year = 'numeric';
        options.month = '2-digit';
        options.day = '2-digit';
    }
    if (document.getElementById('showHour').checked) {
        options.hour = '2-digit';
    }
    if (document.getElementById('showMinutes').checked) {
        options.minute = '2-digit';
    }
    if (document.getElementById('showSeconds').checked) {
        options.second = '2-digit';
    }
    if (document.getElementById('showMilliseconds').checked) {
        options.fractionalSecondDigits = 3;
    }

    // const options = {
    //   year: 'numeric',
    //   month: '2-digit',
    //   day: '2-digit',
    //   hour: '2-digit',
    //   minute: '2-digit',
    //   second: '2-digit',
    //   fractionalSecondDigits: 3,
    // };


    return date.toLocaleString('en-US', options);
}

function adjustDate(baseDate, seconds, milliseconds) {
    const date = new Date(baseDate);
    date.setSeconds(date.getSeconds() + seconds);
    date.setMilliseconds(date.getMilliseconds() + milliseconds);
    return date.getTime();
}

const baseDate = '2019-03-05T15:00:00';

const options = {
    series: [
        {
            name: 'Bob',
            data: [
                {
                    x: 'Design',
                    y: [
                        adjustDate(baseDate, 10, 111),
                        adjustDate(baseDate, 20, 222)
                    ]
                },
                {
                    x: 'Code',
                    y: [
                        adjustDate(baseDate, 30, 333),
                        adjustDate(baseDate, 40, 444)
                    ]
                },
                {
                    x: 'Code',
                    y: [
                        adjustDate(baseDate, 50, 555),
                        adjustDate(baseDate, 60, 666)
                    ]
                },
                {
                    x: 'Test',
                    y: [
                        adjustDate(baseDate, 70, 777),
                        adjustDate(baseDate, 80, 888)
                    ]
                },
                {
                    x: 'Test',
                    y: [
                        adjustDate(baseDate, 90, 999),
                        adjustDate(baseDate, 100, 111)
                    ]
                },
                {
                    x: 'Validation',
                    y: [
                        adjustDate(baseDate, 110, 222),
                        adjustDate(baseDate, 120, 333)
                    ]
                },
                {
                    x: 'Design',
                    y: [
                        adjustDate(baseDate, 330, 444),
                        adjustDate(baseDate, 540, 555)
                    ],
                }
            ]
        },
        {
            name: 'Joe',
            data: [
                {
                    x: 'Design',
                    y: [
                        adjustDate(baseDate, 150, 666),
                        adjustDate(baseDate, 160, 777)
                    ]
                },
                {
                    x: 'Test',
                    y: [
                        adjustDate(baseDate, 170, 888),
                        adjustDate(baseDate, 180, 999)
                    ],
                    goals: [
                        {
                            name: 'Break',
                            value: adjustDate(baseDate, 175, 500),
                            strokeColor: '#CD2F2A'
                        }
                    ]
                },
                {
                    x: 'Code',
                    y: [
                        adjustDate(baseDate, 190, 111),
                        adjustDate(baseDate, 200, 222)
                    ]
                },
                {
                    x: 'Deployment',
                    y: [
                        adjustDate(baseDate, 210, 333),
                        adjustDate(baseDate, 220, 444)
                    ]
                },
                {
                    x: 'Design',
                    y: [
                        adjustDate(baseDate, 230, 555),
                        adjustDate(baseDate, 240, 666)
                    ]
                }
            ]
        },
        {
            name: 'Dan',
            data: [
                {
                    x: 'Code',
                    y: [
                        adjustDate(baseDate, 250, 777),
                        adjustDate(baseDate, 260, 888)
                    ]
                },
                {
                    x: 'Validation',
                    y: [
                        adjustDate(baseDate, 270, 999),
                        adjustDate(baseDate, 280, 111)
                    ],
                    goals: [
                        {
                            name: 'Break',
                            value: adjustDate(baseDate, 275, 500),
                            strokeColor: '#CD2F2A'
                        }
                    ]
                }
            ]
        }
    ],
    chart: {
        height: 450,
        type: 'rangeBar',
        zoom: {
            type: 'x',
            enabled: true,
            autoScaleYaxis: true
        },
        events: {
            // https://stackoverflow.com/questions/62815241/apex-charts-limiting-zoom-out
            beforeZoom: (e, { xaxis }) => {
                const minDate = new Date(xaxis.min);
                const maxDate = new Date(xaxis.max);
                const zoomRange = maxDate - minDate;
                const minZoomRange = 300000; // 100초
                console.log(`----- Zoomed! New range: x=${xaxis.min} to ${xaxis.max}`);
                console.log(`----- Formatted range: ${formatDate(minDate)} to ${formatDate(maxDate)}`);
                console.log(`----- zoomRange range: ${zoomRange}, minZoomRange: ${minZoomRange}`);
                if (zoomRange < minZoomRange) {
                    console.log(`-----Narrow: ${new Date(xaxis.min)} ~ ${new Date(xaxis.min + minZoomRange)}`);
                    // return null;
                    return {
                        xaxis: {
                            min: xaxis.min,
                            max: xaxis.min + minZoomRange
                        }
                    };
                } else {
                    console.log(`-----Wide`);
                    return {
                        xaxis: {
                            min: xaxis.min,
                            max: xaxis.max
                        }
                    };
                }
            },
        }
    },
    plotOptions: {
        bar: {
            horizontal: true,
            barHeight: '80%'
        }
    },
    xaxis: {
        type: 'datetime',
        labels: {
            formatter: function (value) {
                return formatDate(value);
            }
        },
        datetimeUTC: true,
        datetimeFormatter: {
            year: 'yyyy',
            month: "MMM 'yy",
            day: 'dd MMM',
            hour: 'HH:mm',
            minute: 'HH:mm:ss',
            second: 'HH:mm:ss.fff'
        },
        //tickAmount: 8 // x축의 눈금 수를 제한하여 눈금을 조정
    },
    stroke: {
        width: 1
    },
    fill: {
        type: 'solid',
        opacity: 0.6
    },
    legend: {
        position: 'top',
        horizontalAlign: 'left'
    },
    tooltip: {
        enabled: true,
        x: {
            formatter: function (value) {
                return formatDate(value);
            }
        }
    }
};

var chart = new ApexCharts(document.querySelector("#chart"), options);
chart.render();

// Checkboxes change event listeners
const checkboxes = document.querySelectorAll('#controls input[type="checkbox"]');
checkboxes.forEach(checkbox => {
    checkbox.addEventListener('change', () => {
        chart.updateOptions({
            xaxis: {
                labels: {
                    formatter: function (value) { return formatDate(value); }
                }
            },
            tooltip: {
                x: {
                    formatter: function (value) { return formatDate(value); }
                }
            }
        });
    });
});