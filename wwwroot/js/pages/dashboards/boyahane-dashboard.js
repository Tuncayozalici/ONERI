document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('boyahane-dashboard-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent || '{}');
    const data = normalizePayloadKeys(payload.model || {});
    const labels = data.UretimTrendLabels || [];

    const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
    const horizontalPercentLabelPlugin = window.OneriChartHelpers?.createHorizontalPercentLabelPlugin?.({
        textColor: isDarkTheme ? '#cbd5e1' : '#1f2937',
        insideTextColor: '#ffffff',
        font: '600 12px Poppins, sans-serif'
    });

    createUretimTrendChart(labels, data);
    createOeeTrendChart(labels, data);
    createMakineUretimChart(data);
    createMakineOeeChart(data, horizontalPercentLabelPlugin);
    createDuraklamaChart(data);
    createParcaKarmaChart(data);

    function normalizePayloadKeys(value) {
        if (Array.isArray(value)) {
            return value.map(normalizePayloadKeys);
        }

        if (value !== null && typeof value === 'object') {
            const normalized = {};
            for (const [key, nested] of Object.entries(value)) {
                const normalizedKey = key.length > 0 ? key[0].toUpperCase() + key.slice(1) : key;
                normalized[normalizedKey] = normalizePayloadKeys(nested);
            }
            return normalized;
        }

        return value;
    }

    function hasNonZeroValues(values) {
        return Array.isArray(values) && values.some(v => Number(v) > 0);
    }

    function showNoData(canvasId, message) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const parent = canvas.parentElement;
        if (!parent) {
            return;
        }

        parent.innerHTML = `<div class="boya-no-data">${message}</div>`;
    }

    function createUretimTrendChart(chartLabels, model) {
        const chartElement = document.getElementById('boyaUretimHedefTrendGrafigi');
        if (!chartElement) {
            return;
        }

        new Chart(chartElement.getContext('2d'), {
            type: 'line',
            data: {
                labels: chartLabels,
                datasets: [{
                    label: 'Toplam Boyanan',
                    data: model.UretimTrendData || [],
                    borderColor: 'rgba(14, 116, 144, 0.95)',
                    backgroundColor: 'rgba(14, 116, 144, 0.15)',
                    fill: true,
                    tension: 0.25
                }, {
                    label: 'Performans İçin Parça',
                    data: model.HedefTrendData || [],
                    borderColor: 'rgba(217, 119, 6, 0.95)',
                    backgroundColor: 'rgba(217, 119, 6, 0.12)',
                    fill: true,
                    tension: 0.25
                }]
            },
            options: {
                responsive: true,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    function createOeeTrendChart(chartLabels, model) {
        const chartElement = document.getElementById('boyaOeeBilesenTrendGrafigi');
        if (!chartElement) {
            return;
        }

        new Chart(chartElement.getContext('2d'), {
            type: 'line',
            data: {
                labels: chartLabels,
                datasets: [{
                    label: 'Performans (%)',
                    data: model.PerformansTrendData || [],
                    borderColor: 'rgba(79, 70, 229, 0.95)',
                    backgroundColor: 'rgba(79, 70, 229, 0.08)',
                    fill: false,
                    tension: 0.2
                }, {
                    label: 'Kalite (%)',
                    data: model.KaliteTrendData || [],
                    borderColor: 'rgba(16, 185, 129, 0.95)',
                    backgroundColor: 'rgba(16, 185, 129, 0.08)',
                    fill: false,
                    tension: 0.2
                }, {
                    label: 'Kullanılabilirlik (%)',
                    data: model.KullanilabilirlikTrendData || [],
                    borderColor: 'rgba(245, 158, 11, 0.95)',
                    backgroundColor: 'rgba(245, 158, 11, 0.08)',
                    fill: false,
                    tension: 0.2
                }, {
                    label: 'OEE (%)',
                    data: model.OeeTrendData || [],
                    borderColor: 'rgba(220, 38, 38, 0.95)',
                    backgroundColor: 'rgba(220, 38, 38, 0.08)',
                    fill: false,
                    tension: 0.2
                }]
            },
            options: {
                responsive: true,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        suggestedMax: 100
                    }
                }
            }
        });
    }

    function createMakineUretimChart(model) {
        const chartElement = document.getElementById('boyaMakineUretimGrafigi');
        if (!chartElement) {
            return;
        }

        if (!hasNonZeroValues(model.MakineUretimData)) {
            showNoData('boyaMakineUretimGrafigi', 'Makine üretim verisi bulunamadı.');
            return;
        }

        new Chart(chartElement.getContext('2d'), {
            type: 'bar',
            data: {
                labels: model.MakineLabels || [],
                datasets: [{
                    label: 'Toplam Boyanan',
                    data: model.MakineUretimData || [],
                    backgroundColor: 'rgba(14, 116, 144, 0.75)',
                    borderColor: 'rgba(14, 116, 144, 0.95)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    function createMakineOeeChart(model, labelPlugin) {
        const chartElement = document.getElementById('boyaMakineOeeGrafigi');
        if (!chartElement) {
            return;
        }

        if (!hasNonZeroValues(model.MakineOeeData)) {
            showNoData('boyaMakineOeeGrafigi', 'Makine OEE verisi bulunamadı.');
            return;
        }

        new Chart(chartElement.getContext('2d'), {
            type: 'bar',
            data: {
                labels: model.MakineLabels || [],
                datasets: [{
                    label: 'OEE (%)',
                    data: model.MakineOeeData || [],
                    backgroundColor: 'rgba(16, 185, 129, 0.7)',
                    borderColor: 'rgba(16, 185, 129, 0.95)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                indexAxis: 'y',
                scales: {
                    x: {
                        beginAtZero: true,
                        suggestedMax: 100
                    }
                }
            },
            plugins: labelPlugin ? [labelPlugin] : []
        });
    }

    function createDuraklamaChart(model) {
        const chartElement = document.getElementById('boyaDuraklamaGrafigi');
        if (!chartElement) {
            return;
        }

        if (!hasNonZeroValues(model.DuraklamaNedenData)) {
            showNoData('boyaDuraklamaGrafigi', 'Duraklama verisi bulunamadı.');
            return;
        }

        new Chart(chartElement.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: model.DuraklamaNedenLabels || [],
                datasets: [{
                    label: 'Duraklama (dk)',
                    data: model.DuraklamaNedenData || [],
                    backgroundColor: [
                        'rgba(239, 68, 68, 0.82)',
                        'rgba(59, 130, 246, 0.82)',
                        'rgba(245, 158, 11, 0.82)',
                        'rgba(16, 185, 129, 0.82)',
                        'rgba(14, 116, 144, 0.82)',
                        'rgba(99, 102, 241, 0.82)',
                        'rgba(236, 72, 153, 0.82)',
                        'rgba(120, 113, 108, 0.82)'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false
            }
        });
    }

    function createParcaKarmaChart(model) {
        const chartElement = document.getElementById('boyaParcaKarmaGrafigi');
        if (!chartElement) {
            return;
        }

        if (!hasNonZeroValues(model.ParcaKarmaData)) {
            showNoData('boyaParcaKarmaGrafigi', 'Parça dağılım verisi bulunamadı.');
            return;
        }

        new Chart(chartElement.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: model.ParcaKarmaLabels || [],
                datasets: [{
                    label: 'Parça',
                    data: model.ParcaKarmaData || [],
                    backgroundColor: [
                        'rgba(14, 116, 144, 0.82)',
                        'rgba(249, 115, 22, 0.82)',
                        'rgba(132, 204, 22, 0.82)',
                        'rgba(99, 102, 241, 0.82)',
                        'rgba(236, 72, 153, 0.82)'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false
            }
        });
    }

});
