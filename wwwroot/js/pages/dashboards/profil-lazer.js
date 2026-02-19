document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('profil-lazer-data').textContent);
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
    
                // Chart rendering code...
                // Pasta Grafik
                const pastaCtx = document.getElementById('pastaGrafigi').getContext('2d');
                if ((data.ProfilIsimleri || []).length > 0) {
                    new Chart(pastaCtx, {
                        type: 'doughnut',
                        data: {
                            labels: data.ProfilIsimleri,
                            datasets: [{
                                label: 'Üretim Adedi',
                                data: data.ProfilUretimAdetleri,
                                backgroundColor: [
                                    'rgba(255, 99, 132, 0.8)',
                                    'rgba(54, 162, 235, 0.8)',
                                    'rgba(255, 206, 86, 0.8)',
                                    'rgba(75, 192, 192, 0.8)',
                                    'rgba(153, 102, 255, 0.8)',
                                    'rgba(255, 159, 64, 0.8)'
                                ],
                                borderColor: 'rgba(255, 255, 255, 0.9)',
                                borderWidth: 2
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            plugins: {
                                legend: {
                                    position: 'top',
                                }
                            }
                        }
                    });
                }
    
                // Çizgi Grafik
                const cizgiCtx = document.getElementById('cizgiGrafigi').getContext('2d');
                if ((data.Son7GunTarihleri || []).length > 0) {
                    new Chart(cizgiCtx, {
                        type: 'line',
                        data: {
                            labels: data.Son7GunTarihleri,
                            datasets: [{
                                label: 'Günlük Üretim Adedi',
                                data: data.GunlukUretimSayilari,
                                fill: false,
                                borderColor: 'rgb(75, 192, 192)',
                                tension: 0.1
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
    
                const hataliTrendCtx = document.getElementById('hataliTrendGrafigi').getContext('2d');
                if ((data.Son7GunTarihleri || []).length > 0) {
                    new Chart(hataliTrendCtx, {
                        type: 'line',
                        data: {
                            labels: data.Son7GunTarihleri,
                            datasets: [{
                                label: 'Hatalı Ürün Adedi',
                                data: data.GunlukHataliUrunSayilari,
                                fill: false,
                                borderColor: 'rgb(255, 99, 132)',
                                tension: 0.1
                            }]
                        },
                        options: {
                            responsive: true,
                            scales: {
                                y: { beginAtZero: true }
                            }
                        }
                    });
                }
    
                const urunSureCtx = document.getElementById('urunSureGrafigi').getContext('2d');
                if ((data.UrunIsimleri || []).length > 0) {
                    new Chart(urunSureCtx, {
                        type: 'bar',
                        data: {
                            labels: data.UrunIsimleri,
                            datasets: [{
                                label: 'Süre Yüzdesi (%)',
                                data: data.UrunHarcananSure,
                                backgroundColor: [
                                    'rgba(255, 99, 132, 0.8)',
                                    'rgba(54, 162, 235, 0.8)',
                                    'rgba(255, 206, 86, 0.8)',
                                    'rgba(75, 192, 192, 0.8)',
                                    'rgba(153, 102, 255, 0.8)',
                                    'rgba(255, 159, 64, 0.8)'
                                ],
                                borderColor: 'rgba(255, 255, 255, 0.9)',
                                borderWidth: 2
                            }]
                        },
                        options: {
                            responsive: true,
                            plugins: {
                                legend: {
                                    display: false
                                }
                            },
                            scales: {
                                y: {
                                    beginAtZero: true,
                                    ticks: {
                                        callback: function(value) {
                                            return value + '%'
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
    
                const hataNedenCtx = document.getElementById('hataNedenGrafigi').getContext('2d');
                if ((data.HataNedenleri || []).length > 0) {
                    new Chart(hataNedenCtx, {
                        type: 'doughnut',
                        data: {
                            labels: data.HataNedenleri,
                            datasets: [{
                                label: 'Hatalı Adet',
                                data: data.HataNedenAdetleri,
                                backgroundColor: [
                                    'rgba(255, 99, 132, 0.8)',
                                    'rgba(54, 162, 235, 0.8)',
                                    'rgba(255, 206, 86, 0.8)',
                                    'rgba(75, 192, 192, 0.8)',
                                    'rgba(153, 102, 255, 0.8)',
                                    'rgba(255, 159, 64, 0.8)'
                                ],
                                borderColor: 'rgba(255, 255, 255, 0.9)',
                                borderWidth: 2
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            plugins: {
                                legend: {
                                    position: 'top',
                                }
                            }
                        }
                    });
                }
    
                const hataUrunSonucCtx = document.getElementById('hataUrunSonucGrafigi').getContext('2d');
                if ((data.HataUrunSonuclari || []).length > 0) {
                    new Chart(hataUrunSonucCtx, {
                        type: 'doughnut',
                        data: {
                            labels: data.HataUrunSonuclari,
                            datasets: [{
                                label: 'Hatalı Ürün Adet',
                                data: data.HataUrunSonucAdetleri,
                                backgroundColor: [
                                    'rgba(255, 99, 132, 0.8)',
                                    'rgba(54, 162, 235, 0.8)',
                                    'rgba(255, 206, 86, 0.8)',
                                    'rgba(75, 192, 192, 0.8)',
                                    'rgba(153, 102, 255, 0.8)',
                                    'rgba(255, 159, 64, 0.8)'
                                ],
                                borderColor: 'rgba(255, 255, 255, 0.9)',
                                borderWidth: 2
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            plugins: {
                                legend: {
                                    position: 'top',
                                }
                            }
                        }
                    });
                }
});
