document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('pvc-dashboard-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent || '{}');
    const data = normalizePayloadKeys(payload.model || {});
    const chartCanvasIds = [
        'oeeTrendGrafigi',
        'makineOeeTrendGrafigi',
        'uretimTrendGrafigi',
        'makineUretimGrafigi',
        'makineParcaGrafigi',
        'fiiliKayipGrafigi',
        'duraklamaNedenGrafigi'
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
            oeeLine: isDarkTheme ? '#34d399' : '#10b981',
            oeeFill: isDarkTheme ? 'rgba(52, 211, 153, 0.14)' : 'rgba(16, 185, 129, 0.12)',
            productionLine: isDarkTheme ? '#60a5fa' : '#2563eb',
            productionFill: isDarkTheme ? 'rgba(96, 165, 250, 0.14)' : 'rgba(37, 99, 235, 0.12)',
            performanceLine: isDarkTheme ? '#a78bfa' : '#7c3aed',
            performanceFill: isDarkTheme ? 'rgba(167, 139, 250, 0.14)' : 'rgba(124, 58, 237, 0.12)',
            lossLine: isDarkTheme ? '#fbbf24' : '#f59e0b',
            lossFill: isDarkTheme ? 'rgba(251, 191, 36, 0.14)' : 'rgba(245, 158, 11, 0.12)',
            machineBar: isDarkTheme ? 'rgba(45, 212, 191, 0.72)' : 'rgba(13, 148, 136, 0.68)',
            machineParcaBar: isDarkTheme ? 'rgba(96, 165, 250, 0.78)' : 'rgba(37, 99, 235, 0.72)',
            machineOeeBar: isDarkTheme ? 'rgba(52, 211, 153, 0.78)' : 'rgba(16, 185, 129, 0.72)'
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

        parent.innerHTML = `<div class="pvc-empty-state">${message}</div>`;
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

    function getMakineParcaItems() {
        return (data.MakineLabels || [])
            .map(function (label, index) {
                return {
                    label: label,
                    value: (data.MakineParcaData || [])[index] ?? 0
                };
            })
            .filter(function (item) {
                return !String(item.label || '').toLocaleUpperCase('tr-TR').includes('TURAN');
            });
    }

    function getMakineOeeItems() {
        const labels = data.MakineOeeSerieLabels || [];
        const series = data.MakineOeeTrendSeries || [];

        return labels
            .map(function (label, index) {
                const values = Array.isArray(series[index]) ? series[index] : [];
                const validValues = values.filter(function (value) {
                    return typeof value === 'number' && value > 0;
                });
                const average = validValues.length > 0
                    ? validValues.reduce(function (sum, value) { return sum + value; }, 0) / validValues.length
                    : 0;

                return { label: label, value: average };
            })
            .sort(function (a, b) {
                return b.value - a.value;
            });
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
        const makineParcaItems = getMakineParcaItems();
        const makineOeeItems = getMakineOeeItems();

        createChart('oeeTrendGrafigi', {
            type: 'line',
            data: {
                labels: data.UretimTrendLabels || [],
                datasets: [
                    {
                        label: 'OEE (%)',
                        data: data.OeeTrendData || [],
                        borderColor: palette.oeeLine,
                        backgroundColor: palette.oeeFill,
                        tension: 0.28,
                        fill: true
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
        }, hasAnyData(data.OeeTrendData), 'Seçilen dönem için OEE trend verisi bulunamadı.');

        createChart('makineOeeTrendGrafigi', {
            type: 'bar',
            data: {
                labels: makineOeeItems.map(function (item) { return item.label; }),
                datasets: [
                    {
                        label: 'OEE (%)',
                        data: makineOeeItems.map(function (item) { return item.value; }),
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
        }, makineOeeItems.some(function (item) { return Number(item.value) > 0; }), 'Makine bazlı OEE karşılaştırması için yeterli veri bulunamadı.');

        createChart('uretimTrendGrafigi', {
            type: 'line',
            data: {
                labels: data.UretimTrendLabels || [],
                datasets: [
                    {
                        label: 'Metraj',
                        data: data.UretimTrendData || [],
                        borderColor: palette.productionLine,
                        backgroundColor: palette.productionFill,
                        tension: 0.28,
                        fill: true
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
        }, hasAnyData(data.UretimTrendData), 'Üretim trendi oluşturmak için yeterli metraj verisi bulunamadı.');

        createChart('makineUretimGrafigi', {
            type: 'bar',
            data: {
                labels: data.MakineLabels || [],
                datasets: [
                    {
                        label: 'Metraj',
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
        }, hasAnyData(data.MakineUretimData), 'Makine bazlı üretim verisi bulunamadı.');

        createChart('makineParcaGrafigi', {
            type: 'bar',
            data: {
                labels: makineParcaItems.map(function (item) { return item.label; }),
                datasets: [
                    {
                        label: 'Parça',
                        data: makineParcaItems.map(function (item) { return item.value; }),
                        backgroundColor: palette.machineParcaBar,
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
        }, makineParcaItems.some(function (item) { return Number(item.value) > 0; }), 'Makine bazlı parça dağılımı için veri bulunamadı.');

        createChart('fiiliKayipGrafigi', {
            type: 'line',
            data: {
                labels: data.UretimTrendLabels || [],
                datasets: [
                    {
                        label: 'Performans (%)',
                        data: data.UretimOraniTrendData || [],
                        borderColor: palette.performanceLine,
                        backgroundColor: palette.performanceFill,
                        tension: 0.28,
                        fill: true
                    },
                    {
                        label: 'Kayıp Süre (%)',
                        data: data.KayipSureData || [],
                        borderColor: palette.lossLine,
                        backgroundColor: palette.lossFill,
                        tension: 0.28,
                        fill: true
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
        }, hasAnyData(data.UretimOraniTrendData) || hasAnyData(data.KayipSureData), 'Performans veya kayıp süre trendi oluşturmak için yeterli veri bulunamadı.');

        createChart('duraklamaNedenGrafigi', {
            type: 'doughnut',
            data: {
                labels: data.DuraklamaNedenLabels || [],
                datasets: [
                    {
                        label: 'Duraklama (dk)',
                        data: data.DuraklamaNedenData || [],
                        backgroundColor: [
                            'rgba(244, 114, 182, 0.82)',
                            'rgba(96, 165, 250, 0.82)',
                            'rgba(251, 191, 36, 0.82)',
                            'rgba(45, 212, 191, 0.82)',
                            'rgba(129, 140, 248, 0.82)',
                            'rgba(248, 113, 113, 0.82)'
                        ],
                        borderWidth: 0
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                cutout: '58%'
            }
        }, hasAnyData(data.DuraklamaNedenData), 'Duraklama nedeni verisi bulunamadı.');
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
