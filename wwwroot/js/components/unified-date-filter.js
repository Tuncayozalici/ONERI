document.addEventListener("DOMContentLoaded", function () {
    const monthNames = [
        "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    ];

    const forms = document.querySelectorAll("form.js-unified-date-filter-form");
    forms.forEach(initUnifiedDateFilter);

    function initUnifiedDateFilter(form) {
        const root = form.querySelector(".js-unified-date-filter");
        if (!root) {
            return;
        }

        const trigger = root.querySelector(".js-udf-trigger");
        const display = root.querySelector(".js-udf-display");
        const popover = root.querySelector(".js-udf-popover");
        const modeSelect = root.querySelector(".js-udf-mode");
        const singlePanel = root.querySelector(".js-udf-panel-single");
        const rangePanel = root.querySelector(".js-udf-panel-range");
        const monthPanel = root.querySelector(".js-udf-panel-month");
        const singleInput = root.querySelector(".js-udf-single");
        const rangeStartInput = root.querySelector(".js-udf-range-start");
        const rangeEndInput = root.querySelector(".js-udf-range-end");
        const prevYearButton = root.querySelector(".js-udf-prev-year");
        const nextYearButton = root.querySelector(".js-udf-next-year");
        const monthYearLabel = root.querySelector(".js-udf-month-year");
        const monthGrid = root.querySelector(".js-udf-month-grid");
        const hiddenStart = root.querySelector(".js-udf-start");
        const hiddenEnd = root.querySelector(".js-udf-end");
        const applyButton = form.querySelector(".js-unified-filter-apply");
        const clearButton = form.querySelector(".js-unified-filter-clear");

        if (!trigger || !display || !popover || !modeSelect || !singlePanel || !rangePanel || !monthPanel ||
            !singleInput || !rangeStartInput || !rangeEndInput || !prevYearButton || !nextYearButton ||
            !monthYearLabel || !monthGrid || !hiddenStart || !hiddenEnd || !applyButton || !clearButton) {
            return;
        }

        const initialStart = parseIsoDate(hiddenStart.value);
        const initialEnd = parseIsoDate(hiddenEnd.value);
        const initialMode = detectMode(initialStart, initialEnd);

        const state = {
            mode: initialMode,
            startDate: initialStart,
            endDate: initialEnd,
            monthYear: (initialStart || new Date()).getFullYear(),
            monthValue: initialStart ? initialStart.getMonth() : null
        };

        modeSelect.value = state.mode;
        render();

        trigger.addEventListener("click", function () {
            popover.classList.toggle("d-none");
        });

        modeSelect.addEventListener("change", function () {
            state.mode = modeSelect.value;
            state.startDate = null;
            state.endDate = null;
            state.monthValue = null;
            singleInput.value = "";
            rangeStartInput.value = "";
            rangeEndInput.value = "";
            render();
        });

        singleInput.addEventListener("change", function () {
            const selected = normalizeAllowedDate(parseIsoDate(singleInput.value));
            state.startDate = selected;
            state.endDate = selected;
            render();
        });

        rangeStartInput.addEventListener("change", function () {
            state.startDate = normalizeAllowedDate(parseIsoDate(rangeStartInput.value));
            render();
        });

        rangeEndInput.addEventListener("change", function () {
            state.endDate = normalizeAllowedDate(parseIsoDate(rangeEndInput.value));
            render();
        });

        prevYearButton.addEventListener("click", function () {
            state.monthYear -= 1;
            renderMonthGrid();
        });

        nextYearButton.addEventListener("click", function () {
            state.monthYear += 1;
            renderMonthGrid();
        });

        clearButton.addEventListener("click", function () {
            state.mode = "single_day";
            state.startDate = null;
            state.endDate = null;
            state.monthValue = null;
            hiddenStart.value = "";
            hiddenEnd.value = "";
            render();
            window.location.href = window.location.pathname;
        });

        form.addEventListener("submit", function (event) {
            if (!isValid()) {
                event.preventDefault();
                return;
            }

            syncHiddenDates();
        });

        document.addEventListener("click", function (event) {
            if (!root.contains(event.target)) {
                closePopover();
            }
        });

        function render() {
            singlePanel.classList.toggle("d-none", state.mode !== "single_day");
            rangePanel.classList.toggle("d-none", state.mode !== "date_range");
            monthPanel.classList.toggle("d-none", state.mode !== "month");

            if (state.startDate && state.mode === "single_day") {
                singleInput.value = toIsoDate(state.startDate);
            }

            if (state.mode === "date_range") {
                rangeStartInput.value = state.startDate ? toIsoDate(state.startDate) : "";
                rangeEndInput.value = state.endDate ? toIsoDate(state.endDate) : "";
            }

            if (state.mode === "month") {
                renderMonthGrid();
            }

            syncHiddenDates();
            display.textContent = getDisplayValue();
            applyButton.disabled = !isValid();
            clearButton.disabled = !(state.startDate && state.endDate);
        }

        function renderMonthGrid() {
            monthYearLabel.textContent = String(state.monthYear);
            monthGrid.innerHTML = "";

            monthNames.forEach(function (monthName, monthIndex) {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "unified-date-filter__month-btn";
                button.textContent = monthName;

                if (state.monthValue === monthIndex && state.startDate && state.endDate &&
                    state.startDate.getFullYear() === state.monthYear) {
                    button.classList.add("active");
                }

                button.addEventListener("click", function () {
                    state.monthValue = monthIndex;
                    state.startDate = new Date(state.monthYear, monthIndex, 1);
                    state.endDate = new Date(state.monthYear, monthIndex + 1, 0);
                    render();
                    closePopover();
                });

                monthGrid.appendChild(button);
            });
        }

        function syncHiddenDates() {
            hiddenStart.value = state.startDate ? toIsoDate(state.startDate) : "";
            hiddenEnd.value = state.endDate ? toIsoDate(state.endDate) : "";
        }

        function isValid() {
            if (!state.startDate || !state.endDate) {
                return false;
            }

            if (state.endDate < state.startDate) {
                return false;
            }

            if (state.mode === "single_day") {
                return state.startDate.getTime() === state.endDate.getTime();
            }

            if (state.mode === "month") {
                return state.startDate.getDate() === 1 &&
                    state.startDate.getFullYear() === state.endDate.getFullYear() &&
                    state.startDate.getMonth() === state.endDate.getMonth() &&
                    state.endDate.getDate() === new Date(state.endDate.getFullYear(), state.endDate.getMonth() + 1, 0).getDate();
            }

            return true;
        }

        function getDisplayValue() {
            if (!state.startDate || !state.endDate) {
                if (state.mode === "single_day") return "Gün Seç";
                if (state.mode === "date_range") return "Başlangıç-Bitiş";
                return "Ay Seç";
            }

            if (state.mode === "single_day") {
                return formatDateTr(state.startDate);
            }

            if (state.mode === "month") {
                return formatMonthTr(state.startDate);
            }

            return formatDateTr(state.startDate) + " - " + formatDateTr(state.endDate);
        }

        function closePopover() {
            popover.classList.add("d-none");
        }
    }

    function parseIsoDate(value) {
        if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
            return null;
        }

        const [year, month, day] = value.split("-").map(Number);
        return new Date(year, month - 1, day);
    }

    function normalizeAllowedDate(date) {
        if (!date || Number.isNaN(date.getTime())) {
            return null;
        }

        const year = date.getFullYear();
        if (year < 2000 || year > 2099) {
            return null;
        }

        return date;
    }

    function toIsoDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    }

    function formatDateTr(date) {
        const day = String(date.getDate()).padStart(2, "0");
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const year = date.getFullYear();
        return `${day}.${month}.${year}`;
    }

    function formatMonthTr(date) {
        return new Intl.DateTimeFormat("tr-TR", {
            month: "long",
            year: "numeric"
        }).format(date);
    }

    function detectMode(startDate, endDate) {
        if (!startDate || !endDate) {
            return "single_day";
        }

        if (startDate.getTime() === endDate.getTime()) {
            return "single_day";
        }

        const isMonthSelection =
            startDate.getDate() === 1 &&
            startDate.getFullYear() === endDate.getFullYear() &&
            startDate.getMonth() === endDate.getMonth() &&
            endDate.getDate() === new Date(endDate.getFullYear(), endDate.getMonth() + 1, 0).getDate();

        return isMonthSelection ? "month" : "date_range";
    }

});
