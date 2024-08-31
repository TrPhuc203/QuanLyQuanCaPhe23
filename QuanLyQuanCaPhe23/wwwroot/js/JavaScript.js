

function drawChartTheoSoLuong(ctx, labels, data, title) {
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: title,
                data: data,
                borderWidth: 1,
                backgroundColor: ['red', 'green', 'blue', 'gold', 'brown', 'orange']
            }]
        },
        options: {
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });
}
// Slide ads
// Slide ads


