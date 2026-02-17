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
                    } else {
                        summaryDate.value = getModelDateIso();
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
                    options: {
                        responsive: true,
                        maintainAspectRatio: false
                    }
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
                    options: {
                        responsive: true,
                        maintainAspectRatio: false
                    }
                });

                const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
                const axisTickColor = isDarkTheme ? '#cbd5e1' : '#1f2937';
                const axisGridColor = isDarkTheme ? 'rgba(148, 163, 184, 0.16)' : 'rgba(148, 163, 184, 0.45)';
                const oeeAccentColor = isDarkTheme ? '#34d399' : '#16a34a';
                const clampPercent = (value) => {
                    const num = Number(value) || 0;
                    return Math.max(0, Math.min(100, num));
                };

                const genelBilesenCanvas = document.getElementById('genelBilesenGrafigi');
                if (genelBilesenCanvas) {
                    new Chart(genelBilesenCanvas.getContext('2d'), {
                        type: 'bar',
                        data: {
                            labels: ['Performans', 'Kullanılabilirlik', 'Kalite'],
                            datasets: [{
                                label: 'Ortalama (%)',
                                data: [
                                    clampPercent(data.OrtalamaPerformans),
                                    clampPercent(data.OrtalamaKullanilabilirlik),
                                    clampPercent(data.OrtalamaKalite)
                                ],
                                backgroundColor: [
                                    'rgba(59, 130, 246, 0.82)',
                                    'rgba(16, 185, 129, 0.82)',
                                    'rgba(168, 85, 247, 0.82)'
                                ],
                                borderColor: [
                                    'rgba(59, 130, 246, 1)',
                                    'rgba(16, 185, 129, 1)',
                                    'rgba(168, 85, 247, 1)'
                                ],
                                borderWidth: 1,
                                borderRadius: 8,
                                barThickness: 20
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            indexAxis: 'y',
                            scales: {
                                x: {
                                    min: 0,
                                    max: 100,
                                    ticks: {
                                        color: axisTickColor,
                                        callback: (value) => `${value}%`
                                    },
                                    grid: { color: axisGridColor }
                                },
                                y: {
                                    ticks: { color: axisTickColor },
                                    grid: { color: axisGridColor }
                                }
                            },
                            plugins: {
                                legend: { labels: { color: axisTickColor } }
                            }
                        }
                    });
                }

                const genelOeeGaugeCanvas = document.getElementById('genelOeeGaugeGrafigi');
                if (genelOeeGaugeCanvas) {
                    const oeeValue = clampPercent(data.OrtalamaOee);
                    const centerTextPlugin = {
                        id: 'centerTextPlugin',
                        afterDraw(chart) {
                            const { ctx, chartArea } = chart;
                            if (!chartArea) return;
                            const centerX = chartArea.left + (chartArea.right - chartArea.left) / 2;
                            const centerY = chartArea.top + (chartArea.bottom - chartArea.top) * 0.82;
                            ctx.save();
                            ctx.textAlign = 'center';
                            ctx.fillStyle = oeeAccentColor;
                            ctx.font = '700 30px Poppins, sans-serif';
                            ctx.fillText(`${oeeValue.toFixed(2)} %`, centerX, centerY);
                            ctx.fillStyle = isDarkTheme ? '#cbd5e1' : '#334155';
                            ctx.font = '500 13px Poppins, sans-serif';
                            ctx.fillText('Genel OEE', centerX, centerY + 24);
                            ctx.restore();
                        }
                    };

                    new Chart(genelOeeGaugeCanvas.getContext('2d'), {
                        type: 'doughnut',
                        data: {
                            labels: ['OEE', 'Kalan'],
                            datasets: [{
                                data: [oeeValue, 100 - oeeValue],
                                backgroundColor: [
                                    isDarkTheme ? 'rgba(52, 211, 153, 0.9)' : 'rgba(22, 163, 74, 0.9)',
                                    isDarkTheme ? 'rgba(71, 85, 105, 0.45)' : 'rgba(148, 163, 184, 0.45)'
                                ],
                                borderWidth: 0,
                                hoverOffset: 0
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            rotation: -90,
                            circumference: 180,
                            cutout: '74%',
                            plugins: {
                                legend: { display: false },
                                tooltip: {
                                    callbacks: {
                                        label: (context) => `${context.label}: ${context.parsed.toFixed(2)}%`
                                    }
                                }
                            }
                        },
                        plugins: [centerTextPlugin]
                    });
                }

                const makineOeeCanvas = document.getElementById('makineOeeGrafigi');
                if (makineOeeCanvas) {
                    const machineLabels = Array.isArray(data.MakineOeeLabels) ? data.MakineOeeLabels : [];
                    const machineValues = (Array.isArray(data.MakineOeeData) ? data.MakineOeeData : []).map(clampPercent);
                    const hasMachineData = machineLabels.length > 0 && machineValues.some(v => v > 0);

                    new Chart(makineOeeCanvas.getContext('2d'), {
                        type: 'bar',
                        data: {
                            labels: hasMachineData ? machineLabels : ['Veri Yok'],
                            datasets: [{
                                label: 'Ortalama OEE (%)',
                                data: hasMachineData ? machineValues : [0],
                                backgroundColor: hasMachineData
                                    ? 'rgba(22, 163, 74, 0.78)'
                                    : 'rgba(148, 163, 184, 0.35)',
                                borderColor: hasMachineData
                                    ? 'rgba(22, 163, 74, 1)'
                                    : 'rgba(100, 116, 139, 0.7)',
                                borderWidth: 1,
                                borderRadius: 8,
                                barThickness: 22
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            indexAxis: 'y',
                            scales: {
                                x: {
                                    min: 0,
                                    max: 100,
                                    ticks: {
                                        color: axisTickColor,
                                        callback: (value) => `${value}%`
                                    },
                                    grid: { color: axisGridColor }
                                },
                                y: {
                                    ticks: { color: axisTickColor },
                                    grid: { color: axisGridColor }
                                }
                            },
                            plugins: {
                                legend: { labels: { color: axisTickColor } }
                            }
                        }
                    });
                }
});
