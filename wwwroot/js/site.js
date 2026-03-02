// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    let themePickerId = 0;

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

    enhanceNativeDatePickers(document);

    document.addEventListener("click", function (event) {
        const button = event.target.closest(".js-theme-picker-open");
        if (!button) {
            return;
        }

        const targetId = button.getAttribute("data-target");
        if (!targetId) {
            return;
        }

        const pickerInput = document.getElementById(targetId);
        if (!pickerInput || pickerInput.disabled) {
            return;
        }

        pickerInput.focus();

        if (typeof pickerInput.showPicker === "function") {
            pickerInput.showPicker();
            return;
        }

        pickerInput.click();
    });

    function enhanceNativeDatePickers(root) {
        const inputs = root.querySelectorAll('input[type="date"], input[type="month"]');
        inputs.forEach(function (input) {
            if (input.dataset.pickerEnhanced === "true") {
                return;
            }

            if (input.closest(".theme-picker-field")) {
                input.dataset.pickerEnhanced = "true";
                return;
            }

            if (!input.id) {
                themePickerId += 1;
                input.id = "themePickerInput" + themePickerId;
            }

            const wrapper = document.createElement("div");
            wrapper.className = "theme-picker-field";

            const parent = input.parentNode;
            if (!parent) {
                return;
            }

            moveSpacingClasses(input, wrapper);
            parent.insertBefore(wrapper, input);
            wrapper.appendChild(input);

            const button = document.createElement("button");
            button.type = "button";
            button.className = "theme-picker-button js-theme-picker-open";
            button.setAttribute("data-target", input.id);
            button.setAttribute("aria-label", buildPickerAriaLabel(input));
            button.innerHTML = '<i class="bi bi-calendar3" aria-hidden="true"></i>';

            wrapper.appendChild(button);
            input.dataset.pickerEnhanced = "true";
        });
    }

    function buildPickerAriaLabel(input) {
        if (!input.id) {
            return "Tarih seçici aç";
        }

        const label = document.querySelector('label[for="' + input.id + '"]');
        if (!label) {
            return input.type === "month" ? "Ay seçici aç" : "Tarih seçici aç";
        }

        const labelText = (label.textContent || "").trim();
        if (!labelText) {
            return input.type === "month" ? "Ay seçici aç" : "Tarih seçici aç";
        }

        return labelText + " seçici aç";
    }

    function moveSpacingClasses(input, wrapper) {
        Array.from(input.classList).forEach(function (className) {
            if (/^m[trblxy]?-(auto|0|1|2|3|4|5)$/.test(className)) {
                wrapper.classList.add(className);
                input.classList.remove(className);
            }
        });
    }
});
