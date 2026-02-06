(function () {
    try {
        var stored = localStorage.getItem("theme");
        var prefersDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
        var theme = stored || (prefersDark ? "dark" : "light");
        document.documentElement.setAttribute("data-theme", theme);
        document.documentElement.setAttribute("data-bs-theme", theme);
    } catch (e) {
        document.documentElement.setAttribute("data-theme", "light");
        document.documentElement.setAttribute("data-bs-theme", "light");
    }
})();
