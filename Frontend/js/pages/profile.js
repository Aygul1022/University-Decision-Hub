// My profile page: shows the logged-in user's stored profile and lets them update it.
// Requires js/shared/api.js, js/shared/departments.js and js/shared/navbar.js.
(function () {
  // Only for logged-in users; visitors are sent to login and come back here afterwards.
  if (!Session.isLoggedIn) {
    window.location.replace(Site.loginUrl("Pages/auth/profile.html"));
    return;
  }

  const status = document.querySelector("[data-status]");
  const form = document.querySelector("[data-profile-form]");
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
      form.elements[element.dataset.errorFor].setAttribute("aria-invalid", messages.length ? "true" : "false");
    });
  }

  function fill(profile) {
    form.elements.email.value = profile.email;
    form.elements.fullName.value = profile.fullName;
    // Both are null for an account without a student profile.
    form.elements.departmentId.value = profile.departmentId ?? "";
    form.elements.studyYear.value = profile.studyYear ?? "";
  }

  // An empty field is sent as null, not as 0.
  function optionalNumber(text) {
    return text.trim() === "" ? null : Number(text);
  }

  // Keeps the copy of the profile used by the navbar in step with what the server stored.
  function rememberProfile(profile) {
    localStorage.setItem("udh.user", JSON.stringify(profile));
  }

  async function load() {
    try {
      const profile = await apiFetch("/api/auth/me");
      rememberProfile(profile);
      // The list is filled before the stored department is selected in it.
      await Departments.fillSelect(form.elements.departmentId, profile.departmentId);
      fill(profile);
      status.hidden = true;
      form.hidden = false;
    } catch (error) {
      if (error.status === 401) {
        Session.clear();
        window.location.replace(Site.loginUrl("Pages/auth/profile.html"));
        return;
      }
      status.textContent = error.message;
      status.className = "notice error";
    }
  }

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    showFormMessage("", "");

    const values = {
      fullName: form.elements.fullName.value.trim(),
      departmentId: optionalNumber(form.elements.departmentId.value),
      studyYear: optionalNumber(form.elements.studyYear.value),
    };

    const errors = {};
    if (values.fullName.length < 2) errors.fullName = ["Enter your full name."];
    if (values.departmentId !== null && !(values.departmentId >= 1)) errors.departmentId = ["Choose a department from the list."];
    showFieldErrors(errors);
    if (Object.keys(errors).length) {
      showFormMessage("Please correct the highlighted fields.", "error");
      return;
    }

    submitButton.disabled = true;
    try {
      const profile = await apiFetch("/api/auth/me", { method: "PUT", body: values });
      rememberProfile(profile);
      fill(profile);
      showFormMessage("Your profile was saved.", "success");
    } catch (error) {
      showFieldErrors(error.fieldErrors || {});
      showFormMessage(error.message || "Something went wrong. Please try again.", "error");
    } finally {
      submitButton.disabled = false;
    }
  });

  load();
})();
