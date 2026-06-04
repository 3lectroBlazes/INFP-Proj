(function () {
    const dataElement = document.getElementById('admin-vitals-chart-data');
    if (!dataElement || typeof Chart === 'undefined') {
        return;
    }

    const chartData = JSON.parse(dataElement.textContent);
    const labels = chartData.labels || [];
    const series = chartData.series || [];

    const palette = [
        '#0b5f6b',
        '#dc3545',
        '#0d6efd',
        '#6f42c1',
        '#fd7e14',
        '#198754',
        '#d63384',
        '#20c997'
    ];

    const chartDefaults = {
        type: 'line',
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'bottom'
                }
            },
            scales: {
                x: {
                    ticks: { maxRotation: 45, minRotation: 0 }
                }
            }
        }
    };

    function buildDatasets(metricKey) {
        return series.map(function (patient, index) {
            const color = palette[index % palette.length];
            return {
                label: patient.patientName,
                data: patient[metricKey] || [],
                borderColor: color,
                backgroundColor: color + '22',
                fill: false,
                tension: 0.3,
                pointRadius: 3,
                spanGaps: true
            };
        });
    }

    function createChart(canvasId, metricKey) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const container = canvas.closest('.vitals-chart-container');
        if (!container) return;

        new Chart(canvas, {
            ...chartDefaults,
            data: {
                labels: labels,
                datasets: buildDatasets(metricKey)
            }
        });
    }

    createChart('heartRateChart', 'heartRate');
    createChart('breathingRateChart', 'respiratoryRate');
    createChart('bloodPressureChart', 'bloodPressure');
    createChart('temperatureChart', 'temperature');
})();
