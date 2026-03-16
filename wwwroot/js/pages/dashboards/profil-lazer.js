document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('profil-lazer-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent || '{}');
    const data = normalizePayloadKeys(payload.model || {});
    const chartCanvasIds = [
        'pastaGrafigi',
        'cizgiGrafigi',
        'hataliTrendGrafigi',
        'urunSureGrafigi',
        'hataNedenGrafigi',
        'hataUrunSonucGrafigi'
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

    function getThemePalette() {
        const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
        return {
            textColor: isDarkTheme ? '#9aa8bd' : '#475569',
            strongTextColor: isDarkTheme ? '#f8fbff' : '#1f2937',
            gridColor: isDarkTheme ? 'rgba(148, 163, 184, 0.14)' : 'rgba(148, 163, 184, 0.28)',
            productionLine: isDarkTheme ? '#67e8f9' : '#0891b2',
            productionFill: isDarkTheme ? 'rgba(103, 232, 249, 0.18)' : 'rgba(8, 145, 178, 0.12)',
            errorLine: isDarkTheme ? '#fb7185' : '#e11d48',
            errorFill: isDarkTheme ? 'rgba(251, 113, 133, 0.14)' : 'rgba(225, 29, 72, 0.12)',
            durationBar: isDarkTheme ? 'rgba(196, 181, 253, 0.82)' : 'rgba(124, 58, 237, 0.72)',
            durationBarBorder: isDarkTheme ? 'rgba(221, 214, 254, 1)' : 'rgba(109, 40, 217, 1)',
            donutColors: [
                '#67e8f9', '#f59e0b', '#34d399', '#a78bfa', '#fb7185',
                '#38bdf8', '#f97316', '#22c55e', '#eab308', '#818cf8'
            ]
        };
    }

    function configureChartDefaults(palette) {
        Chart.defaults.color = palette.textColor;
        Chart.defaults.font.family = 'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif';
        Chart.defaults.plugins.legend.labels.usePointStyle = true;
    }

    function hasAnyData(values) {
        return Array.isArray(values) && values.some(function (value) {
            return Number(value || 0) !== 0;
        });
    }

    function destroyCharts() {
        while (chartInstances.length > 0) {
            chartInstances.pop().destroy();
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

        parent.innerHTML = '<div class="profile-empty-state">' + message + '</div>';
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

        createChart('pastaGrafigi', {
            type: 'doughnut',
            data: {
                labels: data.ProfilIsimleri || [],
                datasets: [
                    {
                        data: data.ProfilUretimAdetleri || [],
                        backgroundColor: palette.donutColors,
                        borderWidth: 0,
                        cutout: '58%'
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top'
                    }
                }
            }
        }, hasAnyData(data.ProfilUretimAdetleri), 'Profil dagilim verisi bulunamadi.');

        createChart('cizgiGrafigi', {
            type: 'line',
            data: {
                labels: data.Son7GunTarihleri || [],
                datasets: [
                    {
                        label: 'Gunluk Uretim Adedi',
                        data: data.GunlukUretimSayilari || [],
                        borderColor: palette.productionLine,
                        backgroundColor: palette.productionFill,
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
                        }
                    }
                }
            }
        }, hasAnyData(data.GunlukUretimSayilari), 'Uretim trend verisi bulunamadi.');

        createChart('hataliTrendGrafigi', {
            type: 'line',
            data: {
                labels: data.Son7GunTarihleri || [],
                datasets: [
                    {
                        label: 'Hatali Urun Adedi',
                        data: data.GunlukHataliUrunSayilari || [],
                        borderColor: palette.errorLine,
                        backgroundColor: palette.errorFill,
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
                        }
                    }
                }
            }
        }, hasAnyData(data.GunlukHataliUrunSayilari), 'Hatali urun trend verisi bulunamadi.');

        createChart('urunSureGrafigi', {
            type: 'bar',
            data: {
                labels: data.UrunIsimleri || [],
                datasets: [
                    {
                        label: 'Sure Yuzdesi (%)',
                        data: data.UrunHarcananSure || [],
                        backgroundColor: palette.durationBar,
                        borderColor: palette.durationBarBorder,
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
                        }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
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
            }
        }, hasAnyData(data.UrunHarcananSure), 'Sure dagilim verisi bulunamadi.');

        createChart('hataNedenGrafigi', {
            type: 'doughnut',
            data: {
                labels: data.HataNedenleri || [],
                datasets: [
                    {
                        data: data.HataNedenAdetleri || [],
                        backgroundColor: palette.donutColors,
                        borderWidth: 0,
                        cutout: '58%'
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top'
                    }
                }
            }
        }, hasAnyData(data.HataNedenAdetleri), 'Hata nedeni verisi bulunamadi.');

        createChart('hataUrunSonucGrafigi', {
            type: 'doughnut',
            data: {
                labels: data.HataUrunSonuclari || [],
                datasets: [
                    {
                        data: data.HataUrunSonucAdetleri || [],
                        backgroundColor: palette.donutColors,
                        borderWidth: 0,
                        cutout: '58%'
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top'
                    }
                }
            }
        }, hasAnyData(data.HataUrunSonucAdetleri), 'Hata sonucu verisi bulunamadi.');
    }

    renderCharts();

    let activeTheme = document.documentElement.getAttribute('data-theme');
    const themeObserver = new MutationObserver(function () {
        const nextTheme = document.documentElement.getAttribute('data-theme');
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
