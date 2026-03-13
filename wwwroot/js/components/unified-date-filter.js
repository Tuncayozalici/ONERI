document.addEventListener("DOMContentLoaded", function () {
    const filterStorageKey = "oneri.dashboardDateFilter";
    const dashboardPaths = new Set([
        "/home/gunlukveriler",
        "/home/profillazerverileri",
        "/home/boyahanedashboard",
        "/home/pvcdashboard",
        "/home/cncdashboard",
        "/home/masterwooddashboard",
        "/home/skipperdashboard",
        "/home/roverbdashboard",
        "/home/tezgahdashboard",
        "/home/ebatlamadashboard",
        "/home/hataliparcadashboard"
    ]);
    const monthNames = [
        "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    ];

    if (hasClearFlag(window.location)) {
        clearStoredFilter();
    } else {
        const locationFilter = getFilterStateFromLocation(window.location);
        if (locationFilter) {
            persistFilterState(locationFilter);
        }
    }

    const initialStoredFilter = getStoredFilterState();
    if (!getFilterStateFromLocation(window.location) && isDashboardPath(window.location.pathname) && initialStoredFilter) {
        const redirectUrl = buildUrlWithFilter(window.location.href, initialStoredFilter);
        if (redirectUrl !== window.location.pathname + window.location.search + window.location.hash) {
            window.location.replace(redirectUrl);
            return;
        }
    }

    refreshDashboardLinks(initialStoredFilter);

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

        const fallbackFilter = (!hiddenStart.value && !hiddenEnd.value) ? getStoredFilterState() : null;
        const initialStart = parseIsoDate(hiddenStart.value || fallbackFilter?.startDate);
        const initialEnd = parseIsoDate(hiddenEnd.value || fallbackFilter?.endDate);
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
            clearStoredFilter();
            refreshDashboardLinks(null);
            render();
            const clearUrl = form.getAttribute("data-clear-url") || window.location.pathname;
            window.location.href = clearUrl;
        });

        form.addEventListener("submit", function (event) {
            if (!isValid()) {
                event.preventDefault();
                return;
            }

            syncHiddenDates();
            persistCurrentSelection();
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

            if (isValid()) {
                persistCurrentSelection();
            } else {
                refreshDashboardLinks(getStoredFilterState());
            }
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

        function persistCurrentSelection() {
            if (!isValid()) {
                return;
            }

            persistFilterState({
                startDate: toIsoDate(state.startDate),
                endDate: toIsoDate(state.endDate)
            });

            refreshDashboardLinks(getStoredFilterState());
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

    function getFilterStateFromLocation(locationLike) {
        const params = new URLSearchParams(locationLike.search);
        const startDate = params.get("startDate");
        const endDate = params.get("endDate");
        const normalizedPair = normalizeIsoPair(startDate, endDate);
        if (normalizedPair) {
            return normalizedPair;
        }

        const raporTarihi = normalizeIsoValue(params.get("raporTarihi"));
        if (raporTarihi) {
            return {
                startDate: raporTarihi,
                endDate: raporTarihi
            };
        }

        const legacyPair = normalizeIsoPair(
            params.get("baslangicTarihi"),
            params.get("bitisTarihi")
        );
        if (legacyPair) {
            return legacyPair;
        }

        const ay = Number(params.get("ay"));
        const yil = Number(params.get("yil"));
        if (Number.isInteger(ay) && ay >= 1 && ay <= 12 && Number.isInteger(yil) && yil >= 2000 && yil <= 2099) {
            return {
                startDate: toIsoDate(new Date(yil, ay - 1, 1)),
                endDate: toIsoDate(new Date(yil, ay, 0))
            };
        }

        return null;
    }

    function hasClearFlag(locationLike) {
        return new URLSearchParams(locationLike.search).get("clear") === "1";
    }

    function getStoredFilterState() {
        try {
            const raw = window.localStorage.getItem(filterStorageKey);
            if (!raw) {
                return null;
            }

            const parsed = JSON.parse(raw);
            return normalizeIsoPair(parsed?.startDate, parsed?.endDate);
        } catch (error) {
            return null;
        }
    }

    function persistFilterState(filterState) {
        const normalized = normalizeIsoPair(filterState?.startDate, filterState?.endDate);
        if (!normalized) {
            return;
        }

        try {
            window.localStorage.setItem(filterStorageKey, JSON.stringify(normalized));
        } catch (error) {
            // ignore storage errors
        }
    }

    function clearStoredFilter() {
        try {
            window.localStorage.removeItem(filterStorageKey);
        } catch (error) {
            // ignore storage errors
        }
    }

    function refreshDashboardLinks(filterState) {
        document.querySelectorAll("a[href]").forEach(function (anchor) {
            const currentHref = anchor.getAttribute("href");
            if (!currentHref || currentHref.startsWith("#") || currentHref.startsWith("javascript:")) {
                return;
            }

            const baseHref = anchor.dataset.baseHref || currentHref;
            if (!anchor.dataset.baseHref) {
                anchor.dataset.baseHref = baseHref;
            }

            if (!isDashboardHref(baseHref)) {
                return;
            }

            anchor.setAttribute("href", filterState ? buildUrlWithFilter(baseHref, filterState) : stripFilterFromUrl(baseHref));
        });
    }

    function isDashboardHref(href) {
        try {
            const url = new URL(href, window.location.origin);
            return isDashboardPath(url.pathname);
        } catch (error) {
            return false;
        }
    }

    function isDashboardPath(pathname) {
        return dashboardPaths.has((pathname || "").toLowerCase());
    }

    function buildUrlWithFilter(href, filterState) {
        const url = new URL(href, window.location.origin);
        stripFilterParams(url.searchParams);
        url.searchParams.set("startDate", filterState.startDate);
        url.searchParams.set("endDate", filterState.endDate);
        return url.pathname + url.search + url.hash;
    }

    function stripFilterFromUrl(href) {
        const url = new URL(href, window.location.origin);
        stripFilterParams(url.searchParams);
        return url.pathname + url.search + url.hash;
    }

    function stripFilterParams(searchParams) {
        [
            "startDate",
            "endDate",
            "raporTarihi",
            "baslangicTarihi",
            "bitisTarihi",
            "ay",
            "yil",
            "clear"
        ].forEach(function (key) {
            searchParams.delete(key);
        });
    }

    function normalizeIsoPair(startDate, endDate) {
        const normalizedStart = normalizeIsoValue(startDate);
        const normalizedEnd = normalizeIsoValue(endDate);

        if (!normalizedStart && !normalizedEnd) {
            return null;
        }

        const start = parseIsoDate(normalizedStart || normalizedEnd);
        const end = parseIsoDate(normalizedEnd || normalizedStart);
        if (!start || !end) {
            return null;
        }

        if (end < start) {
            return {
                startDate: toIsoDate(end),
                endDate: toIsoDate(start)
            };
        }

        return {
            startDate: toIsoDate(start),
            endDate: toIsoDate(end)
        };
    }

    function normalizeIsoValue(value) {
        const parsed = parseIsoDate(value || "");
        return parsed ? toIsoDate(parsed) : null;
    }

});
