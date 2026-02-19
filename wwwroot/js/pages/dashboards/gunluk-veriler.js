document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('gunluk-veriler-data').textContent);
    const data = normalizePayloadKeys(payload.model || {});
    const defaultYear = payload.defaultYear;

    function normalizePayloadKeys(value) {
        if (Array.isArray(value)) {
            return value.map(normalizePayloadKeys);
        }

        if (value !== null && typeof value === "object") {
            const normalized = {};
            for (const [key, nested] of Object.entries(value)) {
                const normalizedKey = key.length > 0 ? key[0].toUpperCase() + key.slice(1) : key;
                normalized[normalizedKey] = normalizePayloadKeys(nested);
            }
            return normalized;
        }

        return value;
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

                    const year = parsed.getFullYear();
                    const month = String(parsed.getMonth() + 1).padStart(2, '0');
                    const day = String(parsed.getDate()).padStart(2, '0');
                    return `${year}-${month}-${day}`;
                }
                const summaryCard = document.getElementById('summaryCard');
                const summaryToggle = document.getElementById('summaryUltraToggle');
                const sectionNavGrid = document.getElementById('sectionNavGrid');
                if (summaryCard && summaryToggle) {
                    const saved = localStorage.getItem('summary_ultra');
                    const enabled = saved === '1';
                    summaryCard.classList.toggle('summary-ultra', enabled);
                    sectionNavGrid?.classList.toggle('section-nav-grid-ultra', enabled);
                    summaryToggle.checked = enabled;
                    summaryToggle.addEventListener('change', function() {
                        summaryCard.classList.toggle('summary-ultra', summaryToggle.checked);
                        sectionNavGrid?.classList.toggle('section-nav-grid-ultra', summaryToggle.checked);
                        localStorage.setItem('summary_ultra', summaryToggle.checked ? '1' : '0');
                    });
                }
    
                                const summaryFilterInput = document.getElementById('summaryTarihFiltre');
                const summaryDate = document.getElementById('summaryDate');
                const summaryStartDate = document.getElementById('summaryStartDate');
                const summaryEndDate = document.getElementById('summaryEndDate');
                const summaryMonth = document.getElementById('summaryMonth');
                const summaryYear = document.getElementById('summaryYear');
                const summaryClear = document.getElementById('summaryClear');
                const summaryForm = document.getElementById('summaryFilterForm');

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
                    return candidate.getUTCFullYear() == year
                        && candidate.getUTCMonth() + 1 == month
                        && candidate.getUTCDate() == day;
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

                function clearSummaryHiddenFilters() {
                    summaryDate.value = '';
                    summaryStartDate.value = '';
                    summaryEndDate.value = '';
                    summaryMonth.value = '';
                    summaryYear.value = '';
                }

                function syncSummaryHiddenFilters() {
                    clearSummaryHiddenFilters();

                    const raw = String(summaryFilterInput.value || '').trim();
                    if (!raw) {
                        return;
                    }

                    const parsedRange = parseRangeValue(raw);
                    if (parsedRange) {
                        summaryStartDate.value = parsedRange.baslangic;
                        summaryEndDate.value = parsedRange.bitis;
                        summaryFilterInput.value = `${isoToDisplay(parsedRange.baslangic)} - ${isoToDisplay(parsedRange.bitis)}`;
                        return;
                    }

                    const parsedMonth = parseMonthValue(raw);
                    if (parsedMonth) {
                        summaryMonth.value = String(parsedMonth.ay);
                        summaryYear.value = String(parsedMonth.yil);
                        summaryFilterInput.value = `${pad2(parsedMonth.ay)}.${parsedMonth.yil}`;
                        return;
                    }

                    const parsedDate = parseDateToIso(raw);
                    if (parsedDate) {
                        summaryDate.value = parsedDate;
                        summaryFilterInput.value = isoToDisplay(parsedDate);
                    }
                }

                if (summaryFilterInput) {
                    const urlParams = new URLSearchParams(window.location.search);
                    const raporTarihi = urlParams.get('raporTarihi');
                    const baslangicTarihi = urlParams.get('baslangicTarihi');
                    const bitisTarihi = urlParams.get('bitisTarihi');
                    const ay = urlParams.get('ay');
                    const yil = urlParams.get('yil');

                    if (raporTarihi) {
                        summaryFilterInput.value = isoToDisplay(raporTarihi);
                    } else if (baslangicTarihi && bitisTarihi) {
                        summaryFilterInput.value = `${isoToDisplay(baslangicTarihi)} - ${isoToDisplay(bitisTarihi)}`;
                    } else if (ay && yil) {
                        summaryFilterInput.value = `${pad2(Number(ay))}.${yil}`;
                    } else {
                        const modelDateIso = getModelDateIso();
                        summaryFilterInput.value = modelDateIso ? isoToDisplay(modelDateIso) : '';
                    }

                    syncSummaryHiddenFilters();

                    summaryFilterInput.addEventListener('blur', syncSummaryHiddenFilters);

                    if (summaryForm) {
                        summaryForm.addEventListener('submit', function() {
                            syncSummaryHiddenFilters();
                        });
                    }

                    summaryClear.addEventListener('click', function() {
                        summaryFilterInput.value = '';
                        clearSummaryHiddenFilters();
                        window.location.href = '/Home/GunlukVeriler';
                    });
                }
    
                new Chart(document.getElementById('genelHataTrend').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Hatalı Adet',
                            data: data.HataTrendData,
                            borderColor: 'rgba(255, 99, 132, 0.9)',
                            backgroundColor: 'rgba(255, 99, 132, 0.15)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false
                    }
                });
    
                new Chart(document.getElementById('hataNedenGenelGrafigi').getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: data.HataNedenLabels,
                        datasets: [{
                            data: data.HataNedenData,
                            backgroundColor: ['#ff6384', '#36a2eb', '#ffcd56', '#4bc0c0', '#9966ff', '#ff9f40']
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
    
                new Chart(document.getElementById('duraklamaTrendGrafigi').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Duraklama (dk)',
                            data: data.DuraklamaTrendData,
                            borderColor: 'rgba(153, 102, 255, 0.9)',
                            backgroundColor: 'rgba(153, 102, 255, 0.12)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false
                    }
                });

                const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
                const axisTickColor = isDarkTheme ? '#cbd5e1' : '#1f2937';
                const axisGridColor = isDarkTheme ? 'rgba(148, 163, 184, 0.16)' : 'rgba(148, 163, 184, 0.45)';
                const oeeAccentColor = isDarkTheme ? '#34d399' : '#16a34a';
                const horizontalPercentLabelPlugin = window.OneriChartHelpers?.createHorizontalPercentLabelPlugin?.({
                    textColor: axisTickColor,
                    insideTextColor: '#ffffff',
                    font: '600 12px Poppins, sans-serif'
                });
                const clampPercent = (value) => {
                    const num = Number(value) || 0;
                    return Math.max(0, Math.min(100, num));
                };
                const horizontalValueLabelPlugin = {
                    id: 'horizontalValueLabels',
                    afterDatasetsDraw(chart) {
                        if (!chart || !chart.options || chart.options.indexAxis !== 'y') {
                            return;
                        }

                        const { ctx, chartArea } = chart;
                        if (!ctx || !chartArea) {
                            return;
                        }

                        ctx.save();
                        ctx.font = '600 12px Poppins, sans-serif';
                        ctx.textBaseline = 'middle';

                        chart.data.datasets.forEach((dataset, datasetIndex) => {
                            const meta = chart.getDatasetMeta(datasetIndex);
                            if (!meta || meta.hidden) {
                                return;
                            }

                            meta.data.forEach((bar, dataIndex) => {
                                const value = Number(dataset.data[dataIndex]);
                                if (!Number.isFinite(value) || value <= 0) {
                                    return;
                                }

                                const label = value.toLocaleString('tr-TR', {
                                    maximumFractionDigits: 0
                                });
                                const barStartX = Math.min(bar.base, bar.x);
                                const barEndX = Math.max(bar.base, bar.x);
                                const barWidth = Math.max(0, barEndX - barStartX);
                                if (barWidth <= 0) {
                                    return;
                                }
                                const y = bar.y;
                                const labelWidth = ctx.measureText(label).width;
                                const fitsInside = barWidth >= (labelWidth + 14);

                                if (fitsInside) {
                                    const x = barEndX - 8;
                                    ctx.save();
                                    ctx.beginPath();
                                    ctx.rect(
                                        barStartX + 1,
                                        y - (Math.max(0, bar.height || 0) / 2) + 1,
                                        Math.max(0, barWidth - 2),
                                        Math.max(0, Math.max(0, bar.height || 0) - 2)
                                    );
                                    ctx.clip();

                                    ctx.textAlign = 'right';
                                    ctx.fillStyle = '#ffffff';
                                    ctx.fillText(label, x, y);
                                    ctx.restore();
                                } else {
                                    const preferredX = barEndX + 8;
                                    const clampedX = Math.min(
                                        preferredX,
                                        chartArea.right - labelWidth - 2
                                    );
                                    ctx.textAlign = 'left';
                                    ctx.fillStyle = axisTickColor;
                                    ctx.fillText(label, clampedX, y);
                                }
                            });
                        });

                        ctx.restore();
                    }
                };

                const genelBilesenCanvas = document.getElementById('genelBilesenGrafigi');
                if (genelBilesenCanvas) {
                    new Chart(genelBilesenCanvas.getContext('2d'), {
                        type: 'bar',
                        data: {
                            labels: ['Performans', 'Kullanılabilirlik', 'Kalite'],
                            datasets: [{
                                label: 'Ortalama (%)',
                                data: [
                                    clampPercent(data.OrtalamaPerformans),
                                    clampPercent(data.OrtalamaKullanilabilirlik),
                                    clampPercent(data.OrtalamaKalite)
                                ],
                                backgroundColor: [
                                    'rgba(59, 130, 246, 0.82)',
                                    'rgba(16, 185, 129, 0.82)',
                                    'rgba(168, 85, 247, 0.82)'
                                ],
                                borderColor: [
                                    'rgba(59, 130, 246, 1)',
                                    'rgba(16, 185, 129, 1)',
                                    'rgba(168, 85, 247, 1)'
                                ],
                                borderWidth: 1,
                                borderRadius: 8,
                                barThickness: 20
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            indexAxis: 'y',
                            scales: {
                                x: {
                                    min: 0,
                                    max: 100,
                                    ticks: {
                                        color: axisTickColor,
                                        callback: (value) => `${value}%`
                                    },
                                    grid: { color: axisGridColor }
                                },
                                y: {
                                    ticks: { color: axisTickColor },
                                    grid: { color: axisGridColor }
                                }
                            },
                            plugins: {
                                legend: { labels: { color: axisTickColor } }
                            }
                        },
                        plugins: horizontalPercentLabelPlugin ? [horizontalPercentLabelPlugin] : []
                    });
                }

                const genelOeeGaugeCanvas = document.getElementById('genelOeeGaugeGrafigi');
                if (genelOeeGaugeCanvas) {
                    const oeeValue = clampPercent(data.OrtalamaOee);
                    const centerTextPlugin = {
                        id: 'centerTextPlugin',
                        afterDraw(chart) {
                            const { ctx, chartArea } = chart;
                            if (!chartArea) return;
                            const centerX = chartArea.left + (chartArea.right - chartArea.left) / 2;
                            const centerY = chartArea.top + (chartArea.bottom - chartArea.top) * 0.82;
                            ctx.save();
                            ctx.textAlign = 'center';
                            ctx.fillStyle = oeeAccentColor;
                            ctx.font = '700 30px Poppins, sans-serif';
                            ctx.fillText(`${oeeValue.toFixed(2)} %`, centerX, centerY);
                            ctx.fillStyle = isDarkTheme ? '#cbd5e1' : '#334155';
                            ctx.font = '500 13px Poppins, sans-serif';
                            ctx.fillText('Genel OEE', centerX, centerY + 24);
                            ctx.restore();
                        }
                    };

                    new Chart(genelOeeGaugeCanvas.getContext('2d'), {
                        type: 'doughnut',
                        data: {
                            labels: ['OEE', 'Kalan'],
                            datasets: [{
                                data: [oeeValue, 100 - oeeValue],
                                backgroundColor: [
                                    isDarkTheme ? 'rgba(52, 211, 153, 0.9)' : 'rgba(22, 163, 74, 0.9)',
                                    isDarkTheme ? 'rgba(71, 85, 105, 0.45)' : 'rgba(148, 163, 184, 0.45)'
                                ],
                                borderWidth: 0,
                                hoverOffset: 0
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            rotation: -90,
                            circumference: 180,
                            cutout: '74%',
                            plugins: {
                                legend: { display: false },
                                tooltip: {
                                    callbacks: {
                                        label: (context) => `${context.label}: ${context.parsed.toFixed(2)}%`
                                    }
                                }
                            }
                        },
                        plugins: [centerTextPlugin]
                    });
                }

                const makineOeeCanvas = document.getElementById('makineOeeGrafigi');
                if (makineOeeCanvas) {
                    const machineLabels = Array.isArray(data.MakineOeeLabels) ? data.MakineOeeLabels : [];
                    const machineValues = (Array.isArray(data.MakineOeeData) ? data.MakineOeeData : []).map(clampPercent);
                    const hasMachineData = machineLabels.length > 0 && machineValues.some(v => v > 0);
                    const machinePalette = [
                        { bg: 'rgba(59, 130, 246, 0.78)', border: 'rgba(59, 130, 246, 1)' },
                        { bg: 'rgba(16, 185, 129, 0.78)', border: 'rgba(16, 185, 129, 1)' },
                        { bg: 'rgba(249, 115, 22, 0.78)', border: 'rgba(249, 115, 22, 1)' },
                        { bg: 'rgba(168, 85, 247, 0.78)', border: 'rgba(168, 85, 247, 1)' },
                        { bg: 'rgba(236, 72, 153, 0.78)', border: 'rgba(236, 72, 153, 1)' },
                        { bg: 'rgba(234, 179, 8, 0.78)', border: 'rgba(234, 179, 8, 1)' },
                        { bg: 'rgba(20, 184, 166, 0.78)', border: 'rgba(20, 184, 166, 1)' },
                        { bg: 'rgba(239, 68, 68, 0.78)', border: 'rgba(239, 68, 68, 1)' }
                    ];
                    const machineBarColors = hasMachineData
                        ? machineValues.map((_, index) => machinePalette[index % machinePalette.length].bg)
                        : ['rgba(148, 163, 184, 0.35)'];
                    const machineBorderColors = hasMachineData
                        ? machineValues.map((_, index) => machinePalette[index % machinePalette.length].border)
                        : ['rgba(100, 116, 139, 0.7)'];

                    new Chart(makineOeeCanvas.getContext('2d'), {
                        type: 'bar',
                        data: {
                            labels: hasMachineData ? machineLabels : ['Veri Yok'],
                            datasets: [{
                                label: 'Ortalama OEE (%)',
                                data: hasMachineData ? machineValues : [0],
                                backgroundColor: machineBarColors,
                                borderColor: machineBorderColors,
                                borderWidth: 1,
                                borderRadius: 8,
                                barThickness: 22
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            indexAxis: 'y',
                            scales: {
                                x: {
                                    min: 0,
                                    max: 100,
                                    ticks: {
                                        color: axisTickColor,
                                        callback: (value) => `${value}%`
                                    },
                                    grid: { color: axisGridColor }
                                },
                                y: {
                                    ticks: { color: axisTickColor },
                                    grid: { color: axisGridColor }
                                }
                            },
                            plugins: {
                                legend: { display: false }
                            }
                        },
                        plugins: horizontalPercentLabelPlugin ? [horizontalPercentLabelPlugin] : []
                    });
                }

                const bolumOeeCanvas = document.getElementById('bolumOeeGrafigi');
                if (bolumOeeCanvas) {
                    const deptLabels = Array.isArray(data.BolumOeeLabels) ? data.BolumOeeLabels : [];
                    const deptValues = (Array.isArray(data.BolumOeeData) ? data.BolumOeeData : []).map(clampPercent);
                    const hasDeptData = deptLabels.length > 0 && deptValues.some(v => v > 0);

                    new Chart(bolumOeeCanvas.getContext('2d'), {
                        type: 'bar',
                        data: {
                            labels: hasDeptData ? deptLabels : ['Veri Yok'],
                            datasets: [{
                                label: 'Ortalama OEE (%)',
                                data: hasDeptData ? deptValues : [0],
                                backgroundColor: hasDeptData
                                    ? 'rgba(14, 165, 233, 0.76)'
                                    : 'rgba(148, 163, 184, 0.35)',
                                borderColor: hasDeptData
                                    ? 'rgba(14, 165, 233, 1)'
                                    : 'rgba(100, 116, 139, 0.7)',
                                borderWidth: 1,
                                borderRadius: 8,
                                barThickness: 22
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            indexAxis: 'y',
                            scales: {
                                x: {
                                    min: 0,
                                    max: 100,
                                    ticks: {
                                        color: axisTickColor,
                                        callback: (value) => `${value}%`
                                    },
                                    grid: { color: axisGridColor }
                                },
                                y: {
                                    ticks: { color: axisTickColor },
                                    grid: { color: axisGridColor }
                                }
                            },
                            plugins: {
                                legend: { labels: { color: axisTickColor } }
                            }
                        },
                        plugins: horizontalPercentLabelPlugin ? [horizontalPercentLabelPlugin] : []
                    });
                }

                const bolumHataCanvas = document.getElementById('bolumHataGrafigi');
                if (bolumHataCanvas) {
                    const deptHataLabels = Array.isArray(data.BolumHataLabels) ? data.BolumHataLabels : [];
                    const deptHataValues = (Array.isArray(data.BolumHataData) ? data.BolumHataData : []).map(v => Math.max(0, Number(v) || 0));
                    const hasDeptHataData = deptHataLabels.length > 0 && deptHataValues.some(v => v > 0);

                    new Chart(bolumHataCanvas.getContext('2d'), {
                        type: 'bar',
                        data: {
                            labels: hasDeptHataData ? deptHataLabels : ['Veri Yok'],
                            datasets: [{
                                label: 'Hatalı Adet',
                                data: hasDeptHataData ? deptHataValues : [0],
                                backgroundColor: hasDeptHataData
                                    ? 'rgba(244, 63, 94, 0.72)'
                                    : 'rgba(148, 163, 184, 0.35)',
                                borderColor: hasDeptHataData
                                    ? 'rgba(244, 63, 94, 1)'
                                    : 'rgba(100, 116, 139, 0.7)',
                                borderWidth: 1,
                                borderRadius: 8,
                                barThickness: 22
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            indexAxis: 'y',
                            scales: {
                                x: {
                                    beginAtZero: true,
                                    ticks: {
                                        color: axisTickColor,
                                        precision: 0
                                    },
                                    grid: { color: axisGridColor }
                                },
                                y: {
                                    ticks: { color: axisTickColor },
                                    grid: { color: axisGridColor }
                                }
                            },
                            plugins: {
                                legend: { display: false }
                            }
                        },
                        plugins: [horizontalValueLabelPlugin]
                    });
                }
});
