document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('tezgah-dashboard-data').textContent);
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
                const dateInput = document.getElementById('raporTarihi');
                const monthInput = document.getElementById('ay');
                const yearInput = document.getElementById('yil');
                const clearButton = document.getElementById('clearFilter');
    
                function toggleFilters() {
                    if (monthInput.value) {
                        dateInput.disabled = true;
                        dateInput.value = '';
                    } else {
                        dateInput.disabled = false;
                    }
    
                    if (dateInput.value) {
                        monthInput.disabled = true;
                        yearInput.disabled = true;
                        monthInput.value = '';
                        yearInput.value = String(defaultYear);
                    } else {
                        monthInput.disabled = false;
                        yearInput.disabled = false;
                    }
                }
    
                function setDefaultDate() {
                    const today = new Date();
                    const year = today.getFullYear();
                    const month = String(today.getMonth() + 1).padStart(2, '0');
                    const day = String(today.getDate()).padStart(2, '0');
                    dateInput.value = `${year}-${month}-${day}`;
                }
    
                if (dateInput) {
                    const urlParams = new URLSearchParams(window.location.search);
                    const raporTarihi = urlParams.get('raporTarihi');
                    const ay = urlParams.get('ay');
                    const yil = urlParams.get('yil');
                    const clear = urlParams.get('clear');
                    const resolvedYear = payload.resolvedYear;
    
                    if (clear === '1') {
                        dateInput.value = '';
                        monthInput.value = '';
                        yearInput.value = String(defaultYear);
                    } else if (raporTarihi) {
                        dateInput.value = raporTarihi;
                    } else if (ay && yil) {
                        monthInput.value = ay;
                        yearInput.value = resolvedYear ?? yil;
                        dateInput.value = '';
                    } else {
                        setDefaultDate();
                    }
                }
    
                toggleFilters();
    
                monthInput.addEventListener('change', toggleFilters);
                dateInput.addEventListener('input', toggleFilters);
                const filterForm = dateInput.closest('form');
                if (filterForm) {
                    filterForm.addEventListener('submit', function() {
                        if (monthInput.value) {
                            dateInput.value = '';
                            dateInput.disabled = true;
                        }
                    });
                }
    
                clearButton.addEventListener('click', function() {
                    monthInput.value = '';
                    yearInput.value = String(defaultYear);
                    dateInput.value = '';
                    toggleFilters();
                    window.location.href = '/Home/TezgahDashboard?clear=1';
                });
    
                const parcaTrendCtx = document.getElementById('parcaTrendGrafigi').getContext('2d');
                new Chart(parcaTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Parça Adedi',
                            data: data.ParcaTrendData,
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            backgroundColor: 'rgba(54, 162, 235, 0.2)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                const kisiTrendCtx = document.getElementById('kisiTrendGrafigi').getContext('2d');
                new Chart(kisiTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Kişi Sayısı',
                            data: data.KisiTrendData,
                            borderColor: 'rgba(75, 192, 192, 0.9)',
                            backgroundColor: 'rgba(75, 192, 192, 0.2)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                const kullanilabilirlikCtx = document.getElementById('kullanilabilirlikGrafigi').getContext('2d');
                new Chart(kullanilabilirlikCtx, {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Kullanılabilirlik (%)',
                            data: data.KullanilabilirlikTrendData,
                            borderColor: 'rgba(255, 159, 64, 0.9)',
                            backgroundColor: 'rgba(255, 159, 64, 0.15)',
                            tension: 0.2,
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
    
                const kayipCtx = document.getElementById('kayipNedenGrafigi').getContext('2d');
                new Chart(kayipCtx, {
                    type: 'doughnut',
                    data: {
                        labels: data.KayipNedenLabels,
                        datasets: [{
                            data: data.KayipNedenData,
                            backgroundColor: [
                                '#ff6384', '#36a2eb', '#ffcd56', '#4bc0c0', '#9966ff',
                                '#ff9f40', '#c9cbcf', '#7acbf9', '#f06292', '#81c784'
                            ]
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false
                    }
                });
});
