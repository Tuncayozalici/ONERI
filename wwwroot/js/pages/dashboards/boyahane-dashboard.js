document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('boyahane-dashboard-data').textContent);
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
                    window.location.href = '/Home/BoyahaneDashboard?clear=1';
                });
    
                // Chart rendering code...
                // Üretim Dağılımı (Stacked Bar)
                const uretimDagilimCtx = document.getElementById('uretimDagilimGrafigi').getContext('2d');
                new Chart(uretimDagilimCtx, {
                    type: 'bar',
                    data: {
                        labels: data.UretimDagilimi.Labels,
                        datasets: [
                            {
                                label: 'Panel',
                                data: data.UretimDagilimi.PanelData,
                                backgroundColor: 'rgba(54, 162, 235, 0.8)',
                            },
                            {
                                label: 'Döşeme',
                                data: data.UretimDagilimi.DosemeData,
                                backgroundColor: 'rgba(255, 159, 64, 0.8)',
                            }
                        ]
                    },
                    options: {
                        responsive: true,
                        scales: {
                            x: {
                                stacked: true,
                            },
                            y: {
                                stacked: true,
                                beginAtZero: true
                            }
                        }
                    }
                });
    
                // Hata Nedenleri (Doughnut)
                const hataCtx = document.getElementById('hataGrafigi').getContext('2d');
                if ((data.HataNedenleriListesi || []).length > 0) {
                    new Chart(hataCtx, {
                        type: 'doughnut',
                        data: {
                            labels: data.HataNedenleriListesi,
                            datasets: [{
                                label: 'Hatalı Adet',
                                data: data.HataSayilariListesi,
                                backgroundColor: [
                                    'rgba(255, 99, 132, 0.8)',
                                    'rgba(54, 162, 235, 0.8)',
                                    'rgba(255, 206, 86, 0.8)',
                                    'rgba(75, 192, 192, 0.8)',
                                    'rgba(153, 102, 255, 0.8)',
                                    'rgba(255, 159, 64, 0.8)'
                                ],
                                borderWidth: 1
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
    
                // Kalite Trendi (Line)
                const kaliteTrendCtx = document.getElementById('kaliteTrendGrafigi').getContext('2d');
                new Chart(kaliteTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.KaliteTrendi.Labels,
                        datasets: [{
                            label: 'Günlük Hata Sayısı',
                            data: data.KaliteTrendi.Data,
                            borderColor: 'rgba(255, 99, 132, 1)',
                            backgroundColor: 'rgba(255, 99, 132, 0.2)',
                            fill: true,
                            tension: 0.4
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
    
                // Üretim Trendi (Line)
                const uretimTrendCtx = document.getElementById('uretimTrendGrafigi').getContext('2d');
                new Chart(uretimTrendCtx, {
                    type: 'line',
                    data: {
                        labels: data.UretimTrendi.Labels,
                        datasets: [{
                            label: 'Günlük Üretim Sayısı',
                            data: data.UretimTrendi.Data,
                            borderColor: 'rgba(75, 192, 192, 1)',
                            backgroundColor: 'rgba(75, 192, 192, 0.2)',
                            fill: true,
                            tension: 0.4
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
});
