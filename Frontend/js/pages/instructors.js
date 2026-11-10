// Instructor list (instructors.html) and instructor detail (instructor-detail.html?id=<InstructorId>).
// Requires js/shared/api.js and js/shared/navbar.js.
(function () {
  const main = document.querySelector("main[data-page]");
  if (!main) return;

  const status = main.querySelector("[data-status]");

  function showStatus(text, isError) {
    status.textContent = text;
    status.className = "notice" + (isError ? " error" : "");
    status.hidden = !text;
  }

  // The department name will come from the Departments endpoint; until then show its number.
  function departmentLabel(instructor) {
    return "Department " + instructor.departmentId;
  }

  // ---------------------------------------------------------------- list page
  function initList() {
    const form = main.querySelector("[data-search-form]");
    const results = main.querySelector("[data-results]");
    const count = main.querySelector("[data-result-count]");
    let latestRequest = 0;

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
      const params = new URLSearchParams();
      const query = form.elements.query.value.trim();
      const departmentId = form.elements.departmentId.value.trim();
      if (query) params.set("query", query);
      if (departmentId) params.set("departmentId", departmentId);
      const filtered = params.toString() !== "";

      const request = ++latestRequest;
      count.hidden = true;
      showStatus("Loading instructors…", false);

      try {
        const instructors = await apiFetch("/api/instructors" + (filtered ? "?" + params : ""));
        if (request !== latestRequest) return; // a newer search has started
        renderList(instructors);

        if (instructors.length === 0) {
          showStatus(filtered
            ? "No instructors match this search. Try a shorter name or clear the department number."
            : "No instructors have been added yet.", false);
        } else {
          showStatus("", false);
          count.textContent = instructors.length === 1 ? "1 instructor" : instructors.length + " instructors";
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
        showStatus("No instructor was selected. Choose one from the instructor list.", true);
        return;
      }

      try {
        render(await apiFetch("/api/instructors/" + id));
      } catch (error) {
        showStatus(error.status === 404 ? "This instructor could not be found." : error.message, true);
      }
    }

    load();
  }

  if (main.dataset.page === "instructor-list") initList();
  if (main.dataset.page === "instructor-detail") initDetail();
})();
