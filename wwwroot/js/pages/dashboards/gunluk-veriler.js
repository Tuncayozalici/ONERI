document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('gunluk-veriler-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent || '{}');
    const data = normalizePayloadKeys(payload.model || {});
    const personelSeriesLabel = typeof payload.personelSeriesLabel === 'string' && payload.personelSeriesLabel.trim()
        ? payload.personelSeriesLabel.trim()
        : 'Personel';
    const chartCanvasIds = [
        'genelOeeGaugeGrafigi',
        'makineOeeGrafigi',
        'genelBilesenGrafigi',
        'bolumOeeGrafigi',
        'bolumHataGrafigi',
        'personelBolumGrafigi',
        'hataNedenGenelGrafigi',
        'duraklamaTrendGrafigi',
        'genelHataTrend'
    ];
    const chartParents = new Map();
    const chartTemplates = new Map();
    const chartInstances = [];
    const horizontalPercentLabelPluginFactory = window.OneriChartHelpers?.createHorizontalPercentLabelPlugin;

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

    function getThemeName() {
        return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    function getThemePalette() {
        const isDarkTheme = getThemeName() === 'dark';
        return {
            textColor: isDarkTheme ? '#9aa8bd' : '#475569',
            strongTextColor: isDarkTheme ? '#f8fbff' : '#1f2937',
            gridColor: isDarkTheme ? 'rgba(148, 163, 184, 0.14)' : 'rgba(148, 163, 184, 0.28)',
            gaugePrimary: isDarkTheme ? '#34d399' : '#10b981',
            gaugeMuted: isDarkTheme ? 'rgba(71, 85, 105, 0.42)' : 'rgba(148, 163, 184, 0.38)',
            oeeBar: isDarkTheme ? 'rgba(59, 130, 246, 0.78)' : 'rgba(37, 99, 235, 0.7)',
            oeeBarBorder: isDarkTheme ? 'rgba(96, 165, 250, 1)' : 'rgba(29, 78, 216, 0.96)',
            componentBars: [
                isDarkTheme ? 'rgba(96, 165, 250, 0.82)' : 'rgba(37, 99, 235, 0.74)',
                isDarkTheme ? 'rgba(52, 211, 153, 0.82)' : 'rgba(16, 185, 129, 0.74)',
                isDarkTheme ? 'rgba(196, 181, 253, 0.82)' : 'rgba(124, 58, 237, 0.72)'
            ],
            componentBorders: [
                isDarkTheme ? 'rgba(147, 197, 253, 1)' : 'rgba(29, 78, 216, 1)',
                isDarkTheme ? 'rgba(110, 231, 183, 1)' : 'rgba(5, 150, 105, 1)',
                isDarkTheme ? 'rgba(221, 214, 254, 1)' : 'rgba(109, 40, 217, 1)'
            ],
            errorBar: isDarkTheme ? 'rgba(251, 113, 133, 0.82)' : 'rgba(225, 29, 72, 0.72)',
            errorBarBorder: isDarkTheme ? 'rgba(253, 164, 175, 1)' : 'rgba(190, 24, 93, 0.96)',
            personnelBar: isDarkTheme ? 'rgba(34, 211, 238, 0.82)' : 'rgba(8, 145, 178, 0.72)',
            personnelBarBorder: isDarkTheme ? 'rgba(103, 232, 249, 1)' : 'rgba(14, 116, 144, 0.96)',
            trendPurple: isDarkTheme ? '#8b5cf6' : '#7c3aed',
            trendPurpleFill: isDarkTheme ? 'rgba(139, 92, 246, 0.14)' : 'rgba(124, 58, 237, 0.12)',
            trendRose: isDarkTheme ? '#fb7185' : '#e11d48',
            trendRoseFill: isDarkTheme ? 'rgba(251, 113, 133, 0.14)' : 'rgba(225, 29, 72, 0.12)',
            donutColors: [
                '#fb7185', '#60a5fa', '#fbbf24', '#2dd4bf', '#a78bfa',
                '#fb923c', '#94a3b8', '#22c55e', '#f472b6', '#38bdf8'
            ]
        };
    }

    function configureChartDefaults(palette) {
        Chart.defaults.color = palette.textColor;
        Chart.defaults.font.family = 'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
        Chart.defaults.plugins.legend.labels.usePointStyle = true;
    }

    function clampPercent(value) {
        const number = Number(value) || 0;
        return Math.max(0, Math.min(100, number));
    }

    function hasAnyData(values) {
        return Array.isArray(values) && values.some(function (value) {
            return Number(value || 0) !== 0;
        });
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

        parent.innerHTML = '<div class="gunluk-empty-state">' + message + '</div>';
    }

    function createChart(canvasId, config, hasData, emptyMessage) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return null;
        }

        if (!hasData) {
            renderEmptyStateById(canvasId, emptyMessage);
            return null;
        }

        const instance = new Chart(canvas.getContext('2d'), config);
        chartInstances.push(instance);
        return instance;
    }

    function createHorizontalValueLabelPlugin(palette) {
        return {
            id: 'gunlukHorizontalValueLabels',
            afterDatasetsDraw(chart) {
                if (!chart || chart.options.indexAxis !== 'y') {
                    return;
                }

                const ctx = chart.ctx;
                const chartArea = chart.chartArea;
                if (!ctx || !chartArea) {
                    return;
                }

                ctx.save();
                ctx.font = '600 12px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
                ctx.textBaseline = 'middle';

                chart.data.datasets.forEach(function (dataset, datasetIndex) {
                    const meta = chart.getDatasetMeta(datasetIndex);
                    if (!meta || meta.hidden) {
                        return;
                    }

                    meta.data.forEach(function (bar, dataIndex) {
                        const value = Number(dataset.data[dataIndex]);
                        if (!Number.isFinite(value) || value <= 0) {
                            return;
                        }

                        const maximumFractionDigits = Math.abs(value - Math.round(value)) > 0.001 ? 1 : 0;
                        const label = value.toLocaleString('tr-TR', {
                            maximumFractionDigits: maximumFractionDigits
                        });
                        const barStartX = Math.min(bar.base, bar.x);
                        const barEndX = Math.max(bar.base, bar.x);
                        const barWidth = Math.max(0, barEndX - barStartX);
                        const labelWidth = ctx.measureText(label).width;
                        const fitsInside = barWidth >= (labelWidth + 14);

                        if (fitsInside) {
                            ctx.textAlign = 'right';
                            ctx.fillStyle = '#ffffff';
                            ctx.fillText(label, barEndX - 8, bar.y);
                            return;
                        }

                        const x = Math.min(barEndX + 8, chartArea.right - labelWidth - 2);
                        ctx.textAlign = 'left';
                        ctx.fillStyle = palette.textColor;
                        ctx.fillText(label, x, bar.y);
                    });
                });

                ctx.restore();
            }
        };
    }

    function createGaugeCenterPlugin(value, palette) {
        return {
            id: 'gunlukGaugeCenterText',
            afterDraw(chart) {
                const ctx = chart.ctx;
                const chartArea = chart.chartArea;
                if (!ctx || !chartArea) {
                    return;
                }

                const centerX = chartArea.left + ((chartArea.right - chartArea.left) / 2);
                const centerY = chartArea.top + ((chartArea.bottom - chartArea.top) * 0.82);
                ctx.save();
                ctx.textAlign = 'center';
                ctx.fillStyle = palette.gaugePrimary;
                ctx.font = '700 30px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
                ctx.fillText(value.toFixed(2) + ' %', centerX, centerY);
                ctx.fillStyle = palette.textColor;
                ctx.font = '500 13px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
                ctx.fillText('Genel OEE', centerX, centerY + 24);
                ctx.restore();
            }
        };
    }

    function createRankGradientColors(count) {
        if (count <= 0) {
            return { background: [], border: [] };
        }

        const background = [];
        const border = [];
        const maxIndex = Math.max(1, count - 1);

        for (let index = 0; index < count; index += 1) {
            const ratio = index / maxIndex;
            const hue = 165 - (120 * ratio);
            background.push('hsla(' + hue + ', 74%, 48%, 0.76)');
            border.push('hsla(' + hue + ', 78%, 42%, 1)');
        }

        return { background: background, border: border };
    }

    function setupUltraToggle() {
        const summaryCard = document.getElementById('summaryCard');
        const summaryToggle = document.getElementById('summaryUltraToggle');
        const sectionNavGrid = document.getElementById('sectionNavGrid');
        const sectionLinksSection = document.getElementById('sectionLinksSection');
        if (!summaryCard || !summaryToggle) {
            return;
        }

        const saved = localStorage.getItem('summary_ultra');
        const enabled = saved === '1';
        summaryCard.classList.toggle('summary-ultra', enabled);
        if (sectionLinksSection) {
            sectionLinksSection.classList.toggle('section-links-ultra', enabled);
        }
        if (sectionNavGrid) {
            sectionNavGrid.classList.toggle('section-nav-grid-ultra', enabled);
        }
        summaryToggle.checked = enabled;

        summaryToggle.addEventListener('change', function () {
            summaryCard.classList.toggle('summary-ultra', summaryToggle.checked);
            if (sectionLinksSection) {
                sectionLinksSection.classList.toggle('section-links-ultra', summaryToggle.checked);
            }
            if (sectionNavGrid) {
                sectionNavGrid.classList.toggle('section-nav-grid-ultra', summaryToggle.checked);
            }
            localStorage.setItem('summary_ultra', summaryToggle.checked ? '1' : '0');
        });
    }

    function renderCharts() {
        const palette = getThemePalette();
        const horizontalPercentLabelPlugin = horizontalPercentLabelPluginFactory
            ? horizontalPercentLabelPluginFactory({
                textColor: palette.textColor,
                insideTextColor: '#ffffff',
                font: '600 12px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
            })
            : null;
        const horizontalValueLabelPlugin = createHorizontalValueLabelPlugin(palette);

        configureChartDefaults(palette);
        restoreChartContainers();

        const trendLabels = Array.isArray(data.TrendLabels) ? data.TrendLabels : [];
        const trendHasData = trendLabels.length > 0;

        const oeeValue = clampPercent(data.OrtalamaOee);
        createChart('genelOeeGaugeGrafigi', {
            type: 'doughnut',
            data: {
                labels: ['OEE', 'Kalan'],
                datasets: [
                    {
                        data: [oeeValue, 100 - oeeValue],
                        backgroundColor: [palette.gaugePrimary, palette.gaugeMuted],
                        borderWidth: 0,
                        hoverOffset: 0
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                rotation: -90,
                circumference: 180,
                cutout: '74%',
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return context.label + ': ' + Number(context.parsed || 0).toFixed(2) + '%';
                            }
                        }
                    }
                }
            },
            plugins: [createGaugeCenterPlugin(oeeValue, palette)]
        }, true, '');

        const machineLabels = Array.isArray(data.MakineOeeLabels) ? data.MakineOeeLabels : [];
        const machineValues = (Array.isArray(data.MakineOeeData) ? data.MakineOeeData : []).map(clampPercent);
        const machinePalette = createRankGradientColors(machineValues.length);
        createChart('makineOeeGrafigi', {
            type: 'bar',
            data: {
                labels: machineLabels,
                datasets: [
                    {
                        label: 'Ortalama OEE (%)',
                        data: machineValues,
                        backgroundColor: machinePalette.background,
                        borderColor: machinePalette.border,
                        borderWidth: 1,
                        borderRadius: 12,
                        barThickness: 22
                    }
                ]
            },
            options: {
                indexAxis: 'y',
                maintainAspectRatio: false,
                scales: {
                    x: {
                        beginAtZero: true,
                        suggestedMax: 100,
                        ticks: {
                            callback: function (value) {
                                return value + '%';
                            }
                        },
                        grid: {
                            color: palette.gridColor
                        }
                    },
                    y: {
                        grid: {
                            display: false
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            },
            plugins: horizontalPercentLabelPlugin ? [horizontalPercentLabelPlugin] : []
        }, hasAnyData(machineValues), 'Makine bazli OEE verisi bulunamadi.');

        createChart('genelBilesenGrafigi', {
            type: 'bar',
            data: {
                labels: ['Performans', 'Kullanilabilirlik', 'Kalite'],
                datasets: [
                    {
                        label: 'Ortalama (%)',
                        data: [
                            clampPercent(data.OrtalamaPerformans),
                            clampPercent(data.OrtalamaKullanilabilirlik),
                            clampPercent(data.OrtalamaKalite)
                        ],
                        backgroundColor: palette.componentBars,
                        borderColor: palette.componentBorders,
                        borderWidth: 1,
                        borderRadius: 10,
                        barThickness: 28
                    }
                ]
            },
            options: {
                indexAxis: 'y',
                maintainAspectRatio: false,
                scales: {
                    x: {
                        beginAtZero: true,
                        suggestedMax: 100,
                        ticks: {
                            callback: function (value) {
                                return value + '%';
                            }
                        },
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
        }, true, '');

        const bolumOeeLabels = Array.isArray(data.BolumOeeLabels) ? data.BolumOeeLabels : [];
        const bolumOeeValues = (Array.isArray(data.BolumOeeData) ? data.BolumOeeData : []).map(clampPercent);
        createChart('bolumOeeGrafigi', {
            type: 'bar',
            data: {
                labels: bolumOeeLabels,
                datasets: [
                    {
                        label: 'Ortalama OEE (%)',
                        data: bolumOeeValues,
                        backgroundColor: palette.oeeBar,
                        borderColor: palette.oeeBarBorder,
                        borderWidth: 1,
                        borderRadius: 10
                    }
                ]
            },
            options: {
                indexAxis: 'y',
                maintainAspectRatio: false,
                scales: {
                    x: {
                        beginAtZero: true,
                        suggestedMax: 100,
                        ticks: {
                            callback: function (value) {
                                return value + '%';
                            }
                        },
                        grid: {
                            color: palette.gridColor
                        }
                    },
                    y: {
                        grid: {
                            display: false
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            },
            plugins: horizontalPercentLabelPlugin ? [horizontalPercentLabelPlugin] : []
        }, hasAnyData(bolumOeeValues), 'Bolum bazli OEE verisi bulunamadi.');

        const bolumHataLabels = Array.isArray(data.BolumHataLabels) ? data.BolumHataLabels : [];
        const bolumHataValues = Array.isArray(data.BolumHataData) ? data.BolumHataData : [];
        createChart('bolumHataGrafigi', {
            type: 'bar',
            data: {
                labels: bolumHataLabels,
                datasets: [
                    {
                        label: 'Hatali Adet',
                        data: bolumHataValues,
                        backgroundColor: palette.errorBar,
                        borderColor: palette.errorBarBorder,
                        borderWidth: 1,
                        borderRadius: 10
                    }
                ]
            },
            options: {
                indexAxis: 'y',
                maintainAspectRatio: false,
                scales: {
                    x: {
                        beginAtZero: true,
                        grid: {
                            color: palette.gridColor
                        }
                    },
                    y: {
                        grid: {
                            display: false
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            },
            plugins: [horizontalValueLabelPlugin]
        }, hasAnyData(bolumHataValues), 'Bolum bazli hata verisi bulunamadi.');

        const personelLabels = Array.isArray(data.PersonelBolumLabels) ? data.PersonelBolumLabels : [];
        const personelValues = Array.isArray(data.PersonelBolumData) ? data.PersonelBolumData : [];
        createChart('personelBolumGrafigi', {
            type: 'bar',
            data: {
                labels: personelLabels,
                datasets: [
                    {
                        label: personelSeriesLabel,
                        data: personelValues,
                        backgroundColor: palette.personnelBar,
                        borderColor: palette.personnelBarBorder,
                        borderWidth: 1,
                        borderRadius: 10
                    }
                ]
            },
            options: {
                indexAxis: 'y',
                maintainAspectRatio: false,
                scales: {
                    x: {
                        beginAtZero: true,
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
            plugins: [horizontalValueLabelPlugin]
        }, hasAnyData(personelValues), 'Bolum bazli personel verisi bulunamadi.');

        const hataNedenLabels = Array.isArray(data.HataNedenLabels) ? data.HataNedenLabels : [];
        const hataNedenValues = Array.isArray(data.HataNedenData) ? data.HataNedenData : [];
        createChart('hataNedenGenelGrafigi', {
            type: 'doughnut',
            data: {
                labels: hataNedenLabels,
                datasets: [
                    {
                        data: hataNedenValues,
                        backgroundColor: palette.donutColors,
                        borderWidth: 0
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                cutout: '58%'
            }
        }, hasAnyData(hataNedenValues), 'Hata nedeni dagilimi icin veri bulunamadi.');

        const duraklamaValues = Array.isArray(data.DuraklamaTrendData) ? data.DuraklamaTrendData : [];
        createChart('duraklamaTrendGrafigi', {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: 'Duraklama (dk)',
                        data: duraklamaValues,
                        borderColor: palette.trendPurple,
                        backgroundColor: palette.trendPurpleFill,
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
        }, trendHasData && hasAnyData(duraklamaValues), 'Duraklama trend verisi bulunamadi.');

        const hataTrendValues = Array.isArray(data.HataTrendData) ? data.HataTrendData : [];
        createChart('genelHataTrend', {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: 'Hatali Adet',
                        data: hataTrendValues,
                        borderColor: palette.trendRose,
                        backgroundColor: palette.trendRoseFill,
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
        }, trendHasData && hasAnyData(hataTrendValues), 'Hata trend verisi bulunamadi.');
    }

    setupUltraToggle();
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
