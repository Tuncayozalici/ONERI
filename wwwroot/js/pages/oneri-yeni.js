document.addEventListener("DOMContentLoaded", function () {
    var anaBolumSelect = document.getElementById("anaBolum");
    var altBolumKapsayici = document.getElementById("altBolumKapsayici");
    var altBolumSelect = document.getElementById("altBolum");

    if (!anaBolumSelect || !altBolumKapsayici || !altBolumSelect) {
        return;
    }

    var altBolumler = {
        Panel: ["CNC", "PVC", "Ebatlama", "Keson"],
        Metal: ["Boya", "Metal Imalat"]
    };

    anaBolumSelect.addEventListener("change", function () {
        var secilenBolum = anaBolumSelect.value;
        altBolumSelect.innerHTML = "";
        altBolumKapsayici.classList.remove("is-visible");
        altBolumSelect.value = "";

        if (!Object.prototype.hasOwnProperty.call(altBolumler, secilenBolum)) {
            return;
        }

        altBolumKapsayici.classList.add("is-visible");
        altBolumSelect.innerHTML = "<option value=\"\">Lütfen alt birim seçiniz...</option>";

        altBolumler[secilenBolum].forEach(function (birim) {
            var option = document.createElement("option");
            option.value = birim.replace(/\s+/g, "");
            option.textContent = birim;
            altBolumSelect.appendChild(option);
        });
    });
});
