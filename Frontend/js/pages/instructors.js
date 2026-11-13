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

  // The department name comes from the shared list (GET /api/departments).
  function departmentLabel(instructor) {
    return Departments.label(instructor.departmentId);
  }

  // ---------------------------------------------------------------- list page
  function initList() {
    const form = main.querySelector("[data-search-form]");
    const results = main.querySelector("[data-results]");
    const count = main.querySelector("[data-result-count]");
    let latestRequest = 0;

    async function renderList(instructors) {
      await Departments.load(); // one request; the labels below then resolve from it
      const items = [];
      for (const instructor of instructors) {
        const link = document.createElement("a");
        link.href = "instructor-detail.html?id=" + encodeURIComponent(instructor.instructorId);

        const name = document.createElement("span");
        name.className = "instructor-name";
        name.textContent = instructor.fullName;
        link.appendChild(name);

        const meta = document.createElement("span");
        meta.className = "instructor-meta";
        meta.textContent = [instructor.academicTitle, await departmentLabel(instructor)].filter(Boolean).join(" · ");
        link.appendChild(meta);

        const item = document.createElement("li");
        item.appendChild(link);
        items.push(item);
      }
      return items;
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
        const items = await renderList(instructors);
        if (request !== latestRequest) return; // a newer search has started
        results.replaceChildren(...items);

        if (instructors.length === 0) {
          showStatus(filtered
            ? "No instructors match this search. Try a shorter name or choose all departments."
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
    form.elements.departmentId.addEventListener("change", search);
    Departments.fillSelect(form.elements.departmentId);
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

    // One row per course, linking to its detail page, with the periods taught underneath.
    function renderCourses(courses) {
      const list = article.querySelector("[data-courses]");
      const note = article.querySelector("[data-courses-status]");
      list.replaceChildren();
      note.textContent = courses.length === 0 ? "No courses have been assigned to this instructor yet." : "";
      note.hidden = courses.length !== 0;

      for (const course of courses) {
        const link = document.createElement("a");
        link.href = Site.url("Pages/courses/course-detail.html") + "?id=" + encodeURIComponent(course.courseId);

        const title = document.createElement("span");
        title.className = "course-title";
        const code = document.createElement("span");
        code.className = "detail-code";
        code.textContent = course.courseCode;
        title.append(code, course.courseName);
        link.appendChild(title);

        const periods = document.createElement("span");
        periods.className = "course-periods";
        periods.textContent = course.periods.map((period) => period.academicYear + " " + period.semester).join(" · ");
        link.appendChild(periods);

        const item = document.createElement("li");
        item.appendChild(link);
        list.appendChild(item);
      }
    }

    async function render(instructor, courses) {
      document.title = instructor.fullName + " · University Decision Hub";
      setField("academicTitle", instructor.academicTitle);
      setField("fullName", instructor.fullName);
      setField("department", await departmentLabel(instructor));
      renderCourses(courses);

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
        const [instructor, courses] = await Promise.all([
          apiFetch("/api/instructors/" + id),
          apiFetch("/api/instructors/" + id + "/courses"),
        ]);
        await render(instructor, courses);
      } catch (error) {
        showStatus(error.status === 404 ? "This instructor could not be found." : error.message, true);
      }
    }

    load();
  }

  if (main.dataset.page === "instructor-list") initList();
  if (main.dataset.page === "instructor-detail") initDetail();
})();
