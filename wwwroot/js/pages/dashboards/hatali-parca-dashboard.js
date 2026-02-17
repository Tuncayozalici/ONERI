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

                function setDefaultDate() {
                    const modelDateIso = getModelDateIso();
                    if (modelDateIso) {
                        dateInput.value = modelDateIso;
                        return;
                    }

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
    
                const hataNedenCardHeader = document.querySelector('#hataNedenGrafigi')?.closest('.card')?.querySelector('.card-header');
                const anaHataNedenBaslik = 'Hata Nedenleri';
                const donutColors = [
                    '#ff6384', '#36a2eb', '#ffcd56', '#4bc0c0', '#9966ff',
                    '#ff9f40', '#c9cbcf', '#7acbf9', '#f06292', '#81c784'
                ];
                const normalizeBolumKey = (value) => String(value ?? '')
                    .trim()
                    .toLocaleLowerCase('tr-TR');

                const bolumBazliMap = new Map(
                    (Array.isArray(data.BolumBazliHataNedenleri) ? data.BolumBazliHataNedenleri : []).map(item => [
                        normalizeBolumKey(item.Bolum),
                        {
                            labels: Array.isArray(item.NedenLabels) ? item.NedenLabels : [],
                            values: Array.isArray(item.NedenData) ? item.NedenData : []
                        }
                    ])
                );

                const hataNedenChart = new Chart(document.getElementById('hataNedenGrafigi').getContext('2d'), {
                    type: 'doughnut',
                    data: {
                        labels: data.HataNedenLabels,
                        datasets: [{
                            data: data.HataNedenData,
                            backgroundColor: donutColors
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });

                let seciliBolum = null;
                const bolumBarDefault = 'rgba(54, 162, 235, 0.6)';
                const bolumBarSelected = 'rgba(59, 130, 246, 0.95)';

                function updateHataNedenByBolum(bolum) {
                    const detay = bolum ? bolumBazliMap.get(normalizeBolumKey(bolum)) : null;
                    if (detay && detay.labels.length > 0) {
                        hataNedenChart.data.labels = detay.labels;
                        hataNedenChart.data.datasets[0].data = detay.values;
                        if (hataNedenCardHeader) {
                            hataNedenCardHeader.textContent = `Hata Nedenleri - ${bolum}`;
                        }
                    } else {
                        hataNedenChart.data.labels = data.HataNedenLabels;
                        hataNedenChart.data.datasets[0].data = data.HataNedenData;
                        if (hataNedenCardHeader) {
                            hataNedenCardHeader.textContent = anaHataNedenBaslik;
                        }
                    }
                    hataNedenChart.update();
                }

                const bolumChart = new Chart(document.getElementById('bolumGrafigi').getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: data.BolumLabels,
                        datasets: [{
                            label: 'Hatalı Adet',
                            data: data.BolumData,
                            backgroundColor: data.BolumLabels.map(() => bolumBarDefault),
                            borderColor: 'rgba(54, 162, 235, 0.9)',
                            borderWidth: 1
                        }]
                    },
                    options: {
                        responsive: true,
                        onClick: function (_, elements) {
                            if (!elements || !elements.length) {
                                return;
                            }

                            const index = elements[0].index;
                            const tiklananBolum = data.BolumLabels[index];
                            seciliBolum = seciliBolum === tiklananBolum ? null : tiklananBolum;

                            bolumChart.data.datasets[0].backgroundColor = data.BolumLabels.map(label =>
                                label === seciliBolum ? bolumBarSelected : bolumBarDefault
                            );
                            bolumChart.update();

                            updateHataNedenByBolum(seciliBolum);
                        },
                        onHover: function (event, elements) {
                            event.native.target.style.cursor = elements.length ? 'pointer' : 'default';
                        }
                    }
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
    
});
