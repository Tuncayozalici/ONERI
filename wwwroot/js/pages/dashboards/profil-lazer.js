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
                    window.location.href = '/Home/ProfilLazerVerileri?clear=1';
                });
    
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
