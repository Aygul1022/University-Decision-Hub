// Login and Register pages. Register also requires js/shared/departments.js.
(function () {
  const form = document.querySelector("form[data-auth-form]");
  if (!form) return;

  const mode = form.dataset.authForm; // "login" or "register"
  const formMessage = form.querySelector("[data-form-message]");
  const submitButton = form.querySelector("button[type=submit]");
  const registrationDepartmentIds = new Set();

  // ?next=Pages/reviews/reviews.html  -> where to go after a successful login (see Site.membersUrl).
  const requestedNext = new URLSearchParams(window.location.search).get("next");
  const next = Site.isSafeNextPath(requestedNext) ? requestedNext : null;
  const afterLoginUrl = Site.url(next || "index.html");

  if (next) {
    const notice = document.querySelector("[data-next-notice]");
    if (notice) notice.hidden = false;
    // Keep the destination when switching between the Login and Register pages.
    document.querySelectorAll("a[data-keep-next]").forEach((anchor) => {
      anchor.href = anchor.getAttribute("href") + "?next=" + encodeURIComponent(next);
    });
  }

  // Someone who is already logged in has nothing to do here.
  if (Session.isLoggedIn) {
    window.location.replace(afterLoginUrl);
    return;
  }

  function showFormMessage(text, kind) {
    formMessage.textContent = text;
    formMessage.className = "form-message " + (text ? kind : "");
    formMessage.hidden = !text;
  }

  function showFieldErrors(fieldErrors) {
    form.querySelectorAll("[data-error-for]").forEach((element) => {
      const messages = fieldErrors[element.dataset.errorFor] || [];
      element.textContent = messages.join(" ");
      const input = form.elements[element.dataset.errorFor];
      if (input) input.setAttribute("aria-invalid", messages.length ? "true" : "false");
    });
  }

  async function loadDepartmentOptions() {
    const departmentSelect = form.elements.departmentId;
    const departmentStatus = document.querySelector("[data-department-status]");
    submitButton.disabled = true;
    departmentSelect.disabled = true;

    try {
      const count = await Departments.fillSelect(departmentSelect);
      for (const option of departmentSelect.options) {
        if (option.value) registrationDepartmentIds.add(Number(option.value));
      }

      if (count === 0) {
        departmentStatus.textContent = "No department choices are available. You can still register without student information.";
        departmentStatus.hidden = false;
      } else {
        departmentSelect.disabled = false;
      }
    } catch (error) {
      departmentStatus.textContent = error.message || "Department options could not be loaded.";
      departmentStatus.hidden = false;
    } finally {
      submitButton.disabled = false;
    }
  }

  if (mode === "register") loadDepartmentOptions();

  // Checks done in the browser first, so obvious mistakes do not need a round trip.
  // The server repeats every check; these are only for convenience.
  function validate(values) {
    const errors = {};
    if (!values.email) errors.email = ["Enter your email address."];
    // The form is novalidate, so ask the browser's own email rule here: an address without "@"
    // would otherwise reach the server and come back as a confusing answer.
    else if (!form.elements.email.checkValidity()) errors.email = ["Enter a full email address, like name@example.com."];
    if (!values.password) errors.password = ["Enter your password."];
    if (mode === "register") {
      if (values.fullName.length < 2) errors.fullName = ["Enter your full name."];
      if (values.password && values.password.length < 8) errors.password = ["Use at least 8 characters."];
      if (values.password !== form.elements.confirmPassword.value) errors.confirmPassword = ["The two passwords do not match."];
      // Department and year are optional: an account without them can log in but cannot write reviews.
      if (values.departmentId !== null && !registrationDepartmentIds.has(values.departmentId)) {
        errors.departmentId = ["Choose a department from the list."];
      }
    }
    return errors;
  }

  // An empty field is sent as null, not as 0.
  function optionalNumber(text) {
    return text.trim() === "" ? null : Number(text);
  }

  function readValues() {
    const values = {
      email: form.elements.email.value.trim(),
      password: form.elements.password.value,
    };
    if (mode === "register") {
      values.fullName = form.elements.fullName.value.trim();
      values.departmentId = optionalNumber(form.elements.departmentId.value);
      values.studyYear = optionalNumber(form.elements.studyYear.value);
    }
    return values;
  }

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    showFormMessage("", "");

    const values = readValues();
    const errors = validate(values);
    showFieldErrors(errors);
    if (Object.keys(errors).length) {
      showFormMessage("Please correct the highlighted fields.", "error");
      return;
    }

    submitButton.disabled = true;
    try {
      const result = await apiFetch("/api/auth/" + mode, { method: "POST", body: values });
      Session.save(result);
      showFormMessage("Welcome, " + result.user.fullName + ". Redirecting…", "success");
      window.location.href = afterLoginUrl;
    } catch (error) {
      showFieldErrors(error.fieldErrors || {});
      showFormMessage(error.message || "Something went wrong. Please try again.", "error");
      // 409 on Register: this email already has an account, so point to the Log in page.
      if (mode === "register" && error.status === 409) {
        const loginLink = document.querySelector(".auth-switch a");
        if (loginLink) {
          const link = document.createElement("a");
          link.href = loginLink.href;
          link.textContent = "Go to Log in";
          formMessage.append(" ", link);
        }
      }
      submitButton.disabled = false;
    }
  });
})();
