document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('hatali-parca-dashboard-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent || '{}');
    const data = normalizePayloadKeys(payload.model || {});
    const chartCanvasIds = [
        'hataAdetTrend',
        'hataNedenGrafigi',
        'bolumGrafigi',
        'operatorGrafigi',
        'kalinlikGrafigi',
        'renkGrafigi',
        'kesimDurumGrafigi',
        'pvcDurumGrafigi'
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
            trendLine: isDarkTheme ? '#fb7185' : '#e11d48',
            trendFill: isDarkTheme ? 'rgba(251, 113, 133, 0.14)' : 'rgba(225, 29, 72, 0.12)',
            departmentBar: isDarkTheme ? 'rgba(96, 165, 250, 0.78)' : 'rgba(37, 99, 235, 0.72)',
            departmentBarSelected: isDarkTheme ? 'rgba(59, 130, 246, 1)' : 'rgba(29, 78, 216, 0.96)',
            operatorBar: isDarkTheme ? 'rgba(167, 139, 250, 0.78)' : 'rgba(124, 58, 237, 0.72)',
            detailBar: isDarkTheme ? 'rgba(45, 212, 191, 0.74)' : 'rgba(13, 148, 136, 0.68)',
            detailBarAlt: isDarkTheme ? 'rgba(251, 191, 36, 0.74)' : 'rgba(217, 119, 6, 0.68)'
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

        parent.innerHTML = `<div class="hatali-empty-state">${message}</div>`;
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

    function renderCharts() {
        const palette = getThemePalette();
        configureChartDefaults(palette);
        restoreChartContainers();

        const donutColors = [
            '#fb7185', '#60a5fa', '#fbbf24', '#2dd4bf', '#a78bfa',
            '#fb923c', '#94a3b8', '#22c55e', '#f472b6', '#38bdf8'
        ];
        const titleElement = document.getElementById('hataNedenPanelTitle');
        const normalizeKey = function (value) {
            return String(value ?? '').trim().toLocaleLowerCase('tr-TR');
        };
        const bolumBazliMap = new Map(
            (Array.isArray(data.BolumBazliHataNedenleri) ? data.BolumBazliHataNedenleri : []).map(function (item) {
                return [
                    normalizeKey(item.Bolum),
                    {
                        labels: Array.isArray(item.NedenLabels) ? item.NedenLabels : [],
                        values: Array.isArray(item.NedenData) ? item.NedenData : []
                    }
                ];
            })
        );

        createChart('hataAdetTrend', {
            type: 'line',
            data: {
                labels: data.TrendLabels || [],
                datasets: [
                    {
                        label: 'Hatalı Adet',
                        data: data.HataAdetTrendData || [],
                        borderColor: palette.trendLine,
                        backgroundColor: palette.trendFill,
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
        }, hasAnyData(data.HataAdetTrendData), 'Seçilen dönem için hatalı parça trend verisi bulunamadı.');

        const hataNedenChart = createChart('hataNedenGrafigi', {
            type: 'doughnut',
            data: {
                labels: data.HataNedenLabels || [],
                datasets: [
                    {
                        data: data.HataNedenData || [],
                        backgroundColor: donutColors,
                        borderWidth: 0
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                cutout: '58%'
            }
        }, hasAnyData(data.HataNedenData), 'Hata nedeni dağılımı için veri bulunamadı.');

        let seciliBolum = null;

        function updateHataNedenByBolum(bolum) {
            if (!hataNedenChart) {
                return;
            }

            const detay = bolum ? bolumBazliMap.get(normalizeKey(bolum)) : null;
            if (detay && detay.labels.length > 0) {
                hataNedenChart.data.labels = detay.labels;
                hataNedenChart.data.datasets[0].data = detay.values;
                if (titleElement) {
                    titleElement.textContent = `Hata Nedenleri - ${bolum}`;
                }
            } else {
                hataNedenChart.data.labels = data.HataNedenLabels || [];
                hataNedenChart.data.datasets[0].data = data.HataNedenData || [];
                if (titleElement) {
                    titleElement.textContent = 'Hata Nedenleri';
                }
            }

            hataNedenChart.update();
        }

        const bolumChart = createChart('bolumGrafigi', {
            type: 'bar',
            data: {
                labels: data.BolumLabels || [],
                datasets: [
                    {
                        label: 'Hatalı Adet',
                        data: data.BolumData || [],
                        backgroundColor: (data.BolumLabels || []).map(function () { return palette.departmentBar; }),
                        borderRadius: 10
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                onClick: function (_, elements) {
                    if (!elements || !elements.length) {
                        return;
                    }

                    const index = elements[0].index;
                    const tiklananBolum = data.BolumLabels[index];
                    seciliBolum = seciliBolum === tiklananBolum ? null : tiklananBolum;

                    bolumChart.data.datasets[0].backgroundColor = (data.BolumLabels || []).map(function (label) {
                        return label === seciliBolum ? palette.departmentBarSelected : palette.departmentBar;
                    });
                    bolumChart.update();
                    updateHataNedenByBolum(seciliBolum);
                },
                onHover: function (event, elements) {
                    if (event?.native?.target) {
                        event.native.target.style.cursor = elements.length ? 'pointer' : 'default';
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
            }
        }, hasAnyData(data.BolumData), 'Bölüme göre hata grafiği için veri bulunamadı.');

        createChart('operatorGrafigi', {
            type: 'bar',
            data: {
                labels: data.OperatorLabels || [],
                datasets: [
                    {
                        label: 'Hatalı Adet',
                        data: data.OperatorData || [],
                        backgroundColor: palette.operatorBar,
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
            }
        }, hasAnyData(data.OperatorData), 'Operatöre göre hata grafiği için veri bulunamadı.');

        createChart('kalinlikGrafigi', {
            type: 'bar',
            data: {
                labels: data.KalinlikLabels || [],
                datasets: [
                    {
                        label: 'Hatalı Adet',
                        data: data.KalinlikData || [],
                        backgroundColor: palette.detailBar,
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
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            }
        }, hasAnyData(data.KalinlikData), 'Kalınlık dağılımı için veri bulunamadı.');

        createChart('renkGrafigi', {
            type: 'bar',
            data: {
                labels: data.RenkLabels || [],
                datasets: [
                    {
                        label: 'Hatalı Adet',
                        data: data.RenkData || [],
                        backgroundColor: palette.detailBarAlt,
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
            }
        }, hasAnyData(data.RenkData), 'Renk dağılımı için veri bulunamadı.');

        createChart('kesimDurumGrafigi', {
            type: 'bar',
            data: {
                labels: data.KesimDurumLabels || [],
                datasets: [
                    {
                        label: 'Hatalı Adet',
                        data: data.KesimDurumData || [],
                        backgroundColor: palette.departmentBar,
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
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            }
        }, hasAnyData(data.KesimDurumData), 'Kesim durumu dağılımı için veri bulunamadı.');

        createChart('pvcDurumGrafigi', {
            type: 'bar',
            data: {
                labels: data.PvcDurumLabels || [],
                datasets: [
                    {
                        label: 'Hatalı Adet',
                        data: data.PvcDurumData || [],
                        backgroundColor: palette.operatorBar,
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
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            }
        }, hasAnyData(data.PvcDurumData), 'PVC durumu dağılımı için veri bulunamadı.');
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
