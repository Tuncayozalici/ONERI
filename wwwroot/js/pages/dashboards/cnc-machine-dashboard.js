document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('cnc-machine-dashboard-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent || '{}');
    const data = normalizePayloadKeys(payload.model || {});
    const pageType = String(payload.pageType || '').toLowerCase();
    const pageConfig = getPageConfig(pageType);
    if (!pageConfig) {
        return;
    }

    const chartCanvasIds = [
        'oeeTrendGrafigi',
        'uretimTrendGrafigi',
        'hataliTrendGrafigi',
        'oranTrendGrafigi',
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

    function getPageConfig(type) {
        const configs = {
            masterwood: {
                productionDatasets: [
                    {
                        label: 'Delik',
                        key: 'DelikTrendData',
                        lineColor: '#60a5fa',
                        fillColor: 'rgba(96, 165, 250, 0.14)'
                    },
                    {
                        label: 'Delik + Freeze',
                        key: 'DelikFreezeTrendData',
                        lineColor: '#fb7185',
                        fillColor: 'rgba(251, 113, 133, 0.12)'
                    }
                ]
            },
            skipper: {
                productionDatasets: [
                    {
                        label: 'Delik',
                        key: 'DelikTrendData',
                        lineColor: '#60a5fa',
                        fillColor: 'rgba(96, 165, 250, 0.14)'
                    }
                ]
            },
            roverb: {
                productionDatasets: [
                    {
                        label: 'Delik + Freeze',
                        key: 'DelikFreezeTrendData',
                        lineColor: '#60a5fa',
                        fillColor: 'rgba(96, 165, 250, 0.14)'
                    },
                    {
                        label: 'Delik + Freeze + PVC',
                        key: 'DelikFreezePvcTrendData',
                        lineColor: '#f472b6',
                        fillColor: 'rgba(244, 114, 182, 0.12)'
                    }
                ]
            }
        };

        return configs[type] || null;
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
            defectLine: isDarkTheme ? '#f87171' : '#ef4444',
            defectFill: isDarkTheme ? 'rgba(248, 113, 113, 0.14)' : 'rgba(239, 68, 68, 0.12)',
            ratioLine: isDarkTheme ? '#60a5fa' : '#2563eb',
            ratioFill: isDarkTheme ? 'rgba(96, 165, 250, 0.14)' : 'rgba(37, 99, 235, 0.12)',
            lossLine: isDarkTheme ? '#fb7185' : '#e11d48',
            lossFill: isDarkTheme ? 'rgba(251, 113, 133, 0.14)' : 'rgba(225, 29, 72, 0.12)',
            donutColors: [
                'rgba(244, 114, 182, 0.82)',
                'rgba(96, 165, 250, 0.82)',
                'rgba(251, 191, 36, 0.82)',
                'rgba(45, 212, 191, 0.82)',
                'rgba(167, 139, 250, 0.82)',
                'rgba(248, 113, 113, 0.82)'
            ]
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

        parent.innerHTML = '<div class="cnc-empty-state">' + message + '</div>';
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
        const trendLabels = Array.isArray(data.TrendLabels) ? data.TrendLabels : [];

        configureChartDefaults(palette);
        restoreChartContainers();

        const productionDatasets = pageConfig.productionDatasets.map(function (dataset) {
            return {
                label: dataset.label,
                data: Array.isArray(data[dataset.key]) ? data[dataset.key] : [],
                borderColor: dataset.lineColor,
                backgroundColor: dataset.fillColor,
                tension: 0.28,
                fill: true
            };
        });

        createChart('uretimTrendGrafigi', {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: productionDatasets
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
                        }
                    }
                }
            }
        }, productionDatasets.some(function (dataset) { return hasAnyData(dataset.data); }), 'Üretim trendi için veri bulunamadı.');

        createChart('oeeTrendGrafigi', {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: 'OEE (%)',
                        data: Array.isArray(data.OeeTrendData) ? data.OeeTrendData : [],
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
        }, hasAnyData(data.OeeTrendData), 'OEE trendi için veri bulunamadı.');

        createChart('hataliTrendGrafigi', {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: 'Hatalı Parça',
                        data: Array.isArray(data.HataliParcaTrendData) ? data.HataliParcaTrendData : [],
                        borderColor: palette.defectLine,
                        backgroundColor: palette.defectFill,
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
                        }
                    }
                }
            }
        }, hasAnyData(data.HataliParcaTrendData), 'Hatalı parça trendi için veri bulunamadı.');

        createChart('oranTrendGrafigi', {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: 'Performans',
                        data: Array.isArray(data.UretimOraniTrendData) ? data.UretimOraniTrendData : [],
                        borderColor: palette.ratioLine,
                        backgroundColor: palette.ratioFill,
                        tension: 0.28,
                        fill: true
                    },
                    {
                        label: 'Kayıp Süre',
                        data: Array.isArray(data.KayipSureTrendData) ? data.KayipSureTrendData : [],
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
        }, hasAnyData(data.UretimOraniTrendData) || hasAnyData(data.KayipSureTrendData), 'Performans ve kayıp süre trendi için veri bulunamadı.');

        createChart('duraklamaNedenGrafigi', {
            type: 'doughnut',
            data: {
                labels: Array.isArray(data.DuraklamaNedenLabels) ? data.DuraklamaNedenLabels : [],
                datasets: [
                    {
                        data: Array.isArray(data.DuraklamaNedenData) ? data.DuraklamaNedenData : [],
                        backgroundColor: palette.donutColors,
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
