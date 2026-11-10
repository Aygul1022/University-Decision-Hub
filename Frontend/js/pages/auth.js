// Login and Register pages. Requires js/shared/api.js to be loaded first.
(function () {
  const AFTER_LOGIN_URL = "../../index.html";

  const form = document.querySelector("form[data-auth-form]");
  if (!form) return;

  const mode = form.dataset.authForm; // "login" or "register"
  const formMessage = form.querySelector("[data-form-message]");
  const submitButton = form.querySelector("button[type=submit]");

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
      window.location.href = AFTER_LOGIN_URL;
    } catch (error) {
      showFieldErrors(error.fieldErrors || {});
      showFormMessage(error.message || "Something went wrong. Please try again.", "error");
      submitButton.disabled = false;
    }
  });

  // Someone who is already logged in sees who they are and can log out from here.
  const current = Session.user;
  const banner = document.querySelector("[data-session-banner]");
  if (current && banner) {
    banner.hidden = false;
    banner.querySelector("[data-session-name]").textContent = current.fullName;
    banner.querySelector("[data-logout]").addEventListener("click", () => {
      Session.clear();
      banner.hidden = true;
    });
  }
})();
