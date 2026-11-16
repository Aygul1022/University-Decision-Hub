// Requires js/shared/api.js, js/shared/departments.js and js/shared/navbar.js.
(function () {
  const profilePath = "Pages/auth/profile.html";

  function requireLogin() {
    Session.clear();
    window.location.replace(Site.loginUrl(profilePath));
  }

  if (!Session.isLoggedIn) {
    requireLogin();
    return;
  }

  const status = document.querySelector("[data-status]");
  const summary = document.querySelector("[data-profile-summary]");
  const editButton = summary.querySelector("[data-profile-edit]");
  const form = document.querySelector("[data-profile-form]");
  const fields = form.querySelector("fieldset");
  const formMessage = form.querySelector("[data-form-message]");
  const submitButton = form.querySelector("button[type=submit]");
  const departmentSelect = form.elements.departmentId;
  const programmeSelect = form.elements.programmeId;
  const programmeHint = form.querySelector("[data-programme-hint]");
  const programmeRetry = form.querySelector("[data-programme-retry]");
  let profile;
  let programmeRequest = 0;

  function setStatus(message, kind = "") {
    status.textContent = message;
    status.className = "notice " + kind;
    status.hidden = !message;
  }

  function showFormError(message) {
    formMessage.textContent = message;
    formMessage.className = "form-message error";
    formMessage.hidden = !message;
  }

  function showFieldErrors(fieldErrors) {
    for (const element of form.querySelectorAll("[data-error-for]")) {
      const name = element.dataset.errorFor;
      const messages = fieldErrors[name] || [];
      element.textContent = messages.join(" ");
      form.elements[name].setAttribute("aria-invalid", String(messages.length > 0));
    }
  }

  function displaySummary(message = "", restoreFocus = false) {
    // A response from a dropdown request can arrive after Cancel.
    programmeRequest++;
    const values = {
      fullName: profile.fullName,
      email: profile.email,
      department: profile.departmentName ?? (profile.departmentId == null ? "Not selected" : "Department " + profile.departmentId),
      programme: profile.programmeName ?? (profile.programmeId == null ? "Not selected" : "Programme " + profile.programmeId),
      studyYear: profile.isGraduate ? "Graduate" : profile.studyYear == null ? "Not selected" : "Year " + profile.studyYear,
    };
    for (const element of summary.querySelectorAll("[data-profile-value]")) {
      element.textContent = values[element.dataset.profileValue];
    }
    form.hidden = true;
    summary.hidden = false;
    setStatus(message, message ? "success" : "");
    if (restoreFocus) editButton.focus();
  }

  function rememberProfile(savedProfile) {
    profile = savedProfile;
    localStorage.setItem("udh.user", JSON.stringify(profile));
  }

  function optionalNumber(value) {
    return value === "" ? null : Number(value);
  }

  function programmeLabel(programme) {
    const details = [programme.degreeLevel, programme.instructionLanguage].filter(Boolean).join(", ");
    return programme.programmeName + (details ? " (" + details + ")" : "");
  }

  function setProgrammeOptions(programmes, selectedId) {
    const options = programmes.map(programme =>
      new Option(programmeLabel(programme), String(programme.programmeId))
    );
    // Keep the saved ID visible while loading or if the catalogue cannot supply its name.
    if (selectedId != null && !programmes.some(programme => programme.programmeId === selectedId)) {
      options.push(new Option(profile.programmeName ?? "Programme " + selectedId, String(selectedId)));
    }
    programmeSelect.replaceChildren(new Option("Not selected", ""), ...options);
    programmeSelect.value = selectedId ?? "";
  }

  async function loadProgrammes(departmentId, selectedId) {
    const request = ++programmeRequest;
    programmeRetry.hidden = true;
    programmeSelect.disabled = true;
    setProgrammeOptions([], selectedId);

    if (departmentId === null) {
      programmeHint.textContent = "Choose your department first.";
      submitButton.disabled = false;
      return;
    }

    submitButton.disabled = true;
    programmeHint.textContent = "Loading programmes…";

    try {
      const programmes = await apiFetch("/api/programmes?departmentId=" + encodeURIComponent(departmentId));
      if (request !== programmeRequest) return;
      if (!Array.isArray(programmes)) throw new Error("Invalid programme list.");

      setProgrammeOptions(programmes, selectedId);
      programmeSelect.disabled = programmes.length === 0;
      programmeHint.textContent = programmes.length
        ? "Choose the programme you are enrolled in."
        : "No programmes are listed for this department.";
      submitButton.disabled = false;
    } catch {
      if (request !== programmeRequest) return;
      programmeHint.textContent = "Programmes could not be loaded. Try again.";
      programmeRetry.hidden = false;
      // The original department can be saved with its original programme during an outage.
      submitButton.disabled = departmentId !== profile.departmentId;
    }
  }

  function loadSelectedDepartment() {
    const departmentId = optionalNumber(departmentSelect.value);
    const selectedId = departmentId === profile.departmentId ? profile.programmeId : null;
    return loadProgrammes(departmentId, selectedId);
  }

  editButton.addEventListener("click", async () => {
    editButton.disabled = true;
    form.elements.email.value = profile.email;
    form.elements.fullName.value = profile.fullName;
    form.elements.studyYear.value = profile.isGraduate ? "graduate" : profile.studyYear ?? "";
    departmentSelect.length = 1;
    await Departments.fillSelect(departmentSelect, profile.departmentId);

    showFieldErrors({});
    showFormError("");
    summary.hidden = true;
    form.hidden = false;
    setStatus("");
    editButton.disabled = false;
    form.elements.fullName.focus();
    loadSelectedDepartment();
  });

  departmentSelect.addEventListener("change", loadSelectedDepartment);
  programmeRetry.addEventListener("click", loadSelectedDepartment);
  form.querySelector("[data-profile-cancel]").addEventListener("click", () => displaySummary("", true));

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (submitButton.disabled || fields.disabled || form.hidden) return;
    showFormError("");

    const year = form.elements.studyYear.value;
    const values = {
      fullName: form.elements.fullName.value.trim(),
      departmentId: optionalNumber(departmentSelect.value),
      programmeId: optionalNumber(programmeSelect.value),
      isGraduate: year === "graduate",
      studyYear: year === "graduate" ? null : optionalNumber(year),
    };

    const errors = {};
    if (values.fullName.length < 2) errors.fullName = ["Enter your full name."];
    if (values.departmentId !== null && !(values.departmentId >= 1)) errors.departmentId = ["Choose a department from the list."];
    showFieldErrors(errors);
    if (Object.keys(errors).length) {
      showFormError("Please correct the highlighted fields.");
      return;
    }

    fields.disabled = true;
    try {
      rememberProfile(await apiFetch("/api/auth/me", { method: "PUT", body: values }));
      displaySummary("Your profile was saved.", true);
    } catch (error) {
      if (error.status === 401) {
        requireLogin();
        return;
      }
      showFieldErrors(error.fieldErrors || {});
      showFormError(error.message || "Something went wrong. Please try again.");
    } finally {
      fields.disabled = false;
    }
  });

  async function load() {
    try {
      rememberProfile(await apiFetch("/api/auth/me"));
      displaySummary();
    } catch (error) {
      if (error.status === 401) {
        requireLogin();
        return;
      }
      setStatus(error.message || "Your profile could not be loaded.", "error");
    }
  }

  load();
})();
