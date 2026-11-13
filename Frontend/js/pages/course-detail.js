// Course detail page: course-detail.html?id=<CourseId>
// Reads the existing GET /api/courses/{id} endpoint; it does not define its own course data.
// Requires js/shared/api.js, js/shared/departments.js and js/shared/navbar.js.
(function () {
  const status = document.querySelector("[data-status]");
  const article = document.querySelector("[data-course]");

  function showStatus(text, isError) {
    status.textContent = text;
    status.className = "notice" + (isError ? " error" : "");
    status.hidden = false;
    article.hidden = true;
  }

  function setField(name, value) {
    article.querySelector('[data-field="' + name + '"]').textContent = value;
  }

  async function render(course) {
    document.title = course.courseCode + " · University Decision Hub";

    setField("courseCode", course.courseCode);
    setField("courseName", course.courseName);
    setField("courseType", course.isElective ? "Elective" : "Compulsory");
    setField("department", course.departmentName || await Departments.label(course.departmentId));
    setField("recommendedStudyYear", course.recommendedStudyYear ? "Year " + course.recommendedStudyYear : "Not set yet");
    setField("courseCredit", course.courseCredit);
    setField("ects", course.ects);

    const description = (course.courseDescription || "").trim();
    article.querySelector("[data-description-section]").hidden = !description;
    setField("courseDescription", description);

    // Link to the Read Reviews page, filtered to this course. Visitors go through login first (FR01).
    const reviewsPath = "Pages/reviews/reviews.html?courseCode=" + encodeURIComponent(course.courseCode);
    const link = article.querySelector("[data-reviews-link]");
    link.href = Site.membersUrl(reviewsPath);
    link.textContent = Session.isLoggedIn ? "Read reviews for this course" : "Log in to read reviews";
    article.querySelector("[data-reviews-text]").textContent = Session.isLoggedIn
      ? "See what students who took " + course.courseCode + " wrote about it."
      : "Reviews are available to registered students. Log in to see what others wrote about " + course.courseCode + ".";

    status.hidden = true;
    article.hidden = false;
  }

  async function load() {
    const id = Number(new URLSearchParams(window.location.search).get("id"));
    if (!Number.isInteger(id) || id <= 0) {
      showStatus("No course was selected. Open this page from a course link.", true);
      return;
    }

    try {
      await render(await apiFetch("/api/courses/" + id));
    } catch (error) {
      showStatus(error.status === 404 ? "This course could not be found." : error.message, true);
    }
  }

  load();
})();
