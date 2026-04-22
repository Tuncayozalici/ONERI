(function () {
    const preview = document.getElementById("gunlukRolePreview");
    if (!preview) {
        return;
    }

    const dashboardPermission = preview.dataset.dashboardPermission;
    const permissionInputs = Array.from(document.querySelectorAll(".js-role-permission[data-permission-key]"));
    const previewWidgets = Array.from(preview.querySelectorAll("[data-preview-widget][data-permission-key]"));
    const blockedState = preview.querySelector("[data-preview-blocked]");
    const screenState = preview.querySelector("[data-preview-screen]");
    const emptyState = preview.querySelector("[data-preview-empty]");
    const visibleCount = preview.querySelector("[data-preview-visible-count]");
    const accessState = preview.querySelector("[data-preview-access-state]");

    function hasPermission(permissionKey) {
        return permissionInputs.some(function (input) {
            return input.dataset.permissionKey === permissionKey && input.checked;
        });
    }

    function setHidden(element, hidden) {
        if (!element) {
            return;
        }

        element.hidden = hidden;
    }

    function refreshPreview() {
        const hasDashboardAccess = hasPermission(dashboardPermission);
        let shownWidgetCount = 0;

        setHidden(blockedState, hasDashboardAccess);
        setHidden(screenState, !hasDashboardAccess);
        preview.classList.toggle("is-blocked", !hasDashboardAccess);
        preview.classList.toggle("is-open", hasDashboardAccess);

        previewWidgets.forEach(function (widget) {
            const shouldShow = hasDashboardAccess && hasPermission(widget.dataset.permissionKey);
            widget.hidden = !shouldShow;
            widget.classList.toggle("is-visible", shouldShow);

            if (shouldShow) {
                shownWidgetCount += 1;
            }
        });

        setHidden(emptyState, !hasDashboardAccess || shownWidgetCount > 0);

        if (visibleCount) {
            visibleCount.textContent = shownWidgetCount.toString();
        }

        if (accessState) {
            accessState.textContent = hasDashboardAccess ? "Günlük Veriler açık" : "Günlük Veriler kapalı";
        }
    }

    permissionInputs.forEach(function (input) {
        input.addEventListener("change", refreshPreview);
    });

    refreshPreview();
})();
