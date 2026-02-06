document.addEventListener('DOMContentLoaded', function () {
    const payload = JSON.parse(document.getElementById('gunluk-veriler-data').textContent);
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
                const summaryCard = document.getElementById('summaryCard');
                const summaryToggle = document.getElementById('summaryUltraToggle');
                if (summaryCard && summaryToggle) {
                    const saved = localStorage.getItem('summary_ultra');
                    const enabled = saved === '1';
                    summaryCard.classList.toggle('summary-ultra', enabled);
                    summaryToggle.checked = enabled;
                    summaryToggle.addEventListener('change', function() {
                        summaryCard.classList.toggle('summary-ultra', summaryToggle.checked);
                        localStorage.setItem('summary_ultra', summaryToggle.checked ? '1' : '0');
                    });
                }
    
                const summaryDate = document.getElementById('summaryDate');
                const summaryMonth = document.getElementById('summaryMonth');
                const summaryYear = document.getElementById('summaryYear');
                const summaryClear = document.getElementById('summaryClear');
                const summaryForm = document.getElementById('summaryFilterForm');
    
                function toggleSummaryFilters() {
                    if (summaryMonth.value) {
                        summaryDate.disabled = true;
                        summaryDate.value = '';
                    } else {
                        summaryDate.disabled = false;
                    }
    
                    if (summaryDate.value) {
                        summaryMonth.disabled = true;
                        summaryYear.disabled = true;
                        summaryMonth.value = '';
                        summaryYear.value = String(defaultYear);
                    } else {
                        summaryMonth.disabled = false;
                        summaryYear.disabled = false;
                    }
                }
    
                if (summaryDate) {
                    const urlParams = new URLSearchParams(window.location.search);
                    const raporTarihi = urlParams.get('raporTarihi');
                    const ay = urlParams.get('ay');
                    const yil = urlParams.get('yil');
                    const resolvedYear = payload.resolvedYear;
    
                    if (raporTarihi) {
                        summaryDate.value = raporTarihi;
                    } else if (ay && yil) {
                        summaryMonth.value = ay;
                        summaryYear.value = resolvedYear ?? yil;
                        summaryDate.value = '';
                    }
                }
    
                toggleSummaryFilters();
    
                summaryMonth.addEventListener('change', toggleSummaryFilters);
                summaryDate.addEventListener('input', toggleSummaryFilters);
    
                if (summaryForm) {
                    summaryForm.addEventListener('submit', function() {
                        if (summaryMonth.value) {
                            summaryDate.value = '';
                            summaryDate.disabled = true;
                        }
                    });
                }
    
                summaryClear.addEventListener('click', function() {
                    summaryMonth.value = '';
                    summaryYear.value = String(defaultYear);
                    summaryDate.value = '';
                    toggleSummaryFilters();
                    window.location.href = '/Home/GunlukVeriler';
                });
    
                new Chart(document.getElementById('genelUretimTrend').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Toplam Üretim',
                            data: data.UretimTrendData,
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            backgroundColor: 'rgba(54, 162, 235, 0.15)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('genelHataTrend').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Hatalı Adet',
                            data: data.HataTrendData,
                            borderColor: 'rgba(255, 99, 132, 0.9)',
                            backgroundColor: 'rgba(255, 99, 132, 0.15)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('bolumKatkiGrafigi').getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: data.BolumUretimLabels,
                        datasets: [{
                            label: 'Üretim',
                            data: data.BolumUretimData,
                            backgroundColor: 'rgba(75, 192, 192, 0.6)',
                            borderColor: 'rgba(75, 192, 192, 0.9)',
                            borderWidth: 1
                        }]
                    },
                    options: { responsive: true }
                });
    
                new Chart(document.getElementById('hataNedenGenelGrafigi').getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: data.HataNedenLabels,
                        datasets: [{
                            data: data.HataNedenData,
                            backgroundColor: ['#ff6384', '#36a2eb', '#ffcd56', '#4bc0c0', '#9966ff', '#ff9f40']
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
    
                new Chart(document.getElementById('duraklamaTrendGrafigi').getContext('2d'), {
                    type: 'line',
                    data: {
                        labels: data.TrendLabels,
                        datasets: [{
                            label: 'Duraklama (dk)',
                            data: data.DuraklamaTrendData,
                            borderColor: 'rgba(153, 102, 255, 0.9)',
                            backgroundColor: 'rgba(153, 102, 255, 0.12)',
                            tension: 0.2,
                            fill: true
                        }]
                    },
                    options: { responsive: true }
                });
});
