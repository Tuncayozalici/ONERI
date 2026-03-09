document.addEventListener('DOMContentLoaded', function () {
    const payloadElement = document.getElementById('tezgah-dashboard-data');
    if (!payloadElement) {
        return;
    }

    const payload = JSON.parse(payloadElement.textContent);
    const data = normalizePayloadKeys(payload.model || {});

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

    function pad2(value) {
        return String(value).padStart(2, '0');
    }

    function isValidIsoDate(isoDate) {
        const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate);
        if (!match) {
            return false;
        }

        const year = Number(match[1]);
        const month = Number(match[2]);
        const day = Number(match[3]);
        const candidate = new Date(Date.UTC(year, month - 1, day));
        return candidate.getUTCFullYear() === year
            && candidate.getUTCMonth() + 1 === month
            && candidate.getUTCDate() === day;
    }

    function parseDateToIso(value) {
        const raw = String(value || '').trim();
        if (!raw) {
            return null;
        }

        if (isValidIsoDate(raw)) {
            return raw;
        }

        const trMatch = /^(\d{1,2})[./-](\d{1,2})[./-](\d{4})$/.exec(raw);
        if (!trMatch) {
            return null;
        }

        const day = pad2(Number(trMatch[1]));
        const month = pad2(Number(trMatch[2]));
        const year = trMatch[3];
        const iso = `${year}-${month}-${day}`;
        return isValidIsoDate(iso) ? iso : null;
    }

    function parseMonthValue(value) {
        const raw = String(value || '').trim();
        if (!raw) {
            return null;
        }

        let match = /^(\d{1,2})[./-](\d{4})$/.exec(raw);
        if (match) {
            const month = Number(match[1]);
            const year = Number(match[2]);
            if (month >= 1 && month <= 12) {
                return { ay: month, yil: year };
            }
        }

        match = /^(\d{4})[./-](\d{1,2})$/.exec(raw);
        if (match) {
            const year = Number(match[1]);
            const month = Number(match[2]);
            if (month >= 1 && month <= 12) {
                return { ay: month, yil: year };
            }
        }

        return null;
    }

    function parseRangeValue(value) {
        const raw = String(value || '').trim();
        const match = /^(.+)\s-\s(.+)$/.exec(raw);
        if (!match) {
            return null;
        }

        const startIso = parseDateToIso(match[1]);
        const endIso = parseDateToIso(match[2]);
        if (!startIso || !endIso) {
            return null;
        }

        return { baslangic: startIso, bitis: endIso };
    }

    function isoToDisplay(isoDate) {
        if (!isValidIsoDate(isoDate)) {
            return '';
        }

        const [year, month, day] = isoDate.split('-');
        return `${day}.${month}.${year}`;
    }

    function getModelDateIso() {
        const raw = data.RaporTarihi;
        if (!raw) {
            return '';
        }

        if (typeof raw === 'string') {
            const match = raw.match(/^(\d{4})-(\d{2})-(\d{2})/);
            if (match) {
                return `${match[1]}-${match[2]}-${match[3]}`;
            }
        }

        const parsed = new Date(raw);
        if (Number.isNaN(parsed.getTime())) {
            return '';
        }

        return `${parsed.getFullYear()}-${pad2(parsed.getMonth() + 1)}-${pad2(parsed.getDate())}`;
    }

    const filterInput = document.getElementById('tarihFiltre');
    const dateInput = document.getElementById('raporTarihi');
    const startDateInput = document.getElementById('baslangicTarihi');
    const endDateInput = document.getElementById('bitisTarihi');
    const monthInput = document.getElementById('ay');
    const yearInput = document.getElementById('yil');
    const clearButton = document.getElementById('clearFilter') || document.querySelector('.js-unified-filter-clear');

    function clearHiddenFilters() {
        if (dateInput) {
            dateInput.value = '';
        }
        if (startDateInput) {
            startDateInput.value = '';
        }
        if (endDateInput) {
            endDateInput.value = '';
        }
        if (monthInput) {
            monthInput.value = '';
        }
        if (yearInput) {
            yearInput.value = '';
        }
    }

    function syncHiddenFiltersFromText() {
        if (!filterInput) {
            return;
        }

        clearHiddenFilters();

        const raw = String(filterInput.value || '').trim();
        if (!raw) {
            return;
        }

        const parsedRange = parseRangeValue(raw);
        if (parsedRange) {
            if (startDateInput) {
                startDateInput.value = parsedRange.baslangic;
            }
            if (endDateInput) {
                endDateInput.value = parsedRange.bitis;
            }
            filterInput.value = `${isoToDisplay(parsedRange.baslangic)} - ${isoToDisplay(parsedRange.bitis)}`;
            return;
        }

        const parsedMonth = parseMonthValue(raw);
        if (parsedMonth) {
            if (monthInput) {
                monthInput.value = String(parsedMonth.ay);
            }
            if (yearInput) {
                yearInput.value = String(parsedMonth.yil);
            }
            filterInput.value = `${pad2(parsedMonth.ay)}.${parsedMonth.yil}`;
            return;
        }

        const parsedDate = parseDateToIso(raw);
        if (parsedDate) {
            if (dateInput) {
                dateInput.value = parsedDate;
            }
            filterInput.value = isoToDisplay(parsedDate);
        }
    }

    if (filterInput) {
        const urlParams = new URLSearchParams(window.location.search);
        const raporTarihi = urlParams.get('raporTarihi');
        const baslangicTarihi = urlParams.get('baslangicTarihi');
        const bitisTarihi = urlParams.get('bitisTarihi');
        const ay = urlParams.get('ay');
        const yil = urlParams.get('yil');
        const clear = urlParams.get('clear');

        if (clear === '1') {
            filterInput.value = '';
        } else if (raporTarihi) {
            filterInput.value = isoToDisplay(raporTarihi);
        } else if (baslangicTarihi && bitisTarihi) {
            filterInput.value = `${isoToDisplay(baslangicTarihi)} - ${isoToDisplay(bitisTarihi)}`;
        } else if (ay && yil) {
            filterInput.value = `${pad2(Number(ay))}.${yil}`;
        } else {
            const modelDateIso = getModelDateIso();
            filterInput.value = modelDateIso ? isoToDisplay(modelDateIso) : '';
        }

        syncHiddenFiltersFromText();
        filterInput.addEventListener('blur', syncHiddenFiltersFromText);

        const filterForm = filterInput.closest('form');
        if (filterForm) {
            filterForm.addEventListener('submit', syncHiddenFiltersFromText);
        }

        if (clearButton) {
            clearButton.addEventListener('click', function () {
                filterInput.value = '';
                clearHiddenFilters();
                window.location.href = `${window.location.pathname}?clear=1`;
            });
        }
    }

    const chartCanvasIds = [
        'uretimSureGrafigi',
        'oeeKayipGrafigi',
        'urunGrafigi',
        'verimlilikGrafigi',
        'kayipNedenGrafigi',
        'calismaKosuluGrafigi'
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

    function getThemeName() {
        return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    function getThemePalette() {
        const isDarkTheme = getThemeName() === 'dark';
        return {
            textColor: isDarkTheme ? '#9aa8bd' : '#475569',
            gridColor: isDarkTheme ? 'rgba(148, 163, 184, 0.14)' : 'rgba(148, 163, 184, 0.32)',
            productionBar: isDarkTheme ? 'rgba(34, 197, 94, 0.72)' : 'rgba(22, 163, 74, 0.66)',
            durationLine: isDarkTheme ? '#2dd4bf' : '#0f766e',
            durationFill: isDarkTheme ? 'rgba(45, 212, 191, 0.16)' : 'rgba(15, 118, 110, 0.12)',
            oeeLine: isDarkTheme ? '#fbbf24' : '#d97706',
            oeeFill: isDarkTheme ? 'rgba(251, 191, 36, 0.14)' : 'rgba(217, 119, 6, 0.12)',
            kayipBar: isDarkTheme ? 'rgba(251, 113, 133, 0.68)' : 'rgba(225, 29, 72, 0.62)',
            verimBar: isDarkTheme ? 'rgba(129, 140, 248, 0.78)' : 'rgba(79, 70, 229, 0.72)',
            kosulBar: isDarkTheme ? 'rgba(148, 163, 184, 0.7)' : 'rgba(71, 85, 105, 0.65)',
            kosulLine: isDarkTheme ? '#fbbf24' : '#ca8a04',
            kosulFill: isDarkTheme ? 'rgba(251, 191, 36, 0.14)' : 'rgba(202, 138, 4, 0.12)'
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

        parent.innerHTML = `<div class="tezgah-empty-state">${message}</div>`;
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

        createChart('uretimSureGrafigi', {
            type: 'bar',
            data: {
                labels: data.TrendLabels,
                datasets: [
                    {
                        type: 'bar',
                        label: 'Parça adedi',
                        data: data.GunlukParcaTrendData,
                        backgroundColor: palette.productionBar,
                        borderRadius: 10,
                        yAxisID: 'y'
                    },
                    {
                        type: 'line',
                        label: 'Süre (dk)',
                        data: data.GunlukSureTrendData,
                        borderColor: palette.durationLine,
                        backgroundColor: palette.durationFill,
                        tension: 0.28,
                        fill: true,
                        yAxisID: 'y1'
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
                    },
                    y1: {
                        beginAtZero: true,
                        position: 'right',
                        grid: {
                            drawOnChartArea: false
                        },
                        ticks: {
                            precision: 0
                        }
                    }
                }
            }
        }, hasAnyData(data.GunlukParcaTrendData) || hasAnyData(data.GunlukSureTrendData), 'Seçilen dönem için günlük üretim veya süre verisi bulunamadı.');

        createChart('oeeKayipGrafigi', {
            type: 'bar',
            data: {
                labels: data.TrendLabels,
                datasets: [
                    {
                        type: 'line',
                        label: 'OEE (%)',
                        data: data.OeeTrendData,
                        borderColor: palette.oeeLine,
                        backgroundColor: palette.oeeFill,
                        tension: 0.28,
                        fill: true,
                        yAxisID: 'y'
                    },
                    {
                        type: 'bar',
                        label: 'Kayıp süre (dk)',
                        data: data.KayipSureTrendData,
                        backgroundColor: palette.kayipBar,
                        borderRadius: 10,
                        yAxisID: 'y1'
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
                    },
                    y1: {
                        beginAtZero: true,
                        position: 'right',
                        grid: {
                            drawOnChartArea: false
                        },
                        ticks: {
                            precision: 0
                        }
                    }
                }
            }
        }, hasAnyData(data.OeeTrendData) || hasAnyData(data.KayipSureTrendData), 'OEE veya kayıp süre trendi oluşturmak için yeterli veri bulunamadı.');

        createChart('urunGrafigi', {
            type: 'bar',
            data: {
                labels: data.UrunLabels,
                datasets: [
                    {
                        label: 'Toplam parça',
                        data: data.UrunParcaData,
                        backgroundColor: [
                            '#166534',
                            '#0f766e',
                            '#0284c7',
                            '#7c3aed',
                            '#d97706',
                            '#dc2626',
                            '#475569',
                            '#ca8a04'
                        ],
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
                        },
                        ticks: {
                            precision: 0
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
        }, hasAnyData(data.UrunParcaData), 'Ürün bazlı üretim verisi bulunamadı.');

        createChart('verimlilikGrafigi', {
            type: 'bar',
            data: {
                labels: data.UrunLabels,
                datasets: [
                    {
                        label: 'Parça / kişi-saat',
                        data: data.UrunSaatlikVerimData,
                        backgroundColor: palette.verimBar,
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
        }, hasAnyData(data.UrunSaatlikVerimData), 'Saatlik verim grafiği için net süre verisi bulunamadı.');

        createChart('kayipNedenGrafigi', {
            type: 'bar',
            data: {
                labels: data.KayipNedenLabels,
                datasets: [
                    {
                        label: 'Kayıp süre (dk)',
                        data: data.KayipNedenData,
                        backgroundColor: palette.kayipBar,
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
                        },
                        ticks: {
                            precision: 0
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
        }, hasAnyData(data.KayipNedenData), 'Seçili dönemde kayıtlı kayıp süre nedeni bulunamadı.');

        createChart('calismaKosuluGrafigi', {
            type: 'bar',
            data: {
                labels: data.CalismaKosuluLabels,
                datasets: [
                    {
                        type: 'bar',
                        label: 'Süre (dk)',
                        data: data.CalismaKosuluSureData,
                        backgroundColor: palette.kosulBar,
                        borderRadius: 10,
                        yAxisID: 'y'
                    },
                    {
                        type: 'line',
                        label: 'Parça adedi',
                        data: data.CalismaKosuluParcaData,
                        borderColor: palette.kosulLine,
                        backgroundColor: palette.kosulFill,
                        tension: 0.28,
                        fill: true,
                        yAxisID: 'y1'
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
                    },
                    y1: {
                        beginAtZero: true,
                        position: 'right',
                        grid: {
                            drawOnChartArea: false
                        }
                    }
                }
            }
        }, hasAnyData(data.CalismaKosuluSureData) || hasAnyData(data.CalismaKosuluParcaData), 'Çalışma koşulu kırılımı için veri bulunamadı.');
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
