namespace Dual.Web.Blazor.Client.Components.Chart;

public partial class CompChartJs
{
    public static object CreateStackedBarOptions(bool enableDataLabel, bool maintainAspectRatio = false)
    {
        return new
        {
            enableDataLabel = enableDataLabel,
            maintainAspectRatio = maintainAspectRatio,
            tooltips = new
            {
                displayColors = true,
                callbacks = new { mode = "x", },
            },
            scales = new
            {
                x = new { stacked = true, },
                y = new { stacked = true, },
            },
            responsive = true,
        };
    }

    public static string ClickHandlerSnippet = @"
        window.uninstallClickHandler = function (chartId) {
            const canvas = document.getElementById(chartId);
            if (!canvas) {
                console.error('Canvas not found:', chartId);
                return;
            }
            console.log(`---- uninstalling click handler for canvas: ${chartId}`);
            canvas.onclick = null;
        };

        window.installClickHandler = function (chartId, dotNetHelper, handlerName) {

            const canvas = document.getElementById(chartId);
            if (!canvas || !dotNetHelper) {
                console.error(`Canvas [${chartId}] not found or dotNetHelper object is null on installClickHandler`);
                return;
            }
            console.log(`canvas: ${canvas}`);
            canvas.onclick = (evt) => {
                var chart = window.chartJsObjects[chartId];
                const res = chart.getElementsAtEventForMode(
                    evt,
                    'nearest',
                    { intersect: true },
                    true
                );
                // If didn't click on a bar, `res` will be an empty array
                if (res.length === 0) {
                    return;
                }

                console.log('You clicked on ' + chart.data.labels[res[0].index]);
                dotNetHelper.invokeMethodAsync(handlerName, chart.data.labels[res[0].index])        // handlerName: e.g 'GoAssetDetailPage'
                    .catch(e => {
                        console.error('Error invoking .NET method:', e);
                        // 필요한 경우 여기에 오류 처리 로직 추가
                    });
            };


            function attemptFix() {
                // chartJsObjects가 존재하고, 해당 chartId에 대한 객체가 존재하는지 확인
                if (window.chartJsObjects && window.chartJsObjects[chartId]) {
                    var chart = window.chartJsObjects[chartId];
                    if (chart?.options?.plugins?.datalabels != null)
                    {
                        chart.options.plugins.datalabels.formatter = function (value, context) {
                            if (value)
                                return value.toFixed(1); // 소수점 두 자리로 포맷
                            return '';
                        };
                        chart.update();
                    }
                } else {
                    // chartJsObjects[chartId]가 아직 설정되지 않았다면, 500ms 후 다시 시도
                    setTimeout(attemptFix, 500);
                }
            }

            attemptFix();
	    };
    ";
}
