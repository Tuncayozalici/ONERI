document.addEventListener("DOMContentLoaded", function () {
    var dateElement = document.getElementById("current-date");
    if (!dateElement) {
        return;
    }

    var now = new Date();
    var options = { weekday: "long", year: "numeric", month: "long", day: "numeric" };
    dateElement.textContent = now.toLocaleDateString("tr-TR", options);
});
