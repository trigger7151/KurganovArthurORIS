// Админка: WYSIWYG-редактор (Quill), загрузка арта и проверка названия через AJAX,
// подтверждение удаления.
(function () {
    "use strict";

    // ---------- Подтверждение удаления ----------
    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (!window.confirm(form.getAttribute("data-confirm"))) event.preventDefault();
        });
    });

    var form = document.querySelector("form[data-cone-form]");
    if (!form) return;

    // ---------- WYSIWYG-редактор описания ----------
    var editorHost = form.querySelector("[data-editor]");
    var editorInput = form.querySelector("[data-editor-input]");
    if (editorHost && editorInput && window.Quill) {
        var quill = new window.Quill(editorHost, {
            theme: "snow",
            placeholder: "Описание светового конуса…",
            modules: {
                toolbar: [
                    ["bold", "italic", "underline", "strike"],
                    [{ header: 3 }, "blockquote"],
                    [{ list: "ordered" }, { list: "bullet" }],
                    ["link", "clean"]
                ]
            }
        });
        if (editorInput.value) quill.clipboard.dangerouslyPasteHTML(editorInput.value);

        // Перед отправкой (и до проверки формы) переносим HTML редактора в скрытое поле.
        form.addEventListener("submit", function () {
            // getSemanticHTML превращает пробелы в &nbsp; — возвращаем обычные, чтобы текст переносился.
            editorInput.value = quill.getText().trim() ? quill.getSemanticHTML().replace(/&nbsp;/g, " ") : "";
        }, true);
    }

    // ---------- Загрузка арта ----------
    var imageInput = form.elements.imageUrl;
    var fileInput = form.querySelector("[data-upload]");
    var preview = form.querySelector("[data-art-preview]");
    var uploadStatus = form.querySelector("[data-upload-status]");

    function showPreview(url) {
        preview.replaceChildren();
        if (url) {
            var img = document.createElement("img");
            img.alt = "";
            img.src = url;
            preview.appendChild(img);
        } else {
            var span = document.createElement("span");
            span.className = "muted small";
            span.textContent = "Нет арта";
            preview.appendChild(span);
        }
    }

    if (fileInput) {
        fileInput.addEventListener("change", function () {
            var file = fileInput.files[0];
            if (!file) return;
            if (["image/png", "image/jpeg", "image/webp"].indexOf(file.type) < 0) {
                uploadStatus.textContent = "Поддерживаются только PNG, JPEG и WebP.";
                return;
            }
            if (file.size > 5 * 1024 * 1024) {
                uploadStatus.textContent = "Файл больше 5 МБ.";
                return;
            }
            uploadStatus.textContent = "Загрузка…";
            fetch("/admin/upload", { method: "POST", headers: { "Content-Type": file.type }, body: file })
                .then(function (response) {
                    if (response.status === 401) throw new Error("Сеанс истёк — войдите снова.");
                    return response.json();
                })
                .then(function (data) {
                    if (!data.success) throw new Error(data.message || "Не удалось загрузить файл.");
                    imageInput.value = data.url;
                    window.AdminValidation.setError(imageInput, "");
                    showPreview(data.url);
                    uploadStatus.textContent = "Файл загружен. Не забудьте сохранить конус.";
                })
                .catch(function (error) { uploadStatus.textContent = error.message; })
                .finally(function () { fileInput.value = ""; });
        });
    }

    var clearButton = form.querySelector("[data-clear-image]");
    if (clearButton) {
        clearButton.addEventListener("click", function () {
            imageInput.value = "";
            showPreview("");
            uploadStatus.textContent = "";
        });
    }

    if (imageInput) {
        imageInput.addEventListener("change", function () {
            if (window.AdminValidation.rules.imageUrl.pattern.test(imageInput.value.trim()))
                showPreview(imageInput.value.trim());
        });
    }

    // ---------- Проверка уникальности названия (AJAX) ----------
    var nameInput = form.querySelector("[data-name-check]");
    var nameStatus = form.querySelector("[data-name-status]");
    var nameTimer = 0;
    var initialName = nameInput ? nameInput.value.trim() : "";

    if (nameInput && nameStatus) {
        nameInput.addEventListener("input", function () {
            clearTimeout(nameTimer);
            nameStatus.textContent = "";
            nameStatus.className = "field__hint";
            var name = nameInput.value.trim();
            if (name.length < 2 || name === initialName) return;
            nameTimer = setTimeout(function () {
                var params = new URLSearchParams({ name: name, id: form.elements.id.value });
                fetch("/api/cones/name-available?" + params.toString())
                    .then(function (response) { return response.json(); })
                    .then(function (data) {
                        if (nameInput.value.trim() !== name) return;
                        nameStatus.textContent = data.available ? "Название свободно" : "Такое название уже занято";
                        nameStatus.className = "field__hint " + (data.available ? "is-ok" : "is-bad");
                    })
                    .catch(function () { /* проверка необязательна: сервер всё равно проверит при сохранении */ });
            }, 400);
        });
    }

    // ---------- Скопировать текст S1 во все уровни ----------
    var copyButton = form.querySelector("[data-copy-s1]");
    if (copyButton) {
        copyButton.addEventListener("click", function () {
            var source = form.elements.s1.value;
            for (var level = 2; level <= 5; level++) {
                var target = form.elements["s" + level];
                if (!target.value.trim()) {
                    target.value = source;
                    target.dispatchEvent(new Event("input"));
                }
            }
        });
    }
})();
