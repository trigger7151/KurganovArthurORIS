// Страница конуса: переключение уровня (Ур. 1 / Ур. 80) и уровня наложения (S1–S5).
// Все значения уже пришли с сервером в data-атрибутах, запросы не нужны.
(function () {
    "use strict";

    var levelButtons = document.querySelectorAll("[data-level]");
    var statValues = {
        hp: document.querySelector('[data-stat="hp"]'),
        atk: document.querySelector('[data-stat="atk"]'),
        def: document.querySelector('[data-stat="def"]')
    };

    levelButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            levelButtons.forEach(function (other) {
                var active = other === button;
                other.classList.toggle("chip--active", active);
                other.setAttribute("aria-pressed", active ? "true" : "false");
            });
            Object.keys(statValues).forEach(function (key) {
                if (statValues[key]) statValues[key].textContent = button.getAttribute("data-" + key);
            });
        });
    });

    var tabs = document.querySelectorAll("[data-superimposition]");
    var effectText = document.querySelector("[data-effect-text]");

    tabs.forEach(function (tab, index) {
        tab.addEventListener("click", function () { select(tab); });
        tab.addEventListener("keydown", function (event) {
            var next = null;
            if (event.key === "ArrowRight") next = tabs[(index + 1) % tabs.length];
            if (event.key === "ArrowLeft") next = tabs[(index - 1 + tabs.length) % tabs.length];
            if (next) { event.preventDefault(); next.focus(); select(next); }
        });
    });

    function select(tab) {
        tabs.forEach(function (other) {
            var active = other === tab;
            other.classList.toggle("tab--active", active);
            other.setAttribute("aria-selected", active ? "true" : "false");
        });
        if (effectText) effectText.textContent = tab.getAttribute("data-text");
    }
})();
