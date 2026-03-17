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
        'makineUretimGrafigi',
        'urunSureGrafigi',
        'duraklamaNedenGrafigi'
    ];
    const chartParents = new Map();
    const chartTemplates = new Map();
    const chartInstances = [];
    const duraklamaTitleElement = document.getElementById('duraklamaPanelTitle');
    const duraklamaDescriptionElement = document.getElementById('duraklamaPanelDescription');
    let selectedMachine = null;
    let machineProductionChart = null;
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
            gridColor: isDarkTheme ? 'rgba(148, 163, 184, 0.14)' : 'rgba(148, 163, 184, 0.28)',
            productionLine: isDarkTheme ? '#67e8f9' : '#0891b2',
            productionFill: isDarkTheme ? 'rgba(103, 232, 249, 0.18)' : 'rgba(8, 145, 178, 0.12)',
            machineBar: isDarkTheme ? 'rgba(34, 197, 94, 0.8)' : 'rgba(22, 163, 74, 0.72)',
            machineBarBorder: isDarkTheme ? 'rgba(134, 239, 172, 1)' : 'rgba(21, 128, 61, 1)',
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
            duraklamaDescriptionElement.textContent = 'Secili makinenin kayitli duraklama sureleri gosteriliyor. Ayni cubuga tekrar tiklayarak filtreyi kaldirabilirsiniz.';
            return;
        }

        duraklamaTitleElement.textContent = 'Duraklama Nedenleri';
        duraklamaDescriptionElement.textContent = 'Makine bazli filtrelemek icin soldaki grafikten bir makineye tiklayin.';
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

        if (machineProductionChart) {
            const machineStyles = buildMachineBarStyles(machineLabels, palette);
            machineProductionChart.data.datasets[0].backgroundColor = machineStyles.backgroundColor;
            machineProductionChart.data.datasets[0].borderColor = machineStyles.borderColor;
            machineProductionChart.data.datasets[0].borderWidth = machineStyles.borderWidth;
            machineProductionChart.update();
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
        const machineValues = data.MakineUretimData || [];
        const machineDuraklamaDataset = getMachineDuraklamaDataset(selectedMachine);
        const machineStyles = buildMachineBarStyles(machineLabels, palette);

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

        machineProductionChart = createChart('makineUretimGrafigi', {
            type: 'bar',
            data: {
                labels: machineLabels,
                datasets: [
                    {
                        label: 'Uretim Adedi',
                        data: machineValues,
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
                        grid: {
                            color: palette.gridColor
                        }
                    }
                }
            }
        }, hasAnyData(machineValues), 'Makine bazli uretim verisi bulunamadi.');

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
                cutout: '58%',
                animation: {
                    duration: 350,
                    easing: 'easeOutCubic'
                },
                plugins: {
                    legend: {
                        position: 'top'
                    }
                }
            }
        }, hasAnyData(machineDuraklamaDataset.values), selectedMachine
            ? selectedMachine + ' icin duraklama verisi bulunamadi.'
            : 'Duraklama verisi bulunamadi.');

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
