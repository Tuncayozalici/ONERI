// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    const openBtn = document.querySelector(".openbtn");
    const closeBtn = document.querySelector(".closebtn");
    const sidebar = document.getElementById("mySidebar");
    const main = document.getElementById("main");

    if (openBtn && closeBtn && sidebar && main) {
        openBtn.addEventListener("click", openNav);
        closeBtn.addEventListener("click", closeNav);
    }

    /* Set the width of the sidebar to 250px and the left margin of the page content to 250px */
    function openNav() {
        sidebar.style.width = "250px";
        main.style.marginLeft = "250px";
    }

    /* Set the width of the sidebar to 0 and the left margin of the page content to 0 */
    function closeNav() {
        sidebar.style.width = "0";
        main.style.marginLeft = "0";
    }

    const themeToggle = document.getElementById("themeToggle");
    if (themeToggle) {
        const currentTheme = document.documentElement.getAttribute("data-theme") || "light";
        themeToggle.checked = currentTheme === "dark";

        themeToggle.addEventListener("change", function () {
            const nextTheme = themeToggle.checked ? "dark" : "light";
            document.documentElement.setAttribute("data-theme", nextTheme);
            document.documentElement.setAttribute("data-bs-theme", nextTheme);
            try {
                localStorage.setItem("theme", nextTheme);
            } catch (e) {
                // ignore storage errors
            }
        });
    }

    document.querySelectorAll("select[data-auto-submit]").forEach(function (select) {
        select.addEventListener("change", function () {
            var form = select.closest("form");
            if (form) {
                form.submit();
            }
        });
    });

    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            var message = form.getAttribute("data-confirm");
            if (message && !window.confirm(message)) {
                event.preventDefault();
            }
        });
    });
});
