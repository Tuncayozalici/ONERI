document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('boyahane-dashboard-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent || '{}');
    const data = normalizePayloadKeys(payload.model || {});
    const chartCanvasIds = [
        'boyaUretimHedefTrendGrafigi',
        'boyaOeeBilesenTrendGrafigi',
        'boyaMakineUretimGrafigi',
        'boyaMakineOeeGrafigi',
        'boyaDuraklamaGrafigi',
        'boyaParcaKarmaGrafigi'
    ];
    const chartParents = new Map();
    const chartTemplates = new Map();
    const chartInstances = [];

    chartCanvasIds.forEach(function (canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || !canvas.parentElement) {
            return;
        }

        chartParents.set(canvasId, canvas.parentElement);
        chartTemplates.set(canvasId, canvas.parentElement.innerHTML);
    });

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

    function hasAnyData(values) {
        return Array.isArray(values) && values.some(function (value) {
            return Number(value || 0) !== 0;
        });
    }

    function getThemeName() {
        return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    function getThemePalette() {
        const isDarkTheme = getThemeName() === 'dark';
        return {
            textColor: isDarkTheme ? '#9aa8bd' : '#475569',
            gridColor: isDarkTheme ? 'rgba(148, 163, 184, 0.14)' : 'rgba(148, 163, 184, 0.32)',
            productionLine: isDarkTheme ? '#67e8f9' : '#0891b2',
            productionFill: isDarkTheme ? 'rgba(103, 232, 249, 0.14)' : 'rgba(8, 145, 178, 0.12)',
            targetLine: isDarkTheme ? '#fbbf24' : '#d97706',
            targetFill: isDarkTheme ? 'rgba(251, 191, 36, 0.12)' : 'rgba(217, 119, 6, 0.12)',
            performanceLine: isDarkTheme ? '#818cf8' : '#4f46e5',
            performanceFill: isDarkTheme ? 'rgba(129, 140, 248, 0.12)' : 'rgba(79, 70, 229, 0.1)',
            qualityLine: isDarkTheme ? '#34d399' : '#10b981',
            qualityFill: isDarkTheme ? 'rgba(52, 211, 153, 0.12)' : 'rgba(16, 185, 129, 0.1)',
            availabilityLine: isDarkTheme ? '#fbbf24' : '#f59e0b',
            availabilityFill: isDarkTheme ? 'rgba(251, 191, 36, 0.12)' : 'rgba(245, 158, 11, 0.1)',
            oeeLine: isDarkTheme ? '#f87171' : '#dc2626',
            oeeFill: isDarkTheme ? 'rgba(248, 113, 113, 0.12)' : 'rgba(220, 38, 38, 0.1)',
            machineBar: isDarkTheme ? 'rgba(45, 212, 191, 0.72)' : 'rgba(13, 148, 136, 0.68)',
            machineOeeBar: isDarkTheme ? 'rgba(129, 140, 248, 0.78)' : 'rgba(79, 70, 229, 0.72)'
        };
    }

    function configureChartDefaults(palette) {
        Chart.defaults.color = palette.textColor;
        Chart.defaults.font.family = 'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
        Chart.defaults.plugins.legend.labels.usePointStyle = true;
    }

    function destroyCharts() {
        while (chartInstances.length > 0) {
            const instance = chartInstances.pop();
            instance.destroy();
        }
    }

    function restoreChartContainers() {
        destroyCharts();

        chartCanvasIds.forEach(function (canvasId) {
            const parent = chartParents.get(canvasId);
            const template = chartTemplates.get(canvasId);
            if (!parent || !template) {
                return;
            }

            parent.innerHTML = template;
        });
    }

    function renderEmptyStateById(canvasId, message) {
        const parent = chartParents.get(canvasId);
        if (!parent) {
            return;
        }

        parent.innerHTML = `<div class="boya-empty-state">${message}</div>`;
    }

    function createChart(canvasId, config, hasData, emptyMessage) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        if (!hasData) {
            renderEmptyStateById(canvasId, emptyMessage);
            return;
        }

        const instance = new Chart(canvas.getContext('2d'), config);
        chartInstances.push(instance);
    }

    function renderCharts() {
        const palette = getThemePalette();
        configureChartDefaults(palette);
        restoreChartContainers();

        const horizontalPercentLabelPlugin = window.OneriChartHelpers?.createHorizontalPercentLabelPlugin?.({
            textColor: palette.textColor,
            insideTextColor: '#ffffff',
            font: '600 12px system-ui, sans-serif'
        });

        createChart('boyaUretimHedefTrendGrafigi', {
            type: 'line',
            data: {
                labels: data.UretimTrendLabels || [],
                datasets: [
                    {
                        label: 'Toplam Boyanan',
                        data: data.UretimTrendData || [],
                        borderColor: palette.productionLine,
                        backgroundColor: palette.productionFill,
                        fill: true,
                        tension: 0.28
                    },
                    {
                        label: 'Performans İçin Parça',
                        data: data.HedefTrendData || [],
                        borderColor: palette.targetLine,
                        backgroundColor: palette.targetFill,
                        fill: true,
                        tension: 0.28
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: palette.gridColor
                        },
                        ticks: {
                            precision: 0
                        }
                    }
                }
            }
        }, hasAnyData(data.UretimTrendData) || hasAnyData(data.HedefTrendData), 'Seçilen dönem için üretim veya hedef verisi bulunamadı.');

        createChart('boyaOeeBilesenTrendGrafigi', {
            type: 'line',
            data: {
                labels: data.UretimTrendLabels || [],
                datasets: [
                    {
                        label: 'Performans (%)',
                        data: data.PerformansTrendData || [],
                        borderColor: palette.performanceLine,
                        backgroundColor: palette.performanceFill,
                        fill: false,
                        tension: 0.24
                    },
                    {
                        label: 'Kalite (%)',
                        data: data.KaliteTrendData || [],
                        borderColor: palette.qualityLine,
                        backgroundColor: palette.qualityFill,
                        fill: false,
                        tension: 0.24
                    },
                    {
                        label: 'Kullanılabilirlik (%)',
                        data: data.KullanilabilirlikTrendData || [],
                        borderColor: palette.availabilityLine,
                        backgroundColor: palette.availabilityFill,
                        fill: false,
                        tension: 0.24
                    },
                    {
                        label: 'OEE (%)',
                        data: data.OeeTrendData || [],
                        borderColor: palette.oeeLine,
                        backgroundColor: palette.oeeFill,
                        fill: false,
                        tension: 0.24
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        suggestedMax: 100,
                        grid: {
                            color: palette.gridColor
                        }
                    }
                }
            }
        }, hasAnyData(data.PerformansTrendData)
            || hasAnyData(data.KaliteTrendData)
            || hasAnyData(data.KullanilabilirlikTrendData)
            || hasAnyData(data.OeeTrendData), 'OEE bileşen trendi oluşturmak için yeterli veri bulunamadı.');

        createChart('boyaMakineUretimGrafigi', {
            type: 'bar',
            data: {
                labels: data.MakineLabels || [],
                datasets: [
                    {
                        label: 'Toplam Boyanan',
                        data: data.MakineUretimData || [],
                        backgroundColor: palette.machineBar,
                        borderRadius: 10
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                scales: {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: palette.gridColor
                        },
                        ticks: {
                            precision: 0
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            }
        }, hasAnyData(data.MakineUretimData), 'Makine üretim verisi bulunamadı.');

        createChart('boyaMakineOeeGrafigi', {
            type: 'bar',
            data: {
                labels: data.MakineLabels || [],
                datasets: [
                    {
                        label: 'OEE (%)',
                        data: data.MakineOeeData || [],
                        backgroundColor: palette.machineOeeBar,
                        borderRadius: 10
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                indexAxis: 'y',
                scales: {
                    x: {
                        beginAtZero: true,
                        suggestedMax: 100,
                        grid: {
                            color: palette.gridColor
                        }
                    },
                    y: {
                        grid: {
                            display: false
                        }
                    }
                }
            },
            plugins: horizontalPercentLabelPlugin ? [horizontalPercentLabelPlugin] : []
        }, hasAnyData(data.MakineOeeData), 'Makine OEE verisi bulunamadı.');

        createChart('boyaDuraklamaGrafigi', {
            type: 'doughnut',
            data: {
                labels: data.DuraklamaNedenLabels || [],
                datasets: [
                    {
                        label: 'Duraklama (dk)',
                        data: data.DuraklamaNedenData || [],
                        backgroundColor: [
                            'rgba(239, 68, 68, 0.82)',
                            'rgba(249, 115, 22, 0.82)',
                            'rgba(234, 179, 8, 0.82)',
                            'rgba(16, 185, 129, 0.82)',
                            'rgba(8, 145, 178, 0.82)',
                            'rgba(79, 70, 229, 0.82)',
                            'rgba(236, 72, 153, 0.82)',
                            'rgba(100, 116, 139, 0.82)'
                        ],
                        borderWidth: 0
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                cutout: '58%'
            }
        }, hasAnyData(data.DuraklamaNedenData), 'Duraklama verisi bulunamadı.');

        createChart('boyaParcaKarmaGrafigi', {
            type: 'doughnut',
            data: {
                labels: data.ParcaKarmaLabels || [],
                datasets: [
                    {
                        label: 'Parça',
                        data: data.ParcaKarmaData || [],
                        backgroundColor: [
                            'rgba(8, 145, 178, 0.82)',
                            'rgba(249, 115, 22, 0.82)',
                            'rgba(132, 204, 22, 0.82)',
                            'rgba(79, 70, 229, 0.82)',
                            'rgba(236, 72, 153, 0.82)'
                        ],
                        borderWidth: 0
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                cutout: '58%'
            }
        }, hasAnyData(data.ParcaKarmaData), 'Parça dağılım verisi bulunamadı.');
    }

    renderCharts();

    let activeTheme = getThemeName();
    const themeObserver = new MutationObserver(function () {
        const nextTheme = getThemeName();
        if (nextTheme === activeTheme) {
            return;
        }

        activeTheme = nextTheme;
        renderCharts();
    });

    themeObserver.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['data-theme']
    });
});
