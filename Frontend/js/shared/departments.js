// Department names for every page. Load after js/shared/api.js:
//   <script src="../../js/shared/departments.js"></script>
//
// Reads GET /api/departments once per page. The pages never keep their own copy of the list.
//   await Departments.label(3)                 -> "Computer Engineering" ("Department 3" if it cannot be read)
//   await Departments.fillSelect(select, id)   -> adds one <option> per department after the options
//                                                 already in the HTML, then selects `id` when given
const Departments = (function () {
  let request = null;

  // Resolves with [{ departmentId, facultyId, departmentCode, departmentName }], or [] when the list cannot be read.
  function load() {
    if (!request) {
      request = apiFetch("/api/departments")
        .then((list) => (Array.isArray(list) ? list : []))
        .catch(() => []);
    }
    return request;
  }

  function fallbackLabel(departmentId) {
    return "Department " + departmentId;
  }

  async function label(departmentId) {
    if (departmentId === null || departmentId === undefined) return "";
    const match = (await load()).find((department) => department.departmentId === departmentId);
    return match ? match.departmentName : fallbackLabel(departmentId);
  }

  function option(value, text) {
    const element = document.createElement("option");
    element.value = String(value);
    element.textContent = text;
    return element;
  }

  // Returns how many departments were added. `selectedId` stays selectable even if the list could not
  // be read, so saving a form never silently clears a department the user already has.
  async function fillSelect(select, selectedId) {
    const list = await load();
    const sorted = [...list].sort((a, b) => a.departmentName.localeCompare(b.departmentName));
    for (const department of sorted) {
      select.appendChild(option(department.departmentId, department.departmentName));
    }
    if (selectedId !== null && selectedId !== undefined) {
      if (!list.some((department) => department.departmentId === selectedId)) {
        select.appendChild(option(selectedId, fallbackLabel(selectedId)));
      }
      select.value = String(selectedId);
    }
    return list.length;
  }

  return { load, label, fillSelect };
})();
