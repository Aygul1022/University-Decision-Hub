// Requires js/shared/api.js.
(function () {
  const status = document.querySelector("[data-faculty-status]");
  const retry = document.querySelector("[data-faculty-retry]");
  const list = document.querySelector("[data-faculty-list]");

  function element(tag, className = "", text = "") {
    const node = document.createElement(tag);
    node.className = className;
    node.textContent = text;
    return node;
  }

  function renderFaculty(faculty, departments) {
    const item = element("li");
    const panel = element("details", "card faculty-panel");
    const summary = element("summary", "faculty-summary");
    const label = element("strong", "", faculty.facultyName);
    const count = departments.length;
    summary.append(label, element("span", "faculty-count", count + (count === 1 ? " department" : " departments")));
    panel.append(summary);

    if (count === 0) {
      panel.append(element("p", "faculty-empty", "No departments are listed for this faculty."));
    } else {
      const names = element("ul", "faculty-departments");
      for (const department of departments) {
        names.append(element("li", "", department.departmentName));
      }
      panel.append(names);
    }

    item.append(panel);
    return item;
  }

  async function load() {
    status.hidden = false;
    status.className = "notice";
    status.textContent = "Loading faculties and departments…";
    retry.hidden = true;
    list.hidden = true;

    try {
      const [faculties, departments] = await Promise.all([
        apiFetch("/api/faculties"),
        apiFetch("/api/departments"),
      ]);
      if (!Array.isArray(faculties) || !Array.isArray(departments)) {
        throw new Error("Invalid catalogue lists.");
      }

      if (faculties.length === 0) {
        status.textContent = "No faculties are currently listed.";
        return;
      }

      const departmentsByFaculty = new Map();
      for (const department of departments) {
        const group = departmentsByFaculty.get(department.facultyId) ?? [];
        group.push(department);
        departmentsByFaculty.set(department.facultyId, group);
      }
      for (const group of departmentsByFaculty.values()) {
        group.sort((a, b) => a.departmentName.localeCompare(b.departmentName));
      }
      const sortedFaculties = [...faculties].sort((a, b) => a.facultyName.localeCompare(b.facultyName));
      list.replaceChildren(...sortedFaculties.map(faculty =>
        renderFaculty(faculty, departmentsByFaculty.get(faculty.facultyId) ?? [])
      ));
      list.hidden = false;
      status.hidden = true;
    } catch {
      status.className = "notice error";
      status.textContent = "The faculty directory could not be loaded.";
      retry.hidden = false;
    }
  }

  retry.addEventListener("click", load);
  load();
})();
