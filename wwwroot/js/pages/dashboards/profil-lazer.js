document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('profil-lazer-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent || '{}');
    const data = normalizePayloadKeys(payload.model || {});
    const chartCanvasIds = [
        'oeeTrendGrafigi',
        'makineOeeGrafigi',
        'sureDengeGrafigi',
        'duraklamaNedenGrafigi',
        'profilDagilimGrafigi',
        'musteriGrafigi',
        'hataTrendGrafigi'
    ];
    const chartParents = new Map();
    const chartTemplates = new Map();
    const chartInstances = [];
    const duraklamaTitleElement = document.getElementById('duraklamaPanelTitle');
    const duraklamaDescriptionElement = document.getElementById('duraklamaPanelDescription');
    let selectedMachine = null;
    let machineOeeChart = null;
    let duraklamaChart = null;

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
            gridColor: isDarkTheme ? 'rgba(148, 163, 184, 0.16)' : 'rgba(148, 163, 184, 0.28)',
            oeeLine: isDarkTheme ? '#fbbf24' : '#d97706',
            performansLine: isDarkTheme ? '#67e8f9' : '#0891b2',
            kullanilabilirlikLine: isDarkTheme ? '#22c55e' : '#15803d',
            kaliteLine: isDarkTheme ? '#f472b6' : '#db2777',
            machineBar: isDarkTheme ? 'rgba(251, 191, 36, 0.82)' : 'rgba(217, 119, 6, 0.76)',
            machineBarBorder: isDarkTheme ? '#fde68a' : '#92400e',
            usedDuration: isDarkTheme ? 'rgba(14, 165, 233, 0.8)' : 'rgba(2, 132, 199, 0.76)',
            remainingDuration: isDarkTheme ? 'rgba(100, 116, 139, 0.74)' : 'rgba(71, 85, 105, 0.64)',
            customerBar: isDarkTheme ? 'rgba(45, 212, 191, 0.82)' : 'rgba(13, 148, 136, 0.75)',
            errorBar: isDarkTheme ? 'rgba(251, 113, 133, 0.84)' : 'rgba(225, 29, 72, 0.76)',
            donutColors: [
                '#f59e0b', '#06b6d4', '#22c55e', '#e11d48',
                '#8b5cf6', '#f97316', '#14b8a6', '#64748b'
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

    function percentTick(value) {
        return value + '%';
    }

    function getMachineDuraklamaDataset(machineName) {
        if (!machineName) {
            return {
                labels: data.DuraklamaNedenLabels || [],
                values: data.DuraklamaNedenData || []
            };
        }

        const machineBreakdowns = Array.isArray(data.MakineDuraklamaDagilimlari)
            ? data.MakineDuraklamaDagilimlari
            : [];
        const selectedBreakdown = machineBreakdowns.find(function (item) {
            return item && item.Makine === machineName;
        });

        return {
            labels: selectedBreakdown && Array.isArray(selectedBreakdown.DuraklamaNedenLabels)
                ? selectedBreakdown.DuraklamaNedenLabels
                : [],
            values: selectedBreakdown && Array.isArray(selectedBreakdown.DuraklamaNedenData)
                ? selectedBreakdown.DuraklamaNedenData
                : []
        };
    }

    function syncDuraklamaPanelText(machineName) {
        if (!duraklamaTitleElement || !duraklamaDescriptionElement) {
            return;
        }

        if (machineName) {
            duraklamaTitleElement.textContent = 'Duraklama Nedenleri - ' + machineName;
            duraklamaDescriptionElement.textContent = 'Seçili makinenin kayıtlı duraklama süreleri gösteriliyor. Aynı çubuğa tekrar tıklayarak filtreyi kaldırabilirsiniz.';
            return;
        }

        duraklamaTitleElement.textContent = 'Duraklama Nedenleri';
        duraklamaDescriptionElement.textContent = 'Makine seçimi yapıldığında grafik sadece o makinenin kayıtlarını gösterir.';
    }

    function buildMachineBarStyles(machineLabels, palette) {
        const selectedMachineIndex = machineLabels.findIndex(function (label) {
            return label === selectedMachine;
        });

        return {
            backgroundColor: machineLabels.map(function (_, index) {
                return index === selectedMachineIndex
                    ? palette.machineBarBorder
                    : palette.machineBar;
            }),
            borderColor: machineLabels.map(function (_, index) {
                return index === selectedMachineIndex
                    ? palette.strongTextColor
                    : palette.machineBarBorder;
            }),
            borderWidth: machineLabels.map(function (_, index) {
                return index === selectedMachineIndex ? 2 : 1;
            })
        };
    }

    function updateMachineSelection(palette) {
        const machineLabels = data.MakineLabels || [];
        const machineDuraklamaDataset = getMachineDuraklamaDataset(selectedMachine);

        syncDuraklamaPanelText(selectedMachine);

        if (machineOeeChart) {
            const machineStyles = buildMachineBarStyles(machineLabels, palette);
            machineOeeChart.data.datasets[0].backgroundColor = machineStyles.backgroundColor;
            machineOeeChart.data.datasets[0].borderColor = machineStyles.borderColor;
            machineOeeChart.data.datasets[0].borderWidth = machineStyles.borderWidth;
            machineOeeChart.update();
        }

        if (duraklamaChart) {
            duraklamaChart.data.labels = machineDuraklamaDataset.labels;
            duraklamaChart.data.datasets[0].data = machineDuraklamaDataset.values;
            duraklamaChart.update();
        }
    }

    function renderCharts() {
        const palette = getThemePalette();
        configureChartDefaults(palette);
        restoreChartContainers();
        syncDuraklamaPanelText(selectedMachine);

        const machineLabels = data.MakineLabels || [];
        const machineStyles = buildMachineBarStyles(machineLabels, palette);
        const machineDuraklamaDataset = getMachineDuraklamaDataset(selectedMachine);

        createChart('oeeTrendGrafigi', {
            type: 'line',
            data: {
                labels: data.TrendTarihleri || [],
                datasets: [
                    {
                        label: 'OEE',
                        data: data.OeeTrendData || [],
                        borderColor: palette.oeeLine,
                        backgroundColor: 'transparent',
                        borderWidth: 3,
                        tension: 0.28
                    },
                    {
                        label: 'Performans',
                        data: data.PerformansTrendData || [],
                        borderColor: palette.performansLine,
                        backgroundColor: 'transparent',
                        tension: 0.28
                    },
                    {
                        label: 'Kullanılabilirlik',
                        data: data.KullanilabilirlikTrendData || [],
                        borderColor: palette.kullanilabilirlikLine,
                        backgroundColor: 'transparent',
                        tension: 0.28
                    },
                    {
                        label: 'Kalite',
                        data: data.KaliteTrendData || [],
                        borderColor: palette.kaliteLine,
                        backgroundColor: 'transparent',
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
                        suggestedMax: 100,
                        ticks: {
                            callback: percentTick
                        },
                        grid: {
                            color: palette.gridColor
                        }
                    }
                }
            }
        }, hasAnyData(data.OeeTrendData) || hasAnyData(data.PerformansTrendData) || hasAnyData(data.KullanilabilirlikTrendData) || hasAnyData(data.KaliteTrendData), 'Seçilen dönem için OEE bileşen trend verisi bulunamadı.');

        machineOeeChart = createChart('makineOeeGrafigi', {
            type: 'bar',
            data: {
                labels: machineLabels,
                datasets: [
                    {
                        label: 'Ortalama OEE (%)',
                        data: data.MakineOeeData || [],
                        backgroundColor: machineStyles.backgroundColor,
                        borderColor: machineStyles.borderColor,
                        borderWidth: machineStyles.borderWidth,
                        borderRadius: 10
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                onClick: function (_, elements, chart) {
                    if (!elements.length) {
                        return;
                    }

                    const clickedIndex = elements[0].index;
                    const clickedMachine = chart.data.labels[clickedIndex];
                    selectedMachine = selectedMachine === clickedMachine ? null : clickedMachine;
                    updateMachineSelection(palette);
                },
                onHover: function (event, elements) {
                    const target = event && event.native && event.native.target;
                    if (target) {
                        target.style.cursor = elements.length ? 'pointer' : 'default';
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
                        suggestedMax: 100,
                        ticks: {
                            callback: percentTick
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
        }, hasAnyData(data.MakineOeeData), 'Makine bazlı OEE verisi bulunamadı.');

        createChart('sureDengeGrafigi', {
            type: 'bar',
            data: {
                labels: machineLabels,
                datasets: [
                    {
                        label: 'Kullanılan süre (dk)',
                        data: data.MakineKullanilanSureData || [],
                        backgroundColor: palette.usedDuration,
                        borderRadius: 8
                    },
                    {
                        label: 'Kalan süre (dk)',
                        data: data.MakineKalanSureData || [],
                        backgroundColor: palette.remainingDuration,
                        borderRadius: 8
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                scales: {
                    x: {
                        stacked: true,
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        stacked: true,
                        beginAtZero: true,
                        grid: {
                            color: palette.gridColor
                        }
                    }
                }
            }
        }, hasAnyData(data.MakineKullanilanSureData) || hasAnyData(data.MakineKalanSureData), 'Makine bazlı süre dengesi verisi bulunamadı.');

        duraklamaChart = createChart('duraklamaNedenGrafigi', {
            type: 'doughnut',
            data: {
                labels: machineDuraklamaDataset.labels,
                datasets: [
                    {
                        data: machineDuraklamaDataset.values,
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
        }, hasAnyData(machineDuraklamaDataset.values), selectedMachine
            ? selectedMachine + ' için duraklama verisi bulunamadı.'
            : 'Duraklama verisi bulunamadı.');

        createChart('profilDagilimGrafigi', {
            type: 'doughnut',
            data: {
                labels: data.ProfilLabels || [],
                datasets: [
                    {
                        data: data.ProfilParcaData || [],
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
        }, hasAnyData(data.ProfilParcaData), 'Profil dağılım verisi bulunamadı.');

        createChart('musteriGrafigi', {
            type: 'bar',
            data: {
                labels: data.MusteriLabels || [],
                datasets: [
                    {
                        label: 'Parça sayısı',
                        data: data.MusteriParcaData || [],
                        backgroundColor: palette.customerBar,
                        borderRadius: 10,
                        maxBarThickness: 36
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
        }, hasAnyData(data.MusteriParcaData), 'Müşteri bazlı parça verisi bulunamadı.');

        createChart('hataTrendGrafigi', {
            type: 'bar',
            data: {
                labels: data.TrendTarihleri || [],
                datasets: [
                    {
                        label: 'Hata sayısı',
                        data: data.HataTrendData || [],
                        backgroundColor: palette.errorBar,
                        borderRadius: 10,
                        maxBarThickness: 34
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
                            precision: 0
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
        }, hasAnyData(data.HataTrendData), 'Günlük hata verisi bulunamadı.');
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
