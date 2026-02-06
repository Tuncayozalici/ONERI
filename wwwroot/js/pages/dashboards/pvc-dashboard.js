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
                    window.location.href = '/Home/PvcDashboard?clear=1';
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
    
                const makineUretimCtx = document.getElementById('makineUretimGrafigi').getContext('2d');
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
    
                const makineParcaCtx = document.getElementById('makineParcaGrafigi').getContext('2d');
                new Chart(makineParcaCtx, {
                    type: 'bar',
                    data: {
                        labels: data.MakineLabels,
                        datasets: [{
                            label: 'Parça',
                            data: data.MakineParcaData,
                            backgroundColor: 'rgba(54, 162, 235, 0.7)'
                        }]
                    },
                    options: { responsive: true }
                });
    
                const fiiliKayipCtx = document.getElementById('fiiliKayipGrafigi').getContext('2d');
                new Chart(fiiliKayipCtx, {
                    type: 'line',
                    data: {
                        labels: data.FiiliCalismaLabels,
                        datasets: [{
                            label: 'Fiili Çalışma (%)',
                            data: data.FiiliCalismaData,
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
