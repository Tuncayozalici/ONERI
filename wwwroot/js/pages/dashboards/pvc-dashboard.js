document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('pvc-dashboard-data').textContent);
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
                        const filterInput = document.getElementById('tarihFiltre');
    const dateInput = document.getElementById('raporTarihi');
    const startDateInput = document.getElementById('baslangicTarihi');
    const endDateInput = document.getElementById('bitisTarihi');
    const monthInput = document.getElementById('ay');
    const yearInput = document.getElementById('yil');
    const clearButton = document.getElementById('clearFilter');

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

    function clearHiddenFilters() {
        dateInput.value = '';
        startDateInput.value = '';
        endDateInput.value = '';
        monthInput.value = '';
        yearInput.value = '';
    }

    function syncHiddenFiltersFromText() {
        clearHiddenFilters();

        const raw = String(filterInput.value || '').trim();
        if (!raw) {
            return;
        }

        const parsedRange = parseRangeValue(raw);
        if (parsedRange) {
            startDateInput.value = parsedRange.baslangic;
            endDateInput.value = parsedRange.bitis;
            filterInput.value = `${isoToDisplay(parsedRange.baslangic)} - ${isoToDisplay(parsedRange.bitis)}`;
            return;
        }

        const parsedMonth = parseMonthValue(raw);
        if (parsedMonth) {
            monthInput.value = String(parsedMonth.ay);
            yearInput.value = String(parsedMonth.yil);
            filterInput.value = `${pad2(parsedMonth.ay)}.${parsedMonth.yil}`;
            return;
        }

        const parsedDate = parseDateToIso(raw);
        if (parsedDate) {
            dateInput.value = parsedDate;
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
            filterForm.addEventListener('submit', function () {
                syncHiddenFiltersFromText();
            });
        }

        clearButton.addEventListener('click', function () {
            filterInput.value = '';
            clearHiddenFilters();
            window.location.href = `${window.location.pathname}?clear=1`;
        });
    }

                const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
                const horizontalPercentLabelPlugin = window.OneriChartHelpers?.createHorizontalPercentLabelPlugin?.({
                    textColor: isDarkTheme ? '#cbd5e1' : '#1f2937',
                    insideTextColor: '#ffffff',
                    font: '600 12px Poppins, sans-serif'
                });
    
                const uretimTrendCtx = document.getElementById('uretimTrendGrafigi').getContext('2d');
                new Chart(uretimTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.UretimTrendLabels,
                        datasets: [{
                            label: 'Metraj',
                            data: data.UretimTrendData,
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            backgroundColor: 'rgba(54, 162, 235, 0.2)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                const makineUretimEl = document.getElementById('makineUretimGrafigi');
                if (makineUretimEl) {
                    const makineUretimCtx = makineUretimEl.getContext('2d');
                    new Chart(makineUretimCtx, {
                        type: 'bar',
                        data: {
                            labels: data.MakineLabels,
                            datasets: [{
                                label: 'Metraj',
                                data: data.MakineUretimData,
                                backgroundColor: 'rgba(75, 192, 192, 0.7)'
                            }]
                        },
                        options: { responsive: true }
                    });
                }
    
                const makineParcaEl = document.getElementById('makineParcaGrafigi');
                if (makineParcaEl) {
                    const makineParcaCtx = makineParcaEl.getContext('2d');
                    const parcaMakineItems = (data.MakineLabels || []).map((label, index) => ({
                        label: label,
                        value: (data.MakineParcaData || [])[index] ?? 0
                    })).filter(item => !String(item.label || "").toLocaleUpperCase("tr-TR").includes("TURAN"));

                    new Chart(makineParcaCtx, {
                        type: 'bar',
                        data: {
                            labels: parcaMakineItems.map(x => x.label),
                            datasets: [{
                                label: 'Parça',
                                data: parcaMakineItems.map(x => x.value),
                                backgroundColor: 'rgba(54, 162, 235, 0.7)'
                            }]
                        },
                        options: { responsive: true }
                    });
                }
    
                const fiiliKayipCtx = document.getElementById('fiiliKayipGrafigi').getContext('2d');
                new Chart(fiiliKayipCtx, {
                    type: 'line',
                    data: {
                        labels: data.UretimTrendLabels,
                        datasets: [{
                            label: 'Performans (%)',
                            data: data.UretimOraniTrendData,
                            borderColor: 'rgba(153, 102, 255, 0.9)',
                            backgroundColor: 'rgba(153, 102, 255, 0.2)',
                            tension: 0.2,
                            fill: true
                        }, {
                            label: 'Kayıp Süre (%)',
                            data: data.KayipSureData,
                            borderColor: 'rgba(255, 159, 64, 0.9)',
                            backgroundColor: 'rgba(255, 159, 64, 0.15)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });

                const oeeTrendCtx = document.getElementById('oeeTrendGrafigi').getContext('2d');
                new Chart(oeeTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.UretimTrendLabels,
                        datasets: [{
                            label: 'OEE (%)',
                            data: data.OeeTrendData,
                            borderColor: 'rgba(16, 185, 129, 0.95)',
                            backgroundColor: 'rgba(16, 185, 129, 0.15)',
                            tension: 0.25,
                            fill: true
                        }]
                    },
                    options: {
                        responsive: true,
                        scales: {
                            y: {
                                suggestedMin: 0,
                                suggestedMax: 100
                            }
                        }
                    }
                });

                const makineOeeTrendEl = document.getElementById('makineOeeTrendGrafigi');
                if (makineOeeTrendEl) {
                    const makineOeeTrendCtx = makineOeeTrendEl.getContext('2d');
                    const makineOeeLabels = data.MakineOeeSerieLabels || [];
                    const makineOeeSeries = data.MakineOeeTrendSeries || [];
                    const makineOeeItems = makineOeeLabels.map((label, index) => {
                        const serie = Array.isArray(makineOeeSeries[index]) ? makineOeeSeries[index] : [];
                        const validValues = serie.filter(v => typeof v === 'number' && v > 0);
                        const average = validValues.length > 0
                            ? validValues.reduce((sum, v) => sum + v, 0) / validValues.length
                            : 0;
                        return { label, value: average };
                    }).sort((a, b) => b.value - a.value);

                    new Chart(makineOeeTrendCtx, {
                        type: 'bar',
                        data: {
                            labels: makineOeeItems.map(x => x.label),
                            datasets: [{
                                label: 'OEE (%)',
                                data: makineOeeItems.map(x => x.value),
                                backgroundColor: 'rgba(16, 185, 129, 0.65)',
                                borderColor: 'rgba(16, 185, 129, 0.95)',
                                borderWidth: 1
                            }]
                        },
                        options: {
                            responsive: true,
                            indexAxis: 'y',
                            scales: {
                                x: {
                                    beginAtZero: true,
                                    suggestedMin: 0,
                                    suggestedMax: 100
                                }
                            }
                        },
                        plugins: horizontalPercentLabelPlugin ? [horizontalPercentLabelPlugin] : []
                    });
                }
    
                const duraklamaCtx = document.getElementById('duraklamaNedenGrafigi').getContext('2d');
                if ((data.DuraklamaNedenLabels || []).length > 0) {
                    new Chart(duraklamaCtx, {
                        type: 'doughnut',
                        data: {
                            labels: data.DuraklamaNedenLabels,
                            datasets: [{
                                label: 'Duraklama (dk)',
                                data: data.DuraklamaNedenData,
                                backgroundColor: [
                                    'rgba(255, 99, 132, 0.8)',
                                    'rgba(54, 162, 235, 0.8)',
                                    'rgba(255, 206, 86, 0.8)',
                                    'rgba(75, 192, 192, 0.8)',
                                    'rgba(153, 102, 255, 0.8)',
                                    'rgba(255, 159, 64, 0.8)'
                                ]
                            }]
                        },
                        options: { responsive: true, maintainAspectRatio: false }
                    });
                }
});
