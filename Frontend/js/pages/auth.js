// Login and Register pages. Requires js/shared/api.js and js/shared/navbar.js to be loaded first.
(function () {
  const form = document.querySelector("form[data-auth-form]");
  if (!form) return;

  const mode = form.dataset.authForm; // "login" or "register"
  const formMessage = form.querySelector("[data-form-message]");
  const submitButton = form.querySelector("button[type=submit]");

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

  // Checks done in the browser first, so obvious mistakes do not need a round trip.
  // The server repeats every check; these are only for convenience.
  function validate(values) {
    const errors = {};
    if (!values.email) errors.email = ["Enter your email address."];
    if (!values.password) errors.password = ["Enter your password."];
    if (mode === "register") {
      if (values.fullName.length < 2) errors.fullName = ["Enter your full name."];
      if (values.password && values.password.length < 8) errors.password = ["Use at least 8 characters."];
      if (values.password !== form.elements.confirmPassword.value) errors.confirmPassword = ["The two passwords do not match."];
      if (!(values.departmentId >= 1)) errors.departmentId = ["Enter your department number."];
      if (!(values.studyYear >= 1 && values.studyYear <= 6)) errors.studyYear = ["Choose your year of study."];
    }
    return errors;
  }

  function readValues() {
    const values = {
      email: form.elements.email.value.trim(),
      password: form.elements.password.value,
    };
    if (mode === "register") {
      values.fullName = form.elements.fullName.value.trim();
      values.departmentId = Number(form.elements.departmentId.value);
      values.studyYear = Number(form.elements.studyYear.value);
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
      submitButton.disabled = false;
    }
  });
})();
