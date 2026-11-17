// Requires api.js and navbar.js.
(function () {
  const pagePath = "Pages/reviews/review-form.html";
  const requestedCourse = new URLSearchParams(
    window.location.search
  ).get("courseId");

  const requestedId = requestedCourse === null
    ? null
    : Number(requestedCourse);

  const validRequestedId =
    Number.isInteger(requestedId) && requestedId > 0;

  const returnPath = validRequestedId
    ? pagePath + "?courseId=" + requestedId
    : pagePath;

  function redirectToLogin() {
    Session.clear();
    window.location.replace(Site.loginUrl(returnPath));
  }

  if (!Session.isLoggedIn) {
    redirectToLogin();
    return;
  }

  const status = document.querySelector("[data-status]");
  const profileHelp = document.querySelector("[data-profile-help]");
  const form = document.querySelector("[data-review-form]");
  const fieldset = form.querySelector("fieldset");
  const message = form.querySelector("[data-form-message]");
  const success = document.querySelector("[data-success]");
  const readLink = document.querySelector("[data-read-reviews]");

  const courses = new Map();
  const instructorIds = new Set();

  let ready = false;
  let submitting = false;

  function showStatus(text, isError = false) {
    status.textContent = text;
    status.className = "notice" + (isError ? " error" : "");
    status.hidden = !text;
  }

  function showMessage(text) {
    message.textContent = text;
    message.className = "form-message error";
    message.hidden = !text;
  }

  function addOption(select, value, label) {
    const option = document.createElement("option");
    option.value = String(value);
    option.textContent = label;
    select.appendChild(option);
  }

  function errorMessage(error) {
    const fields = Object.values(error.fieldErrors || {}).flat();
    return fields.join(" ") || error.message || "The request failed.";
  }

  async function load() {
    try {
      const profile = await apiFetch("/api/auth/me");

      // Whether graduates may write reviews is still a team decision; the server requires a study year.
      if (profile.isGraduate) {
        showStatus("Reviews need a year of study, so graduate accounts cannot write reviews yet.", true);
        return;
      }

      if (
        !Number.isInteger(profile.programmeId) ||
        profile.programmeId <= 0 ||
        !Number.isInteger(profile.studyYear) ||
        profile.studyYear < 1 ||
        profile.studyYear > 6
      ) {
        showStatus("Complete your programme and study year first.", true);
        profileHelp.hidden = false;
        return;
      }

      const [curriculum, instructors] = await Promise.all([
        apiFetch("/api/programmes/" + profile.programmeId + "/courses"),
        apiFetch("/api/instructors"),
      ]);

      if (!Array.isArray(curriculum) || !Array.isArray(instructors)) {
        throw new Error("The course or academic staff list could not be read.");
      }

      // Historical curriculum entries may refer to the same course.
      for (const entry of curriculum) {
        if (!courses.has(entry.courseId)) {
          courses.set(entry.courseId, entry);
        }
      }

      for (const course of courses.values()) {
        addOption(
          form.elements.courseId,
          course.courseId,
          course.courseCode + " · " + course.courseName
        );
      }

      for (const instructor of instructors) {
        instructorIds.add(instructor.instructorId);

        const label = [instructor.academicTitle, instructor.fullName]
          .filter(Boolean)
          .join(" ");

        addOption(
          form.elements.instructorId,
          instructor.instructorId,
          label
        );
      }

      if (!courses.size) {
        showStatus("No courses have been added for your programme yet.", true);
        return;
      }

      if (!instructorIds.size) {
        showStatus("No academic staff members are available yet.", true);
        return;
      }

      if (requestedCourse !== null) {
        if (validRequestedId && courses.has(requestedId)) {
          form.elements.courseId.value = String(requestedId);
        } else {
          showMessage(
            "The linked course is unavailable in your programme. " +
            "Choose a course from the list."
          );
        }
      }

      // Long lists are searched, not scrolled. Without the helper script the plain selects still work.
      // This runs after the options and any linked course are in place, so the box shows the chosen course.
      // (typeof, not window.SearchableSelect: a top-level const is not a property of window.)
      if (typeof SearchableSelect !== "undefined") {
        SearchableSelect.enhance(form.elements.courseId, {
          placeholder: "Search by course code or name",
          noun: "courses",
        });
        SearchableSelect.enhance(form.elements.instructorId, {
          placeholder: "Search by name or title",
          noun: "academic staff",
        });
      }

      ready = true;
      showStatus("");
      form.hidden = false;
    } catch (error) {
      if (error.status === 401) {
        redirectToLogin();
        return;
      }

      showStatus(errorMessage(error), true);
    }
  }

  form.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (!ready || submitting) return;
    showMessage("");

    const values = {
      courseId: Number(form.elements.courseId.value),
      instructorId: Number(form.elements.instructorId.value),
      academicYear: form.elements.academicYear.value.trim(),
      semester: form.elements.semester.value,
      attendanceConfirmed: form.elements.attendanceConfirmed.checked,
      reviewText: form.elements.reviewText.value.trim(),
    };

    if (!courses.has(values.courseId)) {
      showMessage("Choose a course from your programme.");
      return;
    }

    if (!instructorIds.has(values.instructorId)) {
      showMessage("Choose an academic staff member.");
      return;
    }

    const yearMatch = /^([0-9]{4})-([0-9]{4})$/.exec(
      values.academicYear
    );

    if (
      !yearMatch ||
      Number(yearMatch[1]) < 1 ||
      Number(yearMatch[2]) !== Number(yearMatch[1]) + 1
    ) {
      showMessage("Enter consecutive academic years, for example 2025-2026.");
      return;
    }

    if (!["Fall", "Spring", "Summer"].includes(values.semester)) {
      showMessage("Choose a semester.");
      return;
    }

    if (!values.reviewText) {
      showMessage("Write your review.");
      return;
    }

    if (!values.attendanceConfirmed) {
      showMessage("Confirm that you took the course with this academic staff member.");
      return;
    }

    submitting = true;
    fieldset.disabled = true;

    try {
      await apiFetch("/api/reviews", {
        method: "POST",
        body: values,
      });

      ready = false;
      form.hidden = true;

      success.textContent = "Your review was saved.";
      success.hidden = false;

      const course = courses.get(values.courseId);
      const query = new URLSearchParams({
        courseId: String(values.courseId),
        courseCode: course.courseCode,
      });

      readLink.href = Site.url(
        "Pages/reviews/reviews.html?" + query.toString()
      );
      readLink.hidden = false;
    } catch (error) {
      if (error.status === 401) {
        redirectToLogin();
        return;
      }

      showMessage(errorMessage(error));
    } finally {
      submitting = false;
      fieldset.disabled = false;
    }
  });

  load();
})();