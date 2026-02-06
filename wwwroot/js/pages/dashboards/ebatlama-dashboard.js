document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('ebatlama-dashboard-data').textContent);
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
                    window.location.href = '/Home/EbatlamaDashboard?clear=1';
                });
    
                new Chart(document.getElementById('kesimTrendGrafigi').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Toplam Kesim',
                            data: data.KesimTrendData,
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            backgroundColor: 'rgba(54, 162, 235, 0.2)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('plakaTrendGrafigi').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [
                            {
                                label: '8mm Plaka',
                                data: data.Plaka8TrendData,
                                borderColor: 'rgba(255, 99, 132, 0.9)',
                                backgroundColor: 'rgba(255, 99, 132, 0.12)',
                                tension: 0.2,
                                fill: true
                            },
                            {
                                label: '18mm Plaka',
                                data: data.Plaka18TrendData,
                                borderColor: 'rgba(75, 192, 192, 0.9)',
                                backgroundColor: 'rgba(75, 192, 192, 0.12)',
                                tension: 0.2,
                                fill: true
                            },
                            {
                                label: '30mm Plaka',
                                data: data.Plaka30TrendData,
                                borderColor: 'rgba(255, 159, 64, 0.9)',
                                backgroundColor: 'rgba(255, 159, 64, 0.12)',
                                tension: 0.2,
                                fill: true
                            }
                        ]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('kesimAdetTrendGrafigi').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [
                            {
                                label: '8mm Kesim Adeti',
                                data: data.Kesim8TrendData,
                                borderColor: 'rgba(153, 102, 255, 0.9)',
                                backgroundColor: 'rgba(153, 102, 255, 0.12)',
                                tension: 0.2,
                                fill: true
                            },
                            {
                                label: '30mm Kesim Adeti',
                                data: data.Kesim30TrendData,
                                borderColor: 'rgba(54, 162, 235, 0.9)',
                                backgroundColor: 'rgba(54, 162, 235, 0.12)',
                                tension: 0.2,
                                fill: true
                            }
                        ]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('gonyTrendGrafigi').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Gönyelleme',
                            data: data.GonyellemeTrendData,
                            borderColor: 'rgba(255, 205, 86, 0.9)',
                            backgroundColor: 'rgba(255, 205, 86, 0.12)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('hazirlikTrendGrafigi').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Hazırlık / Malzeme (dk)',
                            data: data.HazirlikTrendData,
                            borderColor: 'rgba(201, 203, 207, 0.9)',
                            backgroundColor: 'rgba(201, 203, 207, 0.2)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('makineKesimGrafigi').getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: data.MakineLabels,
                        datasets: [{
                            label: 'Toplam Kesim',
                            data: data.MakineKesimData,
                            backgroundColor: 'rgba(54, 162, 235, 0.6)',
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            borderWidth: 1
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('mesaiDurumGrafigi').getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: data.MesaiLabels,
                        datasets: [{
                            label: 'Toplam Kesim',
                            data: data.MesaiData,
                            backgroundColor: 'rgba(75, 192, 192, 0.6)',
                            borderColor: 'rgba(75, 192, 192, 0.9)',
                            borderWidth: 1
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('duraklamaNedenGrafigi').getContext('2d'), {
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
