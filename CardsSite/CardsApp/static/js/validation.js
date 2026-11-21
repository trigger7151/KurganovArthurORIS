// Клиентская валидация форм админки. Регулярные выражения совпадают
// с серверными (Services/Validation/ValidationRules.cs): сервер всё равно проверяет данные повторно.
(function () {
    "use strict";

    var rules = {
        login: {
            pattern: /^[A-Za-z0-9_]{3,20}$/,
            message: "Логин: латинские буквы, цифры и _, от 3 до 20 символов."
        },
        title: {
            pattern: /^[A-Za-zА-Яа-яЁё0-9][A-Za-zА-Яа-яЁё0-9 ,.!?'«»"\-—:]*$/,
            message: "Можно использовать буквы, цифры, пробел и знаки , . ! ? ' « » \" - — :"
        },
        imageUrl: {
            pattern: /^(\/uploads\/[A-Za-z0-9_\-]+\.(png|jpe?g|webp)|https?:\/\/[^\s"'<>]+)$/i,
            message: "Укажите адрес вида https://... или загрузите файл."
        },
        slug: {
            pattern: /^[a-z]{3,20}$/,
            message: "Ключ: только строчные латинские буквы, например destruction."
        },
        pathName: {
            pattern: /^[A-Za-zА-Яа-яЁё][A-Za-zА-Яа-яЁё \-]{1,39}$/,
            message: "Название пути: только буквы, пробел и дефис."
        },
        stat: {
            pattern: /^\d{1,5}$/,
            message: "Введите целое число (до 5 цифр)."
        }
    };

    function fieldOf(control) { return control.closest(".field"); }

    function setError(control, message) {
        var field = fieldOf(control);
        if (!field) return;
        var box = field.querySelector("[data-error]");
        field.classList.toggle("field--error", !!message);
        if (box) box.textContent = message || "";
        control.setAttribute("aria-invalid", message ? "true" : "false");
    }

    function validate(control) {
        var label = control.getAttribute("data-label") || control.name;
        var value = (control.value || "").trim();
        var min = parseInt(control.getAttribute("data-min") || "0", 10);
        var max = parseInt(control.getAttribute("maxlength") || "0", 10);
        var rule = rules[control.getAttribute("data-rule")];

        if (!value) {
            return control.required ? (control.tagName === "SELECT"
                ? "Выберите значение в поле «" + label + "»."
                : "Заполните поле «" + label + "».") : "";
        }
        if (min && value.length < min) return "«" + label + "»: не короче " + min + " символов.";
        if (max && value.length > max) return "«" + label + "»: не длиннее " + max + " символов.";
        if (rule && !rule.pattern.test(value)) return rule.message;

        // Характеристика на Ур. 80 не может быть меньше, чем на Ур. 1.
        var match = /^(hp|atk|def)80$/.exec(control.name);
        if (match) {
            var base = control.form.elements[match[1] + "1"];
            if (base && rules.stat.pattern.test(base.value.trim()) && parseInt(value, 10) < parseInt(base.value, 10))
                return "На Ур. 80 значение не может быть меньше, чем на Ур. 1.";
        }
        return "";
    }

    function controlsOf(form) {
        return Array.prototype.filter.call(form.elements, function (c) {
            return c.hasAttribute("data-label") && !c.disabled;
        });
    }

    // Счётчик символов с ограничением длины (data-counter + maxlength).
    function attachCounter(control) {
        var max = parseInt(control.getAttribute("maxlength") || "0", 10);
        if (!max) return;
        var counter = document.createElement("span");
        counter.className = "char-counter";
        control.insertAdjacentElement("afterend", counter);
        function update() {
            var length = control.value.length;
            counter.textContent = length + " / " + max;
            counter.classList.toggle("is-limit", length >= max);
        }
        control.addEventListener("input", update);
        update();
    }

    document.querySelectorAll("form[data-validate]").forEach(function (form) {
        var controls = controlsOf(form);

        form.querySelectorAll("[data-counter]").forEach(attachCounter);

        controls.forEach(function (control) {
            control.addEventListener("blur", function () {
                if (control.value) setError(control, validate(control));
            });
            control.addEventListener("input", function () {
                if (fieldOf(control) && fieldOf(control).classList.contains("field--error"))
                    setError(control, validate(control));
            });
            control.addEventListener("change", function () { setError(control, validate(control)); });
        });

        form.addEventListener("submit", function (event) {
            var firstInvalid = null;
            controlsOf(form).forEach(function (control) {
                var message = validate(control);
                setError(control, message);
                if (message && !firstInvalid) firstInvalid = control;
            });
            if (firstInvalid) {
                event.preventDefault();
                firstInvalid.focus();
                firstInvalid.scrollIntoView({ block: "center", behavior: "smooth" });
            }
        });
    });

    window.AdminValidation = { setError: setError, rules: rules };
})();
