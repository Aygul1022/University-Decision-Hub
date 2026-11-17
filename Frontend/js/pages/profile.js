// Requires js/shared/api.js and js/shared/navbar.js.
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
  const departmentHint = form.querySelector("[data-department-hint]");
  const departmentRetry = form.querySelector("[data-department-retry]");
  const programmeSelect = form.elements.programmeId;
  const programmeHint = form.querySelector("[data-programme-hint]");
  const programmeRetry = form.querySelector("[data-programme-retry]");
  let profile;
  let departmentRequest = 0;
  let programmeRequest = 0;
  let selectableProgrammes = new Map();

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
    // Dropdown requests can complete after the form is closed.
    departmentRequest++;
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
  submitButton.disabled = true;
  selectableProgrammes = new Map();

  // Kayıtlı seçim yükleme sırasında kaybolmasın.
  setProgrammeOptions([], selectedId);
  programmeHint.textContent = "Loading programmes…";

  try {
    const programmes = await apiFetch("/api/programmes");

    if (request !== programmeRequest) return;

    if (!Array.isArray(programmes)) {
      throw new Error("Invalid programme list.");
    }

    const linked = programmes.filter(
      programme => programme.departmentId === departmentId
    );

    // Eski/kadro bölümüne program bağlı değilse kullanıcı
    // gerçek katalog programları arasından açıkça seçim yapabilir.
    const available = linked.length > 0 ? linked : programmes;

    selectableProgrammes = new Map(
      available.map(programme => [
        String(programme.programmeId),
        programme
      ])
    );

    setProgrammeOptions(available, selectedId);
    programmeSelect.disabled = available.length === 0;

    programmeHint.textContent =
      available.length === 0
        ? "No programmes are available in the catalogue."
        : linked.length > 0
          ? "Choose the programme you are enrolled in."
          : "Choose your enrolled programme. Its department will be updated in this form; save to confirm.";

    submitButton.disabled = false;
  } catch (error) {
    if (request !== programmeRequest) return;

    // Kayıtlı seçenek setProgrammeOptions ile korunuyor.
    programmeHint.textContent =
      "Programmes could not be loaded. Try again.";

    programmeRetry.hidden = false;

    // Hata sırasında başka bölüme eksik bilgilerle kayıt yapılmasın.
    submitButton.disabled =
      departmentId !== profile.departmentId;
  }
}

  function loadSelectedDepartment() {
    const departmentId = optionalNumber(departmentSelect.value);
    const selectedId = departmentId === profile.departmentId ? profile.programmeId : null;
    return loadProgrammes(departmentId, selectedId);
  }

  function setDepartmentOptions(departments) {
    const savedId = profile.departmentId;
    const options = departments.map(department => new Option(department.label, String(department.departmentId)));
    if (savedId != null && !departments.some(department => department.departmentId === savedId)) {
      const savedLabel = profile.departmentName ?? "Department " + savedId;
      options.push(new Option(savedLabel + " (current selection)", String(savedId)));
    }
    departmentSelect.replaceChildren(new Option("Not a student", ""), ...options);
    departmentSelect.value = savedId ?? "";
  }

  async function loadDepartmentOptions() {
    const request = ++departmentRequest;
    programmeRequest++;
    departmentRetry.hidden = true;
    departmentSelect.disabled = true;
    programmeSelect.disabled = true;
    programmeRetry.hidden = true;
    submitButton.disabled = true;
    departmentHint.textContent = "Loading departments…";
    programmeHint.textContent = "";
    setDepartmentOptions([]);
    setProgrammeOptions([], profile.programmeId);

    try {
      const [departments, programmes] = await Promise.all([
        apiFetch("/api/departments"),
        apiFetch("/api/programmes"),
      ]);
      if (request !== departmentRequest) return;
      if (!Array.isArray(departments) || !Array.isArray(programmes)) {
        throw new Error("Invalid catalogue lists.");
      }
      const linkedDepartments = new Set(programmes.map(programme => programme.departmentId));
      const available = departments.filter(department => linkedDepartments.has(department.departmentId))
        .map(department => ({
          departmentId: department.departmentId,
          label: department.departmentKey === "DEP-0064"
            ? "Bilgisayar Mühendisliği (İngilizce)"
            : department.departmentName,
        }))
        .sort((a, b) => a.label.localeCompare(b.label));

      setDepartmentOptions(available);
      departmentSelect.disabled = false;
      departmentHint.textContent = available.length
        ? "Choose a department with a programme."
        : "No departments with programmes are currently listed.";
    } catch {
      if (request !== departmentRequest) return;
      departmentHint.textContent = "Departments could not be loaded. Try again.";
      departmentRetry.hidden = false;
    }

    return loadSelectedDepartment();
  }

  editButton.addEventListener("click", () => {
    form.elements.email.value = profile.email;
    form.elements.fullName.value = profile.fullName;
    form.elements.studyYear.value = profile.isGraduate ? "graduate" : profile.studyYear ?? "";
    showFieldErrors({});
    showFormError("");
    summary.hidden = true;
    form.hidden = false;
    setStatus("");
    form.elements.fullName.focus();
    return loadDepartmentOptions();
  });

  departmentSelect.addEventListener("change", loadSelectedDepartment);
  programmeSelect.addEventListener("change", () => {
  const programme = selectableProgrammes.get(programmeSelect.value);

  // “Not selected” veya korunmuş eski seçenek:
  // bölüm bilgisini kendiliğinden değiştirme.
  if (!programme) return;

  const departmentValue = String(programme.departmentId);

  const departmentExists = Array.from(
    departmentSelect.options
  ).some(option => option.value === departmentValue);

  if (!departmentExists) {
    departmentSelect.add(
      new Option(
        "Department " + programme.departmentId,
        departmentValue
      )
    );
  }

  // Bölüm ID’si seçilen programın gerçek API kaydından gelir.
  departmentSelect.value = departmentValue;

  programmeHint.textContent =
    "Programme selected. Save changes to confirm your profile.";
});
  departmentRetry.addEventListener("click", loadDepartmentOptions);
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
