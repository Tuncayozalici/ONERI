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
        'planUyumBolumGrafigi',
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
            valueBadgeTextColor: isDarkTheme ? '#f8fbff' : '#1f2937',
            valueBadgeBackground: isDarkTheme ? 'rgba(15, 23, 42, 0.92)' : 'rgba(255, 255, 255, 0.92)',
            valueBadgeBorder: isDarkTheme ? 'rgba(148, 163, 184, 0.28)' : 'rgba(148, 163, 184, 0.28)',
            gridColor: isDarkTheme ? 'rgba(148, 163, 184, 0.14)' : 'rgba(148, 163, 184, 0.28)',
            gaugePrimary: isDarkTheme ? '#34d399' : '#10b981',
            gaugePrimaryStrong: isDarkTheme ? '#22c55e' : '#059669',
            gaugeMuted: isDarkTheme ? 'rgba(71, 85, 105, 0.42)' : 'rgba(148, 163, 184, 0.38)',
            gaugeTarget: isDarkTheme ? '#fbbf24' : '#d97706',
            gaugeTargetZone: isDarkTheme ? 'rgba(248, 113, 113, 0.42)' : 'rgba(239, 68, 68, 0.28)',
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
            occupancySeries: [
                isDarkTheme ? '#60a5fa' : '#3b82f6',
                isDarkTheme ? '#34d399' : '#10b981',
                isDarkTheme ? '#fbbf24' : '#f59e0b',
                isDarkTheme ? '#f472b6' : '#ec4899',
                isDarkTheme ? '#a78bfa' : '#8b5cf6',
                isDarkTheme ? '#22d3ee' : '#06b6d4',
                isDarkTheme ? '#fb7185' : '#f43f5e'
            ],
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

    function getPercentAxisMax(values) {
        const numericValues = Array.isArray(values)
            ? values.map(clampPercent).filter(function (value) { return value > 0; })
            : [];

        if (numericValues.length === 0) {
            return 100;
        }

        const maxValue = Math.max.apply(null, numericValues);
        return Math.min(100, Math.max(10, Math.ceil(maxValue / 10) * 10));
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

    function createHorizontalValueLabelPlugin(palette, formatter) {
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
                        const label = typeof formatter === 'function'
                            ? formatter(value)
                            : value.toLocaleString('tr-TR', {
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

    function formatPercentValue(value) {
        return value.toLocaleString('tr-TR', {
            minimumFractionDigits: Math.abs(value - Math.round(value)) > 0.001 ? 1 : 0,
            maximumFractionDigits: 1
        }) + '%';
    }

    function buildSortedPairs(labels, values, direction, limit) {
        const pairs = (Array.isArray(labels) ? labels : []).map(function (label, index) {
            return {
                label: label,
                value: Number(Array.isArray(values) ? values[index] : 0) || 0
            };
        }).filter(function (item) {
            return item.label && item.value > 0;
        });

        pairs.sort(function (first, second) {
            return direction === 'asc'
                ? first.value - second.value
                : second.value - first.value;
        });

        const visiblePairs = Number.isFinite(limit) && limit > 0
            ? pairs.slice(0, limit)
            : pairs;

        return {
            labels: visiblePairs.map(function (item) { return item.label; }),
            values: visiblePairs.map(function (item) { return item.value; })
        };
    }

    function createVerticalValueLabelPlugin(palette, formatter) {
        return {
            id: 'gunlukVerticalValueLabels',
            afterDatasetsDraw(chart) {
                if (!chart || chart.options.indexAxis === 'y') {
                    return;
                }

                const ctx = chart.ctx;
                const chartArea = chart.chartArea;
                if (!ctx || !chartArea) {
                    return;
                }

                ctx.save();
                ctx.font = '700 12px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';

                function drawRoundedRect(x, y, width, height, radius) {
                    const safeRadius = Math.min(radius, width / 2, height / 2);
                    ctx.beginPath();
                    ctx.moveTo(x + safeRadius, y);
                    ctx.lineTo(x + width - safeRadius, y);
                    ctx.quadraticCurveTo(x + width, y, x + width, y + safeRadius);
                    ctx.lineTo(x + width, y + height - safeRadius);
                    ctx.quadraticCurveTo(x + width, y + height, x + width - safeRadius, y + height);
                    ctx.lineTo(x + safeRadius, y + height);
                    ctx.quadraticCurveTo(x, y + height, x, y + height - safeRadius);
                    ctx.lineTo(x, y + safeRadius);
                    ctx.quadraticCurveTo(x, y, x + safeRadius, y);
                    ctx.closePath();
                }

                chart.data.datasets.forEach(function (dataset, datasetIndex) {
                    const meta = chart.getDatasetMeta(datasetIndex);
                    if (!meta || meta.hidden || dataset.type === 'line') {
                        return;
                    }

                    meta.data.forEach(function (bar, dataIndex) {
                        const value = Number(dataset.data[dataIndex]);
                        if (!Number.isFinite(value) || value <= 0) {
                            return;
                        }

                        const label = typeof formatter === 'function'
                            ? formatter(value)
                            : value.toLocaleString('tr-TR', { maximumFractionDigits: 0 });
                        const labelWidth = ctx.measureText(label).width;
                        const badgeWidth = labelWidth + 14;
                        const badgeHeight = 22;
                        const badgeX = Math.max(
                            chartArea.left,
                            Math.min(chartArea.right - badgeWidth, bar.x - (badgeWidth / 2))
                        );
                        const badgeY = Math.max(chartArea.top + 4, bar.y - badgeHeight - 8);

                        ctx.fillStyle = palette.valueBadgeBackground;
                        drawRoundedRect(badgeX, badgeY, badgeWidth, badgeHeight, 10);
                        ctx.fill();
                        ctx.strokeStyle = palette.valueBadgeBorder;
                        ctx.lineWidth = 1;
                        ctx.stroke();

                        ctx.fillStyle = palette.valueBadgeTextColor;
                        ctx.fillText(label, badgeX + (badgeWidth / 2), badgeY + (badgeHeight / 2));
                    });
                });

                ctx.restore();
            }
        };
    }

    function createAverageLinePlugin(palette, averageValue, label) {
        return {
            id: 'gunlukAverageLine',
            afterDatasetsDraw(chart) {
                const yScale = chart.scales?.y;
                const chartArea = chart.chartArea;
                const ctx = chart.ctx;
                const value = Number(averageValue);
                if (!ctx || !chartArea || !yScale || !Number.isFinite(value) || value <= 0) {
                    return;
                }

                const y = yScale.getPixelForValue(value);
                if (!Number.isFinite(y) || y < chartArea.top || y > chartArea.bottom) {
                    return;
                }

                ctx.save();
                ctx.setLineDash([6, 6]);
                ctx.strokeStyle = palette.trendRose;
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.moveTo(chartArea.left, y);
                ctx.lineTo(chartArea.right, y);
                ctx.stroke();
                ctx.setLineDash([]);
                ctx.fillStyle = palette.trendRose;
                ctx.font = '700 12px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
                ctx.textAlign = 'right';
                ctx.textBaseline = 'bottom';
                ctx.fillText(label + ': ' + value.toFixed(1) + '%', chartArea.right - 4, y - 4);
                ctx.restore();
            }
        };
    }

    function createDoughnutCenterPlugin(palette, value, label) {
        return {
            id: 'gunlukDoughnutCenterText',
            afterDraw(chart) {
                const ctx = chart.ctx;
                const chartArea = chart.chartArea;
                if (!ctx || !chartArea) {
                    return;
                }

                const centerX = chartArea.left + ((chartArea.right - chartArea.left) / 2);
                const centerY = chartArea.top + ((chartArea.bottom - chartArea.top) / 2);
                ctx.save();
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = palette.strongTextColor;
                ctx.font = '800 24px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
                ctx.fillText(Number(value || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 }), centerX, centerY - 8);
                ctx.fillStyle = palette.textColor;
                ctx.font = '700 12px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
                ctx.fillText(label, centerX, centerY + 16);
                ctx.restore();
            }
        };
    }

    function buildOccupancySummary(seriesList, labels, palette) {
        return seriesList.map(function (series, index) {
            const color = palette.occupancySeries[index % palette.occupancySeries.length];
            const values = Array.isArray(series.DolulukOranlari)
                ? series.DolulukOranlari.map(clampPercent)
                : [];
            const moduleCounts = Array.isArray(series.ModulSayilari) ? series.ModulSayilari : [];
            const positiveValues = values.filter(function (value) { return value > 0; });
            let latestIndex = -1;

            for (let valueIndex = values.length - 1; valueIndex >= 0; valueIndex -= 1) {
                if (values[valueIndex] > 0) {
                    latestIndex = valueIndex;
                    break;
                }
            }

            return {
                label: series.Bolum || ('Bölüm ' + (index + 1)),
                average: positiveValues.length > 0
                    ? positiveValues.reduce(function (total, value) { return total + value; }, 0) / positiveValues.length
                    : 0,
                latest: latestIndex >= 0 ? values[latestIndex] : 0,
                latestLabel: latestIndex >= 0 ? (labels[latestIndex] || '') : '',
                latestModuleCount: latestIndex >= 0 ? (moduleCounts[latestIndex] || 0) : 0,
                color: color
            };
        }).filter(function (item) {
            return item.average > 0;
        }).sort(function (first, second) {
            return second.average - first.average;
        });
    }

    function createGaugeCenterPlugin(value, target, palette) {
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
                ctx.fillStyle = palette.gaugeTarget;
                ctx.font = '700 15px system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
                ctx.fillText('Hedef: ' + target.toFixed(0) + ' %', centerX, centerY + 46);
                ctx.restore();
            }
        };
    }

    function buildGaugeDataset(value, target, palette) {
        const normalizedValue = clampPercent(value);
        const normalizedTarget = clampPercent(target);

        if (normalizedValue <= normalizedTarget) {
            return {
                values: [
                    normalizedValue,
                    normalizedTarget - normalizedValue,
                    100 - normalizedTarget
                ],
                colors: [
                    palette.gaugePrimary,
                    palette.gaugeTargetZone,
                    palette.gaugeMuted
                ],
                labels: ['Gerçekleşen OEE', 'Hedefe kalan alan', 'Hedef sonrası alan']
            };
        }

        return {
            values: [
                normalizedTarget,
                normalizedValue - normalizedTarget,
                100 - normalizedValue
            ],
            colors: [
                palette.gaugePrimary,
                palette.gaugePrimaryStrong,
                palette.gaugeMuted
            ],
            labels: ['Hedefe kadar OEE', 'Hedef üstü OEE', 'Kalan alan']
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
        const hero = document.getElementById('gunlukHero');
        const page = document.getElementById('gunlukPage');
        if (summaryCard) {
            summaryCard.classList.add('summary-ultra');
        }
        if (hero) {
            hero.classList.add('hero-ultra');
        }
        if (page) {
            page.classList.add('page-ultra');
        }
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
        const horizontalPercentValueLabelPlugin = createHorizontalValueLabelPlugin(palette, formatPercentValue);
        const verticalPercentLabelPlugin = createVerticalValueLabelPlugin(palette, function (value) {
            return value.toLocaleString('tr-TR', {
                minimumFractionDigits: 1,
                maximumFractionDigits: 1
            }) + '%';
        });
        const verticalValueLabelPlugin = createVerticalValueLabelPlugin(palette, function (value) {
            return value.toLocaleString('tr-TR', {
                maximumFractionDigits: 0
            });
        });

        configureChartDefaults(palette);
        restoreChartContainers();

        const trendLabels = Array.isArray(data.TrendLabels) ? data.TrendLabels : [];
        const trendHasData = trendLabels.length > 0;

        const oeeValue = clampPercent(data.OrtalamaOee);
        const oeeTarget = 60;
        const gaugeDataset = buildGaugeDataset(oeeValue, oeeTarget, palette);
        createChart('genelOeeGaugeGrafigi', {
            type: 'doughnut',
            data: {
                labels: gaugeDataset.labels,
                datasets: [
                    {
                        data: gaugeDataset.values,
                        backgroundColor: gaugeDataset.colors,
                        borderRadius: 8,
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
            plugins: [
                createGaugeCenterPlugin(oeeValue, oeeTarget, palette)
            ]
        }, true, '');

        const machineLabels = Array.isArray(data.MakineOeeLabels) ? data.MakineOeeLabels : [];
        const machineValues = (Array.isArray(data.MakineOeeData) ? data.MakineOeeData : []).map(clampPercent);
        const machinePairs = buildSortedPairs(machineLabels, machineValues, 'desc', 12);
        const machineAxisMax = getPercentAxisMax(machinePairs.values);
        const machinePalette = createRankGradientColors(machinePairs.values.length);
        createChart('makineOeeGrafigi', {
            type: 'bar',
            data: {
                labels: machinePairs.labels,
                datasets: [
                    {
                        label: 'Ortalama OEE (%)',
                        data: machinePairs.values,
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
                        max: machineAxisMax,
                        ticks: {
                            stepSize: 10,
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
            plugins: [horizontalPercentLabelPlugin || horizontalPercentValueLabelPlugin]
        }, hasAnyData(machinePairs.values), 'Makine bazlı OEE verisi bulunamadı.');

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
                maintainAspectRatio: false,
                scales: {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        max: getPercentAxisMax([
                            data.OrtalamaPerformans,
                            data.OrtalamaKullanilabilirlik,
                            data.OrtalamaKalite
                        ]),
                        ticks: {
                            stepSize: 10,
                            callback: function (value) {
                                return value + '%';
                            }
                        },
                        grid: {
                            color: palette.gridColor
                        }
                    }
                }
            },
            plugins: [verticalPercentLabelPlugin]
        }, true, '');

        const bolumOeeLabels = Array.isArray(data.BolumOeeLabels) ? data.BolumOeeLabels : [];
        const bolumOeeValues = (Array.isArray(data.BolumOeeData) ? data.BolumOeeData : []).map(clampPercent);
        const bolumOeePairs = buildSortedPairs(bolumOeeLabels, bolumOeeValues, 'desc', 10);
        const bolumOeeAxisMax = getPercentAxisMax(bolumOeePairs.values);
        const bolumOeeAverage = bolumOeeValues.filter(function (value) { return value > 0; })
            .reduce(function (total, value, _, values) { return total + (value / values.length); }, 0);
        createChart('bolumOeeGrafigi', {
            type: 'bar',
            data: {
                labels: bolumOeePairs.labels,
                datasets: [
                    {
                        label: 'Ortalama OEE (%)',
                        data: bolumOeePairs.values,
                        backgroundColor: palette.oeeBar,
                        borderColor: palette.oeeBarBorder,
                        borderWidth: 1,
                        borderRadius: 10,
                        barThickness: 24
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
                        max: bolumOeeAxisMax,
                        ticks: {
                            stepSize: 10,
                            callback: function (value) {
                                return value + '%';
                            }
                        },
                        grid: {
                            color: palette.gridColor
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            },
            plugins: [
                verticalPercentLabelPlugin
            ]
        }, hasAnyData(bolumOeePairs.values), 'Bölüm bazlı OEE verisi bulunamadı.');

        const bolumHataLabels = Array.isArray(data.BolumHataLabels) ? data.BolumHataLabels : [];
        const bolumHataValues = Array.isArray(data.BolumHataData) ? data.BolumHataData : [];
        const bolumHataPairs = buildSortedPairs(bolumHataLabels, bolumHataValues, 'desc', 10);
        createChart('bolumHataGrafigi', {
            type: 'bar',
            data: {
                labels: bolumHataPairs.labels,
                datasets: [
                    {
                        label: 'Hatali Adet',
                        data: bolumHataPairs.values,
                        backgroundColor: palette.errorBar,
                        borderColor: palette.errorBarBorder,
                        borderWidth: 1,
                        borderRadius: 10,
                        barThickness: 24
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
        }, hasAnyData(bolumHataPairs.values), 'Bölüm bazlı hata verisi bulunamadı.');

        const personelLabels = Array.isArray(data.PersonelBolumLabels) ? data.PersonelBolumLabels : [];
        const direktPersonelValues = Array.isArray(data.DirektPersonelBolumData) ? data.DirektPersonelBolumData : [];
        const endirektPersonelValues = Array.isArray(data.EndirektPersonelBolumData) ? data.EndirektPersonelBolumData : [];
        const personelOzetLabels = Array.isArray(data.PersonelOzetLabels) && data.PersonelOzetLabels.length > 0
            ? data.PersonelOzetLabels
            : ['Direkt Çalışan Sayısı', 'Endirekt Çalışan Sayısı'];
        const personelOzetValues = Array.isArray(data.PersonelOzetData) ? data.PersonelOzetData : [];
        const personelBackButton = document.getElementById('personelChartBack');

        function renderPersonelSummaryChart() {
            if (personelBackButton) {
                personelBackButton.classList.add('d-none');
            }

            const roundedPersonelOzetValues = personelOzetValues.map(function (value) {
                return Math.round(Number(value || 0));
            });
            const personelTotal = roundedPersonelOzetValues.reduce(function (total, value) {
                return total + Number(value || 0);
            }, 0);
            return createChart('personelBolumGrafigi', {
                type: 'doughnut',
                data: {
                    labels: personelOzetLabels,
                    datasets: [
                        {
                            label: personelSeriesLabel,
                            data: roundedPersonelOzetValues,
                            backgroundColor: [palette.personnelBar, palette.trendPurpleFill],
                            borderColor: [palette.personnelBarBorder, palette.trendPurple],
                            borderWidth: 2,
                            hoverOffset: 6,
                            detailTypes: ['direct', 'indirect']
                        }
                    ]
                },
                options: {
                    maintainAspectRatio: false,
                    cutout: '62%',
                    onClick: function (_, elements) {
                        if (!elements || elements.length === 0) {
                            return;
                        }

                        const type = elements[0].index === 0 ? 'direct' : 'indirect';
                        renderPersonelDetailChart(type);
                    },
                    onHover: function (event, elements) {
                        if (event?.native?.target) {
                            event.native.target.style.cursor = elements.length ? 'pointer' : 'default';
                        }
                    },
                    plugins: {
                        legend: {
                            display: true,
                            position: 'bottom'
                        },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    return context.label + ': ' + Math.round(Number(context.parsed || 0)).toLocaleString('tr-TR');
                                }
                            }
                        }
                    }
                },
                plugins: [createDoughnutCenterPlugin(palette, personelTotal, 'toplam')]
            }, hasAnyData(roundedPersonelOzetValues), 'Personel özet verisi bulunamadı.');
        }

        function renderPersonelDetailChart(type) {
            const canvasId = 'personelBolumGrafigi';
            const parent = chartParents.get(canvasId);
            const template = chartTemplates.get(canvasId);
            if (!parent || !template) {
                return;
            }

            const existingIndex = chartInstances.findIndex(function (instance) {
                return instance.canvas && instance.canvas.id === canvasId;
            });
            if (existingIndex >= 0) {
                chartInstances[existingIndex].destroy();
                chartInstances.splice(existingIndex, 1);
            }

            parent.innerHTML = template;
            if (personelBackButton) {
                personelBackButton.classList.remove('d-none');
            }

            const values = (type === 'direct' ? direktPersonelValues : endirektPersonelValues)
                .map(function (value) {
                    return Math.round(Number(value || 0));
                });
            const title = type === 'direct' ? 'Direkt Çalışan Sayısı' : 'Endirekt Çalışan Sayısı';
            createChart(canvasId, {
                type: 'bar',
                data: {
                    labels: personelLabels,
                    datasets: [
                        {
                            label: title,
                            data: values,
                            backgroundColor: type === 'direct' ? palette.personnelBar : palette.trendPurpleFill,
                            borderColor: type === 'direct' ? palette.personnelBarBorder : palette.trendPurple,
                            borderWidth: 1,
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
                            },
                            ticks: {
                                maxRotation: 0,
                                autoSkip: false
                            }
                        },
                        y: {
                            beginAtZero: true,
                            grid: {
                                color: palette.gridColor
                            }
                        }
                    },
                    plugins: {
                        legend: {
                            display: true
                        },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    return context.dataset.label + ': ' + Math.round(Number(context.parsed.y || 0)).toLocaleString('tr-TR');
                                }
                            }
                        }
                    }
                },
                plugins: [verticalValueLabelPlugin]
            }, hasAnyData(values), title + ' bölüm detayı bulunamadı.');
        }

        if (personelBackButton) {
            personelBackButton.onclick = function () {
                const canvasId = 'personelBolumGrafigi';
                const existingIndex = chartInstances.findIndex(function (instance) {
                    return instance.canvas && instance.canvas.id === canvasId;
                });
                if (existingIndex >= 0) {
                    chartInstances[existingIndex].destroy();
                    chartInstances.splice(existingIndex, 1);
                }

                const parent = chartParents.get(canvasId);
                const template = chartTemplates.get(canvasId);
                if (parent && template) {
                    parent.innerHTML = template;
                }
                renderPersonelSummaryChart();
            };
        }

        renderPersonelSummaryChart();

        const planUyumLabels = Array.isArray(data.PlanUyumBolumLabels) ? data.PlanUyumBolumLabels : [];
        const planUyumValues = (Array.isArray(data.PlanUyumBolumData) ? data.PlanUyumBolumData : []).map(clampPercent);
        const planUyumPairs = buildSortedPairs(planUyumLabels, planUyumValues, 'desc', 10);
        const planUyumAxisMax = getPercentAxisMax(planUyumPairs.values);
        createChart('planUyumBolumGrafigi', {
            type: 'bar',
            data: {
                labels: planUyumPairs.labels,
                datasets: [
                    {
                        label: 'Plana Uyum (%)',
                        data: planUyumPairs.values,
                        backgroundColor: palette.componentBars[1],
                        borderColor: palette.componentBorders[1],
                        borderWidth: 1,
                        borderRadius: 10,
                        barThickness: 24
                    }
                ]
            },
            options: {
                indexAxis: 'y',
                maintainAspectRatio: false,
                scales: {
                    x: {
                        beginAtZero: true,
                        max: planUyumAxisMax,
                        ticks: {
                            stepSize: 10,
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
            plugins: [horizontalPercentLabelPlugin || horizontalPercentValueLabelPlugin]
        }, hasAnyData(planUyumPairs.values), 'Plana uyum verisi bulunamadı.');

        const hataNedenLabels = Array.isArray(data.HataNedenLabels) ? data.HataNedenLabels : [];
        const hataNedenValues = Array.isArray(data.HataNedenData) ? data.HataNedenData : [];
        const hataNedenPairs = buildSortedPairs(hataNedenLabels, hataNedenValues, 'desc', 8);
        const hataNedenTotal = hataNedenPairs.values.reduce(function (total, value) {
            return total + (Number(value) || 0);
        }, 0);
        createChart('hataNedenGenelGrafigi', {
            type: 'doughnut',
            data: {
                labels: hataNedenPairs.labels,
                datasets: [
                    {
                        data: hataNedenPairs.values,
                        backgroundColor: hataNedenPairs.values.map(function (_, index) {
                            return palette.donutColors[index % palette.donutColors.length] + 'cc';
                        }),
                        borderColor: hataNedenPairs.values.map(function (_, index) {
                            return palette.donutColors[index % palette.donutColors.length];
                        }),
                        borderWidth: 2,
                        hoverOffset: 8
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                cutout: '52%',
                radius: '92%',
                plugins: {
                    legend: {
                        display: true,
                        position: 'bottom'
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                const value = Number(context.parsed || 0).toLocaleString('tr-TR');
                                const total = hataNedenTotal || 1;
                                const ratio = ((Number(context.parsed || 0) / total) * 100).toLocaleString('tr-TR', {
                                    minimumFractionDigits: 1,
                                    maximumFractionDigits: 1
                                });
                                return context.label + ': ' + value + ' (' + ratio + '%)';
                            }
                        }
                    }
                }
            },
            plugins: [createDoughnutCenterPlugin(palette, hataNedenTotal, 'toplam hata')]
        }, hasAnyData(hataNedenPairs.values), 'Hata nedeni dağılımı için veri bulunamadı.');

        const modulTrendValues = Array.isArray(data.ModulTrendData) ? data.ModulTrendData : [];
        createChart('duraklamaTrendGrafigi', {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: 'Depoya Giren Modul',
                        data: modulTrendValues,
                        borderColor: palette.personnelBarBorder,
                        backgroundColor: palette.personnelBar,
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
        }, trendHasData && hasAnyData(modulTrendValues), 'Depo modül trend verisi bulunamadı.');

        const istasyonDolulukSerileri = Array.isArray(data.IstasyonDolulukSerileri) ? data.IstasyonDolulukSerileri : [];
        const occupancySummary = buildOccupancySummary(istasyonDolulukSerileri, trendLabels, palette);
        const occupancyAverageValues = occupancySummary.map(function (item) { return item.average; });
        createChart('genelHataTrend', {
            type: 'bar',
            data: {
                labels: occupancySummary.map(function (item) { return item.label; }),
                datasets: [
                    {
                        label: 'Ortalama Doluluk (%)',
                        data: occupancyAverageValues,
                        backgroundColor: occupancySummary.map(function (item) { return item.color + 'b8'; }),
                        borderColor: occupancySummary.map(function (item) { return item.color; }),
                        borderWidth: 1,
                        borderRadius: 12,
                        barThickness: 26
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                interaction: {
                    mode: 'nearest',
                    intersect: true
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                const item = occupancySummary[context.dataIndex];
                                const average = Number(context.parsed.y || 0).toLocaleString('tr-TR', {
                                    minimumFractionDigits: 1,
                                    maximumFractionDigits: 1
                                });
                                if (!item) {
                                    return 'Ortalama: ' + average + '%';
                                }

                                const latest = Number(item.latest || 0).toLocaleString('tr-TR', {
                                    minimumFractionDigits: 1,
                                    maximumFractionDigits: 1
                                });
                                const moduleCount = Number(item.latestModuleCount || 0).toLocaleString('tr-TR');
                                const latestLabel = item.latestLabel ? ' | Son gun: ' + item.latestLabel + ' ' + latest + '%' : '';
                                return 'Ortalama: ' + average + '%' + latestLabel + ' | Modül: ' + moduleCount;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        max: getPercentAxisMax(occupancyAverageValues),
                        ticks: {
                            stepSize: 10,
                            callback: function (value) {
                                return value + '%';
                            }
                        },
                        grid: {
                            color: palette.gridColor
                        }
                    }
                }
            },
            plugins: [verticalPercentLabelPlugin]
        }, trendHasData && hasAnyData(occupancyAverageValues), 'İstasyon doluluk verisi bulunamadı.');
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
