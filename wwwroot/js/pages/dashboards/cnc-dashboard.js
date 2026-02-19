document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('cnc-dashboard-data').textContent);
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

    const cncUretimCtx = document.getElementById('cncUretimTrendGrafigi').getContext('2d');
    new Chart(cncUretimCtx, {
        type: 'line',
        data: {
            labels: data.TrendLabels,
            datasets: [{
                label: 'Toplam Üretim',
                data: data.UretimTrendData,
                borderColor: 'rgba(59, 130, 246, 0.95)',
                backgroundColor: 'rgba(59, 130, 246, 0.15)',
                tension: 0.25,
                fill: true
            }]
        },
        options: { responsive: true }
    });

    const cncOeeCtx = document.getElementById('cncOeeTrendGrafigi').getContext('2d');
    new Chart(cncOeeCtx, {
        type: 'line',
        data: {
            labels: data.TrendLabels,
            datasets: [{
                label: 'Ortalama OEE (%)',
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

    const cncHataliTrendCanvas = document.getElementById('cncHataliTrendGrafigi');
    if (cncHataliTrendCanvas) {
        const cncHataliTrendCtx = cncHataliTrendCanvas.getContext('2d');
        new Chart(cncHataliTrendCtx, {
            type: 'line',
            data: {
                labels: data.TrendLabels,
                datasets: [{
                    label: 'Hatalı Parça',
                    data: data.HataliParcaTrendData,
                    borderColor: 'rgba(239, 68, 68, 0.95)',
                    backgroundColor: 'rgba(239, 68, 68, 0.16)',
                    tension: 0.25,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    const cncDuraklamaCanvas = document.getElementById('cncDuraklamaNedenGrafigi');
    if (cncDuraklamaCanvas && (data.DuraklamaNedenLabels || []).length > 0) {
        const cncDuraklamaCtx = cncDuraklamaCanvas.getContext('2d');
        new Chart(cncDuraklamaCtx, {
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
