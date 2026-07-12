(function () {
    const dataElement = document.getElementById('vitals-chart-data');
    if (!dataElement || typeof Chart === 'undefined') {
        return;
    }

    const chartData = JSON.parse(dataElement.textContent);
    const labels = chartData.labels;

    const chartDefaults = {
        type: 'line',
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                x: {
                    ticks: { maxRotation: 45, minRotation: 0 }
                }
            }
        }
    };

    function createChart(canvasId, label, data, color) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const container = canvas.closest('.vitals-chart-container');
        if (!container) return;

        new Chart(canvas, {
            ...chartDefaults,
            data: {
                labels,
                datasets: [{
                    label,
                    data,
                    borderColor: color,
                    backgroundColor: color + '33',
                    fill: true,
                    tension: 0.3,
                    pointRadius: 4
                }]
            }
        });
    }

    createChart('heartRateChart', 'Heart Rate', chartData.heartRate, '#dc3545');
    createChart('breathingRateChart', 'Breathing Rate', chartData.respiratoryRate, '#0d6efd');
    createChart('systolicBloodPressureChart', 'Systolic Blood Pressure', chartData.systolicBloodPressure, '#6f42c1');
    createChart('diastolicBloodPressureChart', 'Diastolic Blood Pressure', chartData.diastolicBloodPressure, '#fd7e14');
})();
