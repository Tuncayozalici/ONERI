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

    function buildOccupancyDatasets(seriesList, palette) {
        return seriesList.map(function (series, index) {
            const color = palette.occupancySeries[index % palette.occupancySeries.length];
            return {
                label: series.Bolum || ('Bolum ' + (index + 1)),
                data: Array.isArray(series.DolulukOranlari) ? series.DolulukOranlari.map(clampPercent) : [],
                moduleCounts: Array.isArray(series.ModulSayilari) ? series.ModulSayilari : [],
                borderColor: color,
                backgroundColor: color + '22',
                tension: 0.28,
                borderWidth: 2,
                fill: false,
                pointRadius: 3,
                pointHoverRadius: 5
            };
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
                labels: ['Gerceklesen OEE', 'Hedefe kalan alan', 'Hedef sonrasi alan']
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
            labels: ['Hedefe kadar OEE', 'Hedef ustu OEE', 'Kalan alan']
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
        const hero = document.getElementById('gunlukHero');
        const page = document.getElementById('gunlukPage');
        if (!summaryCard || !summaryToggle) {
            return;
        }

        const saved = localStorage.getItem('summary_ultra');
        const enabled = saved === '1';
        summaryCard.classList.toggle('summary-ultra', enabled);
        if (hero) {
            hero.classList.toggle('hero-ultra', enabled);
        }
        if (page) {
            page.classList.toggle('page-ultra', enabled);
        }
        summaryToggle.checked = enabled;

        summaryToggle.addEventListener('change', function () {
            summaryCard.classList.toggle('summary-ultra', summaryToggle.checked);
            if (hero) {
                hero.classList.toggle('hero-ultra', summaryToggle.checked);
            }
            if (page) {
                page.classList.toggle('page-ultra', summaryToggle.checked);
            }
            localStorage.setItem('summary_ultra', summaryToggle.checked ? '1' : '0');
        });
    }

    function setupSectionPicker() {
        const options = Array.from(document.querySelectorAll('.js-section-option'));
        const cta = document.getElementById('sectionPickerCta');
        const ctaLabel = document.getElementById('sectionPickerCtaLabel');
        if (options.length === 0 || !cta || !ctaLabel) {
            return;
        }

        function applySelection(option) {
            if (!option) {
                return;
            }

            options.forEach(function (item) {
                const isSelected = item === option;
                item.classList.toggle('is-selected', isSelected);
                item.setAttribute('aria-selected', isSelected ? 'true' : 'false');
            });

            const title = option.dataset.sectionTitle || 'Bolum';
            const url = option.dataset.sectionUrl || '#';
            const key = option.dataset.sectionKey || title;

            cta.setAttribute('href', url);
            ctaLabel.textContent = title + ' ekranina git';
            localStorage.setItem('gunluk_selected_section', key);
        }

        const savedKey = localStorage.getItem('gunluk_selected_section');
        const initialOption = options.find(function (item) {
            return item.dataset.sectionKey === savedKey;
        }) || options.find(function (item) {
            return item.classList.contains('is-selected');
        }) || options[0];

        options.forEach(function (option) {
            option.addEventListener('click', function () {
                applySelection(option);
            });
        });

        applySelection(initialOption);
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
        const machineAxisMax = getPercentAxisMax(machineValues);
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
        const bolumOeeAxisMax = getPercentAxisMax(bolumOeeValues);
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

            return createChart('personelBolumGrafigi', {
                type: 'bar',
                data: {
                    labels: personelOzetLabels,
                    datasets: [
                        {
                            label: personelSeriesLabel,
                            data: personelOzetValues,
                            backgroundColor: [palette.personnelBar, palette.trendPurpleFill],
                            borderColor: [palette.personnelBarBorder, palette.trendPurple],
                            borderWidth: 1,
                            borderRadius: 10,
                            detailTypes: ['direct', 'indirect']
                        }
                    ]
                },
                options: {
                    indexAxis: 'y',
                    maintainAspectRatio: false,
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
            }, hasAnyData(personelOzetValues), 'Personel ozet verisi bulunamadi.');
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

            const values = type === 'direct' ? direktPersonelValues : endirektPersonelValues;
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
                            display: true
                        }
                    }
                },
                plugins: [horizontalValueLabelPlugin]
            }, hasAnyData(values), title + ' bolum detayi bulunamadi.');
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
        const planUyumAxisMax = getPercentAxisMax(planUyumValues);
        createChart('planUyumBolumGrafigi', {
            type: 'bar',
            data: {
                labels: planUyumLabels,
                datasets: [
                    {
                        label: 'Plana Uyum (%)',
                        data: planUyumValues,
                        backgroundColor: palette.componentBars[1],
                        borderColor: palette.componentBorders[1],
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
            plugins: horizontalPercentLabelPlugin ? [horizontalPercentLabelPlugin] : []
        }, hasAnyData(planUyumValues), 'Plana uyum verisi bulunamadi.');

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
        }, trendHasData && hasAnyData(modulTrendValues), 'Depo modul trend verisi bulunamadi.');

        const istasyonDolulukSerileri = Array.isArray(data.IstasyonDolulukSerileri) ? data.IstasyonDolulukSerileri : [];
        const occupancyDatasets = buildOccupancyDatasets(istasyonDolulukSerileri, palette);
        const occupancyHasData = occupancyDatasets.some(function (dataset) {
            return hasAnyData(dataset.data);
        });
        createChart('genelHataTrend', {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: occupancyDatasets
            },
            options: {
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                const doluluk = Number(context.parsed.y || 0).toFixed(2);
                                const moduleCounts = Array.isArray(context.dataset.moduleCounts)
                                    ? context.dataset.moduleCounts
                                    : [];
                                const moduleCount = moduleCounts[context.dataIndex] ?? 0;
                                return context.dataset.label + ': ' + doluluk + '% | Modul: ' + Number(moduleCount).toLocaleString('tr-TR');
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
                        max: 100,
                        ticks: {
                            callback: function (value) {
                                return value + '%';
                            }
                        },
                        grid: {
                            color: palette.gridColor
                        }
                    }
                }
            }
        }, trendHasData && occupancyHasData, 'Istasyon doluluk verisi bulunamadi.');
    }

    setupUltraToggle();
    setupSectionPicker();
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
