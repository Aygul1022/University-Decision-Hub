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

  const programmeSelect = form.elements.programmeId;
  const programmeHint = form.querySelector("[data-programme-hint]");
  let programmeRequest = 0;

  // "Computer Engineering (Bachelor, English)"; degree and language are left out when unknown.
  function programmeLabel(programme) {
    const details = [programme.degreeLevel, programme.instructionLanguage].filter(Boolean).join(", ");
    return programme.programmeName + (details ? " (" + details + ")" : "");
  }

  function addProgrammeOption(value, text) {
    const option = document.createElement("option");
    option.value = String(value);
    option.textContent = text;
    programmeSelect.appendChild(option);
  }

  // Lists the programmes of one department (GET /api/programmes?departmentId=...). `selectedId` stays
  // selectable even when the list cannot be read: PUT /api/auth/me replaces the whole profile, so a
  // missing option would silently clear a programme the user already has.
  async function fillProgrammes(departmentId, selectedId) {
    const requestNumber = ++programmeRequest;
    programmeSelect.length = 1; // keeps only "Not selected"
    programmeSelect.disabled = true;

    let list = [];
    let failed = false;
    if (departmentId !== null) {
      try {
        const result = await apiFetch("/api/programmes?departmentId=" + encodeURIComponent(departmentId));
        list = Array.isArray(result) ? result : [];
      } catch (error) {
        failed = true;
      }
    }
    if (requestNumber !== programmeRequest) return; // the department changed again meanwhile

    for (const programme of list) addProgrammeOption(programme.programmeId, programmeLabel(programme));
    if (selectedId !== null && selectedId !== undefined) {
      if (!list.some((programme) => programme.programmeId === selectedId)) {
        addProgrammeOption(selectedId, "Programme " + selectedId);
      }
      programmeSelect.value = String(selectedId);
    }

    programmeSelect.disabled = programmeSelect.length === 1;
    programmeHint.textContent = departmentId === null
      ? "Choose your department first."
      : failed
        ? "The programme list could not be loaded. Reload the page to try again."
        : list.length === 0
          ? "No programmes have been added for this department yet."
          : "Needed to write reviews. Choose the programme you are enrolled in.";
  }

  function fill(profile) {
    form.elements.email.value = profile.email;
    form.elements.fullName.value = profile.fullName;
    // All three are null for an account without a student profile.
    form.elements.departmentId.value = profile.departmentId ?? "";
    // A graduate has no study year and shows the "Graduate" choice instead.
    form.elements.studyYear.value = profile.isGraduate ? "graduate" : profile.studyYear ?? "";
    return fillProgrammes(profile.departmentId ?? null, profile.programmeId ?? null);
  }

  // A programme belongs to one department, so a different department starts without a programme.
  form.elements.departmentId.addEventListener("change", () => {
    fillProgrammes(optionalNumber(form.elements.departmentId.value), null);
  });

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
      await fill(profile);
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

    const year = form.elements.studyYear.value;
    const values = {
      fullName: form.elements.fullName.value.trim(),
      departmentId: optionalNumber(form.elements.departmentId.value),
      // Always sent: the server treats a missing programmeId as "clear the selection".
      programmeId: optionalNumber(programmeSelect.value),
      isGraduate: year === "graduate",
      studyYear: year === "graduate" ? null : optionalNumber(year),
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
      await fill(profile);
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
