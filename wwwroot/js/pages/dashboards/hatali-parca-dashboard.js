document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('hatali-parca-dashboard-data').textContent);
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
                    window.location.href = '/Home/HataliParcaDashboard?clear=1';
                });
    
                new Chart(document.getElementById('hataAdetTrend').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Hatalı Adet',
                            data: data.HataAdetTrendData,
                            borderColor: 'rgba(255, 99, 132, 0.9)',
                            backgroundColor: 'rgba(255, 99, 132, 0.15)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('hataM2Trend').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Hatalı m²',
                            data: data.HataM2TrendData,
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            backgroundColor: 'rgba(54, 162, 235, 0.15)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('hataNedenGrafigi').getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: data.HataNedenLabels,
                        datasets: [{
                            data: data.HataNedenData,
                            backgroundColor: [
                                '#ff6384', '#36a2eb', '#ffcd56', '#4bc0c0', '#9966ff',
                                '#ff9f40', '#c9cbcf', '#7acbf9', '#f06292', '#81c784'
                            ]
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
    
                new Chart(document.getElementById('bolumGrafigi').getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: data.BolumLabels,
                        datasets: [{
                            label: 'Hatalı Adet',
                            data: data.BolumData,
                            backgroundColor: 'rgba(54, 162, 235, 0.6)',
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            borderWidth: 1
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('operatorGrafigi').getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: data.OperatorLabels,
                        datasets: [{
                            label: 'Hatalı Adet',
                            data: data.OperatorData,
                            backgroundColor: 'rgba(153, 102, 255, 0.6)',
                            borderColor: 'rgba(153, 102, 255, 0.9)',
                            borderWidth: 1
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('kalinlikGrafigi').getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: data.KalinlikLabels,
                        datasets: [{
                            label: 'Hatalı Adet',
                            data: data.KalinlikData,
                            backgroundColor: 'rgba(75, 192, 192, 0.6)',
                            borderColor: 'rgba(75, 192, 192, 0.9)',
                            borderWidth: 1
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('renkGrafigi').getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: data.RenkLabels,
                        datasets: [{
                            label: 'Hatalı Adet',
                            data: data.RenkData,
                            backgroundColor: 'rgba(255, 159, 64, 0.6)',
                            borderColor: 'rgba(255, 159, 64, 0.9)',
                            borderWidth: 1
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('kesimDurumGrafigi').getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: data.KesimDurumLabels,
                        datasets: [{
                            data: data.KesimDurumData,
                            backgroundColor: ['#36a2eb', '#ffcd56', '#ff6384', '#4bc0c0', '#9966ff']
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
    
                new Chart(document.getElementById('pvcDurumGrafigi').getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: data.PvcDurumLabels,
                        datasets: [{
                            data: data.PvcDurumData,
                            backgroundColor: ['#4bc0c0', '#ff9f40', '#c9cbcf', '#ff6384', '#36a2eb']
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
});
