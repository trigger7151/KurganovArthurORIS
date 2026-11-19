// Каталог без перезагрузки: фильтры, поиск и сортировка отправляют AJAX-запрос
// на /api/cones и перерисовывают сетку. Без JavaScript страница работает
// через обычные ссылки и форму — сервер рендерит тот же результат.
(function () {
    "use strict";

    var form = document.getElementById("catalog-filters");
    var grid = document.getElementById("cone-grid");
    var empty = document.getElementById("empty-state");
    if (!form || !grid) return;

    var searchInput = form.querySelector("input[name=q]");
    var rarityInput = form.querySelector("input[name=rarity]");
    var pathInput = form.querySelector("input[name=path]");
    var sortSelects = form.querySelectorAll("select[data-sort]");
    var countLong = form.querySelector("[data-count-long]");
    var countShort = form.querySelector("[data-count-short]");
    var resetLink = form.querySelector(".filters__reset");
    var sortLabel = form.querySelector("[data-sort-label]");
    var sortLabelShort = form.querySelector("[data-sort-label-short]");
    var mobile = window.matchMedia("(max-width: 640px)");
    var controller = null;
    var debounceTimer = 0;

    // Мобильная версия: короткий плейсхолдер, как в макете.
    function updatePlaceholder() {
        searchInput.placeholder = mobile.matches
            ? searchInput.getAttribute("data-short-placeholder")
            : "Найти световой конус по названию";
    }
    updatePlaceholder();
    if (mobile.addEventListener) mobile.addEventListener("change", updatePlaceholder);

    sortSelects.forEach(function (select) { select.disabled = false; select.removeAttribute("name"); });

    function currentSort() { return sortSelects.length ? sortSelects[0].value : "rarity"; }

    function buildParams() {
        var params = new URLSearchParams();
        var q = searchInput.value.trim();
        if (q) params.set("q", q);
        if (rarityInput.value) params.set("rarity", rarityInput.value);
        if (pathInput.value) params.set("path", pathInput.value);
        if (currentSort() !== "rarity") params.set("sort", currentSort());
        return params;
    }

    function setActiveChip(group, value) {
        form.querySelectorAll('[data-filter="' + group + '"] .chip').forEach(function (chip) {
            chip.classList.toggle("chip--active", chip.getAttribute("data-value") === value);
        });
    }

    function syncSortLabels() {
        var option = sortSelects[0] && sortSelects[0].selectedOptions[0];
        if (!option) return;
        if (sortLabel) sortLabel.textContent = option.getAttribute("data-long") || option.textContent;
        if (sortLabelShort) sortLabelShort.textContent = option.getAttribute("data-short") || option.textContent;
    }

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    // Карточка собирается через DOM API (textContent), поэтому данные не могут внедрить разметку.
    function renderCard(cone) {
        var card = el("a", "cone-card rarity-" + cone.rarity);
        card.href = cone.url;

        var art = el("div", "cone-card__art");
        if (cone.hasImage) {
            var img = el("img", "cone-card__img");
            img.src = cone.imageUrl;
            img.alt = cone.name;
            img.loading = "lazy";
            art.appendChild(img);
        } else {
            var icon = el("img");
            icon.src = "/icons/card-image.svg";
            icon.width = 28;
            icon.height = 28;
            icon.alt = "";
            art.appendChild(icon);
            art.appendChild(el("span", "art-size", "902 × 1260"));
        }

        var info = el("div", "cone-card__info");
        info.appendChild(el("p", "cone-card__name", cone.name));
        var meta = el("p", "cone-card__meta");
        meta.appendChild(el("span", "stars", cone.stars));
        meta.appendChild(el("span", "", cone.pathName));
        info.appendChild(meta);

        card.appendChild(art);
        card.appendChild(el("div", "cone-card__bar"));
        card.appendChild(info);
        return card;
    }

    function render(data) {
        var fragment = document.createDocumentFragment();
        data.items.forEach(function (cone) { fragment.appendChild(renderCard(cone)); });
        grid.replaceChildren(fragment);
        countLong.textContent = data.countText;
        countShort.textContent = data.countShort;
        empty.classList.toggle("is-hidden", data.items.length > 0);
        if (resetLink) resetLink.classList.toggle("is-hidden", !data.isFiltered);
    }

    function load() {
        var params = buildParams();
        var query = params.toString();
        history.replaceState(null, "", query ? "/?" + query : "/");

        if (controller) controller.abort();
        controller = new AbortController();
        grid.classList.add("is-loading");

        fetch("/api/cones" + (query ? "?" + query : ""), {
            headers: { "Accept": "application/json" },
            signal: controller.signal
        })
            .then(function (response) {
                if (!response.ok) throw new Error("HTTP " + response.status);
                return response.json();
            })
            .then(function (data) {
                if (!data.success) throw new Error(data.message || "Ошибка");
                render(data);
            })
            .catch(function (error) {
                if (error.name === "AbortError") return;
                countLong.textContent = "Не удалось загрузить каталог. Проверьте соединение и попробуйте ещё раз.";
                countShort.textContent = "Ошибка загрузки";
            })
            .finally(function () { grid.classList.remove("is-loading"); });
    }

    form.addEventListener("click", function (event) {
        var chip = event.target.closest(".chip");
        if (chip) {
            event.preventDefault();
            var group = chip.parentElement.getAttribute("data-filter");
            var value = chip.getAttribute("data-value");
            (group === "rarity" ? rarityInput : pathInput).value = value;
            setActiveChip(group, value);
            load();
        }
    });

    document.querySelectorAll("[data-reset]").forEach(function (link) {
        link.addEventListener("click", function (event) {
            event.preventDefault();
            searchInput.value = "";
            rarityInput.value = "";
            pathInput.value = "";
            setActiveChip("rarity", "");
            setActiveChip("path", "");
            load();
        });
    });

    searchInput.addEventListener("input", function () {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(load, 300);
    });

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        clearTimeout(debounceTimer);
        load();
    });

    sortSelects.forEach(function (select) {
        select.addEventListener("change", function () {
            sortSelects.forEach(function (other) { other.value = select.value; });
            syncSortLabels();
            load();
        });
    });
})();
