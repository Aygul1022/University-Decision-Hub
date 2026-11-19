// Instructor list (instructors.html) and instructor detail (instructor-detail.html?id=<InstructorId>).
// Requires js/shared/api.js, js/shared/departments.js and js/shared/navbar.js.
(function () {
  const main = document.querySelector("main[data-page]");
  if (!main) return;

  const status = main.querySelector("[data-status]");

  function showStatus(text, isError) {
    status.textContent = text;
    status.className = "notice" + (isError ? " error" : "");
    status.hidden = !text;
  }

  let departmentNames = new Map();
  const compareNames = new Intl.Collator("tr");

  function labelKey(name) {
    return name.normalize("NFC").trim().replace(/\s+/g, " ").toLocaleLowerCase("tr");
  }

  // Official catalogue aliases for staff search. Keep the underlying IDs for every match.
  const aliases = new Map(Object.entries({
    "Bilgisayar Mühendisliği": "Computer Engineering",
    "Bilgisayar Mühendisliği (İngilizce)": "Computer Engineering",
    "İktisat (İngilizce)": "Economics",
    "İktisat (Türkçe)": "İktisat",
    "Psikoloji (İngilizce)": "Psychology",
    "Psikoloji (Türkçe)": "Psikoloji",
    "Endüstri Mühendisliği (İngilizce)": "Industrial Engineering",
    "Endüstri Mühendisliği (Türkçe)": "Endüstri Mühendisliği",
    "Makine Mühendisliği (İngilizce)": "Mechanical Engineering",
    "Makine Mühendisliği (Türkçe)": "Makine Mühendisliği",
    "Grafik (İngilizce)": "Graphics",
    "Grafik Bölümü (İngilizce)": "Graphics",
    "İşletme (Türkçe)": "İşletme",
    "İşletme (İngilizce)": "Business Administration",
    "Dış Ticaret (Türkçe)": "Dış Ticaret",
    "Dış Ticaret (İngilizce)": "Foreign Trade",
    "Elektrik- Elektronik Mühendisliği": "Elektrik-Elektronik Mühendisliği",
    "Elektronik Teknolojisi Programı": "Elektronik Teknolojisi",
    "Halkla İlişkiler Tanıtım": "Halkla İlişkiler ve Tanıtım",
    "Kurumsal Bilişim Uzmanlığı Pr.": "Kurumsal Bilişim Uzmanlığı",
    "Mahkeme Büro Hizmetleri (Adalet)": "Mahkeme Büro Hizmetleri",
    "Robotik ve Yapay Zeka": "Robotik ve Yapay Zekâ",
    "Turizm ve Otel İşletmeci Programı": "Turizm ve Otel İşletmeciliği",
    "Çocuk Gelişimi Programı": "Çocuk Gelişimi",
    "İnternet Ağ ve Teknolojileri": "İnternet ve Ağ Teknolojileri",
    "Görsel İletişim Tasarım": "Görsel İletişim Tasarımı",
  }).map(([name, label]) => [labelKey(name), label]));
  const sharedUnits = new Map([
    [labelKey("İktisat"), ["İktisat", "Economics"]],
    [labelKey("Makine Mühendisliği"), ["Makine Mühendisliği", "Mechanical Engineering"]],
    [labelKey("İşletme"), ["İşletme", "Business Administration"]],
  ]);

  async function loadDepartmentNames() {
    const departments = await Departments.load();
    const internationalRelationsFaculties = new Set(departments
      .filter(({ departmentName }) => labelKey(departmentName) === labelKey("International Relations"))
      .map(({ facultyId }) => facultyId));
    const entries = departments.map(({ departmentId, departmentName, facultyId }) => {
      const key = labelKey(departmentName);
      // A combined faculty staff record belongs in both language filters.
      const shared = key.match(/^(.+) \((?:türkçe\s*(?:\/|-)\s*ingilizce|ingilizce\s*(?:\/|-)\s*türkçe)\)$/u);
      let labels = shared ? sharedUnits.get(shared[1]) : undefined;
      // Keep the institute's separate Turkish graduate programme.
      if (key === labelKey("Uluslararası İlişkiler") && internationalRelationsFaculties.has(facultyId)) {
        labels = ["International Relations"];
      }
      return { departmentId, labels: labels || [aliases.get(key) || departmentName] };
    });
    departmentNames = new Map(entries.map(({ departmentId, labels }) => [departmentId, labels.join(" · ")]));
    return entries;
  }

  function departmentLabel(instructor) {
    return departmentNames.get(instructor.departmentId) || "";
  }

  // ---------------------------------------------------------------- list page
  async function initList() {
    const form = main.querySelector("[data-search-form]");
    const results = main.querySelector("[data-results]");
    const count = main.querySelector("[data-result-count]");
    const departmentSelect = form.elements.departmentId;
    const departmentStatus = main.querySelector("[data-department-status]");
    let latestRequest = 0;

    const departmentsByLabel = new Map();
    for (const { departmentId, labels } of await loadDepartmentNames()) {
      for (const label of labels) {
        const key = labelKey(label);
        if (!departmentsByLabel.has(key)) departmentsByLabel.set(key, { label, ids: new Set() });
        departmentsByLabel.get(key).ids.add(departmentId);
      }
    }
    const options = [...departmentsByLabel].sort(([, a], [, b]) => compareNames.compare(a.label, b.label));
    for (const [key, { label }] of options) {
      const option = document.createElement("option");
      option.value = key;
      option.textContent = label;
      departmentSelect.appendChild(option);
    }
    if (options.length === 0) {
      departmentSelect.disabled = true;
      departmentStatus.textContent = "Department filtering is unavailable. Search by name instead.";
      departmentStatus.hidden = false;
    }

    function renderList(instructors) {
      results.replaceChildren();
      for (const instructor of instructors) {
        const link = document.createElement("a");
        link.href = "instructor-detail.html?id=" + encodeURIComponent(instructor.instructorId);

        const name = document.createElement("span");
        name.className = "instructor-name";
        name.textContent = instructor.fullName;
        link.appendChild(name);

        const meta = document.createElement("span");
        meta.className = "instructor-meta";
        meta.textContent = [instructor.academicTitle, departmentLabel(instructor)].filter(Boolean).join(" · ");
        link.appendChild(meta);

        const item = document.createElement("li");
        item.appendChild(link);
        results.appendChild(item);
      }
    }

    async function search() {
      const query = form.elements.query.value.trim();
      const selected = departmentsByLabel.get(departmentSelect.value);
      const filtered = Boolean(query || selected);

      const request = ++latestRequest;
      count.hidden = true;
      showStatus("Loading academic staff…", false);

      try {
        const ids = selected ? [...selected.ids] : [null];
        const matches = await Promise.all(ids.map((id) => {
          const params = new URLSearchParams();
          if (query) params.set("query", query);
          if (id !== null) params.set("departmentId", id);
          return apiFetch("/api/instructors" + (filtered ? "?" + params : ""));
        }));
        if (request !== latestRequest) return; // a newer search has started
        const instructors = matches.length === 1 ? matches[0] : [...new Map(matches.flat()
          .map((instructor) => [instructor.instructorId, instructor])).values()]
          .sort((a, b) => compareNames.compare(a.fullName, b.fullName) || a.instructorId - b.instructorId);
        renderList(instructors);

        if (instructors.length === 0) {
          showStatus(filtered
            ? "No academic staff members match this search. Try a shorter name or clear the department filter."
            : "No academic staff members have been added yet.", false);
        } else {
          showStatus("", false);
          count.textContent = instructors.length === 1
            ? "1 academic staff member"
            : instructors.length + " academic staff members";
          count.hidden = false;
        }
      } catch (error) {
        if (request !== latestRequest) return;
        results.replaceChildren();
        const fieldMessages = Object.values(error.fieldErrors || {}).flat().join(" ");
        showStatus(fieldMessages || error.message, true);
      }
    }

    form.addEventListener("submit", (event) => {
      event.preventDefault();
      search();
    });
    search();
  }

  // -------------------------------------------------------------- detail page
  function initDetail() {
    const article = main.querySelector("[data-instructor]");

    function setField(name, value) {
      const element = article.querySelector('[data-field="' + name + '"]');
      element.textContent = value || "";
      element.hidden = !value;
    }

    function render(instructor) {
      document.title = instructor.fullName + " · University Decision Hub";
      setField("academicTitle", instructor.academicTitle);
      setField("fullName", instructor.fullName);
      setField("department", departmentLabel(instructor));

      // Link to the Read Reviews page, filtered to this instructor. Visitors go through login first (FR01).
      const reviewsPath = "Pages/reviews/reviews.html?instructorId=" + encodeURIComponent(instructor.instructorId);
      const link = article.querySelector("[data-reviews-link]");
      link.href = Site.membersUrl(reviewsPath);
      link.textContent = Session.isLoggedIn ? "Read reviews of their courses" : "Log in to read reviews";
      article.querySelector("[data-reviews-text]").textContent = Session.isLoggedIn
        ? "Reviews are written about courses. These are the reviews of courses taught by " + instructor.fullName + "."
        : "Reviews are written about courses and are available to registered students. Log in to see the reviews of courses taught by " + instructor.fullName + ".";

      showStatus("", false);
      article.hidden = false;
    }

    async function load() {
      const id = Number(new URLSearchParams(window.location.search).get("id"));
      if (!Number.isInteger(id) || id <= 0) {
        showStatus("No academic staff member was selected. Choose one from the academic staff list.", true);
        return;
      }

      try {
        const instructor = await apiFetch("/api/instructors/" + id);
        await loadDepartmentNames();
        render(instructor);
      } catch (error) {
        showStatus(error.status === 404 ? "This academic staff member could not be found." : error.message, true);
      }
    }

    load();
  }

  if (main.dataset.page === "instructor-list") initList();
  if (main.dataset.page === "instructor-detail") initDetail();
})();
