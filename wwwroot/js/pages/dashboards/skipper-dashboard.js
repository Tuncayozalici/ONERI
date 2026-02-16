document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('skipper-dashboard-data').textContent);
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
    
                    if (clear === '1') {
                        dateInput.value = '';
                        monthInput.value = '';
                        yearInput.value = String(defaultYear);
                    } else if (raporTarihi) {
                        dateInput.value = raporTarihi;
                    } else if (ay && yil) {
                        monthInput.value = ay;
                        yearInput.value = yil;
                        dateInput.value = '';
                    } else {
                        setDefaultDate();
                    }
                }
    
                toggleFilters();
    
                monthInput.addEventListener('change', toggleFilters);
                dateInput.addEventListener('input', toggleFilters);
    
                clearButton.addEventListener('click', function() {
                    monthInput.value = '';
                    yearInput.value = String(defaultYear);
                    dateInput.value = '';
                    toggleFilters();
                    window.location.href = '/Home/SkipperDashboard?clear=1';
                });
    
                const uretimTrendCtx = document.getElementById('uretimTrendGrafigi').getContext('2d');
                new Chart(uretimTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Delik',
                            data: data.DelikTrendData,
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            backgroundColor: 'rgba(54, 162, 235, 0.2)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                const oranTrendCtx = document.getElementById('oranTrendGrafigi').getContext('2d');
                new Chart(oranTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [
                            {
                                label: 'Performans',
                                data: data.UretimOraniTrendData,
                                borderColor: 'rgba(54, 162, 235, 0.9)',
                                backgroundColor: 'rgba(54, 162, 235, 0.15)',
                                tension: 0.2,
                                fill: true
                            },
                            {
                                label: 'Kayıp Süre',
                                data: data.KayipSureTrendData,
                                borderColor: 'rgba(255, 99, 132, 0.9)',
                                backgroundColor: 'rgba(255, 99, 132, 0.12)',
                                tension: 0.2,
                                fill: true
                            }
                        ]
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

                const oeeTrendCtx = document.getElementById('oeeTrendGrafigi').getContext('2d');
                new Chart(oeeTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
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
    
                const duraklamaCtx = document.getElementById('duraklamaNedenGrafigi').getContext('2d');
                new Chart(duraklamaCtx, {
                    type: 'doughnut',
                    data: {
                        labels: data.DuraklamaNedenLabels,
                        datasets: [{
                            data: data.DuraklamaNedenData,
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
