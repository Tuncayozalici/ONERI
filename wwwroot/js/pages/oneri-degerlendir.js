document.addEventListener("DOMContentLoaded", function () {
    var scoreInputs = document.querySelectorAll(".score-input");
    var totalScoreEl = document.getElementById("total-score");
    var decisionEl = document.getElementById("decision");

    if (!totalScoreEl || !decisionEl || scoreInputs.length === 0) {
        return;
    }

    function calculateTotal() {
        var total = 0;
        scoreInputs.forEach(function (input) {
            var value = parseInt(input.value, 10);
            if (!isNaN(value)) {
                total += value;
            }
        });

        totalScoreEl.value = String(total);
        updateDecision(total);
    }

    function updateDecision(total) {
        if (total >= 60) {
            decisionEl.textContent = "KABUL";
            decisionEl.className = "fw-bold mt-2 fs-5 text-success";
            totalScoreEl.className = "form-control form-control-lg text-center display-4 fw-bold p-0 total-score-input text-success";
            return;
        }

        decisionEl.textContent = "RED";
        decisionEl.className = "fw-bold mt-2 fs-5 text-danger";
        totalScoreEl.className = "form-control form-control-lg text-center display-4 fw-bold p-0 total-score-input text-danger";
    }

    scoreInputs.forEach(function (input) {
        input.addEventListener("input", calculateTotal);
    });

    calculateTotal();
});
