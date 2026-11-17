// Searchable select: turns a long <select> into a search box with a filtered list.
//
//   SearchableSelect.enhance(document.querySelector("#courseId"), { placeholder: "Search courses" });
//   SearchableSelect.refresh(select);   // after the options or the selected value were changed by code
//
// The <select> stays in the page (hidden) and remains the one source of truth: choosing an item sets
// select.value and fires "change", so code that reads form.elements.courseId.value keeps working.
// Without this script the page still shows the ordinary select. Load it before the page script.
const SearchableSelect = (function () {
  const MAX_SHOWN = 100;

  // Case-insensitive and Turkish-aware: "istatistik" finds "İstatistik" and "mikrobiyoloji" finds
  // "Mikrobiyoloji"; letters with diacritics also match their plain form (s for ş, g for ğ, ...).
  function normalize(text) {
    return String(text ?? "")
      .toLocaleLowerCase("tr")
      .normalize("NFD")
      .replace(/[̀-ͯ]/g, "")
      .replace(/ı/g, "i");
  }

  function create(tag, className, text) {
    const element = document.createElement(tag);
    if (className) element.className = className;
    if (text !== undefined) element.textContent = text;
    return element;
  }

  function enhance(select, options = {}) {
    if (select._searchableSelect) {
      select._searchableSelect.refresh();
      return select._searchableSelect;
    }

    const placeholder = options.placeholder || "Search…";
    const noun = options.noun || "items";
    const id = select.id || "select-" + Math.random().toString(36).slice(2, 8);

    const wrapper = create("div", "ss");
    const input = create("input", "ss-input");
    input.type = "text";
    input.id = id + "-search";
    input.placeholder = placeholder;
    input.autocomplete = "off";
    input.spellcheck = false;
    input.setAttribute("role", "combobox");
    input.setAttribute("aria-autocomplete", "list");
    input.setAttribute("aria-expanded", "false");
    input.setAttribute("aria-controls", id + "-list");

    const clear = create("button", "ss-clear", "×");
    clear.type = "button";
    clear.hidden = true;
    clear.setAttribute("aria-label", "Clear selection");

    const list = create("ul", "ss-list");
    list.id = id + "-list";
    list.setAttribute("role", "listbox");
    list.hidden = true;

    const status = create("p", "field-hint ss-status");
    status.setAttribute("role", "status");
    status.setAttribute("aria-live", "polite");

    wrapper.append(input, clear, list, status);
    select.after(wrapper);
    select.hidden = true;

    // A click on the field's label should reach the search box instead of the hidden select.
    const label = document.querySelector('label[for="' + select.id + '"]');
    if (label) label.htmlFor = input.id;

    let items = [];
    let shown = [];
    let active = -1;
    let open = false;

    function readItems() {
      items = [...select.options]
        .filter((option) => option.value !== "")
        .map((option) => ({ value: option.value, label: option.textContent, key: normalize(option.textContent) }));
    }

    function selectedItem() {
      return items.find((item) => item.value === select.value) || null;
    }

    function showSelectedText() {
      const item = selectedItem();
      input.value = item ? item.label : "";
      clear.hidden = !item;
    }

    function match(query) {
      const terms = normalize(query).split(/\s+/).filter(Boolean);
      return terms.length ? items.filter((item) => terms.every((term) => item.key.includes(term))) : items;
    }

    function setActive(index, scroll = true) {
      active = index;
      const rows = list.querySelectorAll(".ss-option");
      rows.forEach((row, position) => row.classList.toggle("is-active", position === index));
      const row = rows[index];
      if (row) {
        input.setAttribute("aria-activedescendant", row.id);
        if (scroll) row.scrollIntoView({ block: "nearest" });
      } else {
        input.removeAttribute("aria-activedescendant");
      }
    }

    function render(query) {
      const matches = match(query);
      shown = matches.slice(0, MAX_SHOWN);
      list.replaceChildren();

      shown.forEach((item, position) => {
        const row = create("li", "ss-option", item.label);
        row.id = list.id + "-" + position;
        row.setAttribute("role", "option");
        row.setAttribute("aria-selected", String(item.value === select.value));
        // mousedown, not click: the input must not lose focus (and close the list) before the choice is made.
        row.addEventListener("mousedown", (event) => {
          event.preventDefault();
          choose(item);
        });
        list.appendChild(row);
      });

      if (!matches.length) {
        list.appendChild(create("li", "ss-empty", "No " + noun + " match “" + query.trim() + "”."));
      } else if (matches.length > shown.length) {
        list.appendChild(create("li", "ss-more", "Showing the first " + shown.length + " of " + matches.length + ". Type more to narrow the list."));
      }

      const typed = query.trim();
      status.textContent = typed
        ? matches.length + " of " + items.length + " " + noun + " match"
        : items.length + " " + noun + " — type to search";
      setActive(shown.length ? 0 : -1);
    }

    function openList(query) {
      open = true;
      list.hidden = false;
      input.setAttribute("aria-expanded", "true");
      render(query);
    }

    function closeList() {
      open = false;
      list.hidden = true;
      input.setAttribute("aria-expanded", "false");
      input.removeAttribute("aria-activedescendant");
      showSelectedText();
      status.textContent = "";
    }

    function choose(item) {
      select.value = item.value;
      select.dispatchEvent(new Event("change", { bubbles: true }));
      closeList();
    }

    input.addEventListener("focus", () => {
      // The whole text is selected so typing replaces the chosen item; the list starts unfiltered.
      input.select();
      openList("");
    });

    input.addEventListener("click", () => {
      if (!open) openList("");
    });

    input.addEventListener("input", () => {
      if (!open) open = true;
      list.hidden = false;
      input.setAttribute("aria-expanded", "true");
      render(input.value);
    });

    input.addEventListener("keydown", (event) => {
      if (event.key === "ArrowDown" || event.key === "ArrowUp") {
        event.preventDefault();
        if (!open) {
          openList("");
          return;
        }
        if (!shown.length) return;
        const step = event.key === "ArrowDown" ? 1 : -1;
        setActive((active + step + shown.length) % shown.length);
      } else if (event.key === "Enter") {
        // Enter picks the highlighted item and never submits the form from this box.
        event.preventDefault();
        if (open && shown[active]) choose(shown[active]);
      } else if (event.key === "Escape") {
        if (open) {
          event.preventDefault();
          closeList();
        }
      } else if (event.key === "Tab") {
        if (open) closeList();
      }
    });

    input.addEventListener("blur", () => {
      if (open) closeList();
    });

    clear.addEventListener("click", () => {
      select.value = "";
      select.dispatchEvent(new Event("change", { bubbles: true }));
      showSelectedText();
      input.focus();
    });

    function refresh() {
      readItems();
      showSelectedText();
      if (open) render(input.value);
    }

    refresh();
    const api = { refresh };
    select._searchableSelect = api;
    return api;
  }

  function refresh(select) {
    if (select._searchableSelect) select._searchableSelect.refresh();
  }

  return { enhance, refresh, normalize };
})();
