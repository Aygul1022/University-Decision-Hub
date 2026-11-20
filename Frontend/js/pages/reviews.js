(function () {
  const reviewsPath = "Pages/reviews/reviews.html";
  const form = document.querySelector(".review-filters");
  const results = document.querySelector("[data-review-results]");
  const status = document.querySelector("[data-review-status]");
  const catalogStatus = document.querySelector("[data-catalog-status]");
  const pagination = document.querySelector(".review-pagination");
  const dialog = document.querySelector(".review-dialog");
  const detailTitle = dialog.querySelector("h2");
  const detailContext = dialog.querySelector("[data-detail-context]");
  const detailStatus = dialog.querySelector("[data-detail-status]");
  const detailText = dialog.querySelector("[data-detail-text]");
  const moderationButton = dialog.querySelector("[data-review-moderation]");

  const currentQuery = new URLSearchParams(window.location.search);
  const params = new URLSearchParams();
  const filterNames = ["courseCode", "departmentId", "instructorId", "academicYear", "semester"];
  for (const name of ["courseId", ...filterNames, "page", "pageSize"]) {
    const value = currentQuery.get(name)?.trim();
    if (value) params.set(name, value);
  }

  function pageUrl(query) {
    const search = query.toString().replaceAll(".", "%2E").replaceAll("*", "%2A");
    return search ? reviewsPath + "?" + search : reviewsPath;
  }

  function redirectToLogin() {
    Session.clear();
    const nextPath = pageUrl(params);
    window.location.replace(Site.loginUrl(Site.isSafeNextPath(nextPath) ? nextPath : reviewsPath));
  }

  if (!Session.isLoggedIn) {
    redirectToLogin();
    return;
  }

  const isAdministrator = Session.user?.isAdministrator === true;
  if (isAdministrator) {
    form.querySelector("[data-moderation-filter]").hidden = false;
    form.elements.includeHidden.checked = currentQuery.get("includeHidden") === "true";
    if (form.elements.includeHidden.checked) params.set("includeHidden", "true");
  }

  function setNotice(element, message, isError = false) {
    element.textContent = message;
    element.classList.toggle("error", isError);
    element.hidden = !message;
  }

  form.elements.courseCode.value = params.get("courseCode") || "";
  form.elements.academicYear.value = params.get("academicYear") || "";
  form.elements.semester.value = params.get("semester") || "";
  form.querySelector("[data-course-scope]").hidden = !params.has("courseId");
  form.addEventListener("submit", (event) => {
    event.preventDefault();
    const search = new URLSearchParams();
    for (const name of filterNames) {
      const value = form.elements[name].value.trim();
      if (value) search.set(name, value);
    }
    // Editing the course or department starts a new search across the catalogue.
    if (params.has("courseId") && search.get("courseCode") === params.get("courseCode")
        && search.get("departmentId") === params.get("departmentId")) {
      search.set("courseId", params.get("courseId"));
    }
    if (isAdministrator && form.elements.includeHidden.checked) search.set("includeHidden", "true");
    if (params.has("pageSize")) search.set("pageSize", params.get("pageSize"));
    window.location.assign(Site.url(pageUrl(search)));
  });

  async function loadOptions(name, endpoint, labelFor, fallbackLabel) {
    const select = form.elements[name];
    const selected = params.get(name);
    if (selected) {
      select.add(new Option(fallbackLabel + " " + selected, selected, false, true));
    }

    try {
      const entries = await apiFetch(endpoint);
      // Preserve a deep-linked ID even when its record is missing from the catalog.
      const fallback = selected ? select.options[1] : null;
      for (const entry of entries) {
        const id = String(entry[name]);
        if (id === selected) {
          fallback.textContent = labelFor(entry);
        } else {
          select.add(new Option(labelFor(entry), id));
        }
      }
      return true;
    } catch {
      return false;
    }
  }

  function textElement(tag, className, text) {
    const element = document.createElement(tag);
    element.className = className;
    element.textContent = text;
    return element;
  }

  function courseTitle(review) {
    return [review.courseCode, review.courseName].filter(Boolean).join(" · ") || "Course review";
  }

  function reviewContext(review) {
    return [review.instructorName, review.academicYear, review.semester].filter(Boolean).join(" · ");
  }

  let detailRequest = 0;
  async function showDetail(reviewId) {
    const request = ++detailRequest;
    detailTitle.textContent = "Review detail";
    detailContext.hidden = true;
    detailText.hidden = true;
    moderationButton.hidden = true;
    moderationButton.onclick = null;
    setNotice(detailStatus, "Loading review…");
    dialog.showModal();
    try {
      const review = await apiFetch("/api/reviews/" + reviewId);
      if (request !== detailRequest || !dialog.open) return;
      detailTitle.textContent = courseTitle(review);
      detailContext.textContent = reviewContext(review);
      detailContext.hidden = !detailContext.textContent;
      detailText.textContent = review.reviewText;
      detailText.hidden = false;
      setNotice(detailStatus, review.isHidden ? "This review is hidden from readers." : "");
      if (isAdministrator) {
        moderationButton.textContent = review.isHidden ? "Restore review" : "Hide review";
        moderationButton.onclick = () => setReviewVisibility(review, request);
        moderationButton.hidden = false;
      }
    } catch (error) {
      if (request !== detailRequest || !dialog.open) return;
      if (error.status === 401) return redirectToLogin();
      setNotice(detailStatus, error.status === 404 ? "This review is no longer available." : error.message, true);
    }
  }

  async function setReviewVisibility(review, request) {
    const isHidden = !review.isHidden;
    if (isHidden && !window.confirm("Hide this review from readers?")) return;

    moderationButton.disabled = true;
    try {
      await apiFetch("/api/reviews/" + review.reviewId + "/moderation", {
        method: "PUT",
        body: { isHidden },
      });
      if (request === detailRequest && dialog.open) dialog.close();
      await loadReviews(isHidden ? "Review hidden." : "Review restored.");
    } catch (error) {
      if (error.status === 401) return redirectToLogin();
      if (request === detailRequest && dialog.open) setNotice(detailStatus, error.message, true);
    } finally {
      moderationButton.disabled = false;
    }
  }

  function renderReview(review) {
    const article = document.createElement("article");
    article.className = "card review-card";
    const context = reviewContext(review);
    article.append(textElement("h3", "", courseTitle(review)));
    if (context) article.append(textElement("p", "review-context", context));
    if (review.isHidden) article.append(textElement("p", "review-context", "Hidden from readers"));
    const text = review.reviewText || "";
    const excerpt = text.length > 240 ? text.slice(0, 237).trimEnd() + "…" : text;
    article.append(textElement("p", "review-text", excerpt));
    const button = textElement("button", "button secondary", "View review");
    button.type = "button";
    button.setAttribute("aria-label", "View review for " + courseTitle(review));
    button.addEventListener("click", () => showDetail(review.reviewId));
    const actions = textElement("div", "review-actions", "");
    actions.append(button);
    // Only a convenience: hiding the button protects nothing. The server checks the Administrator role itself.
    if (isAdministrator) {
      const deleteButton = textElement("button", "button secondary", "Delete");
      deleteButton.type = "button";
      deleteButton.setAttribute("aria-label", "Delete review for " + courseTitle(review));
      deleteButton.addEventListener("click", () => deleteReview(review, deleteButton));
      actions.append(deleteButton);
    }
    article.append(actions);
    return article;
  }

  const deleting = new Set();
  async function deleteReview(review, deleteButton) {
    if (deleting.has(review.reviewId)) return;
    if (!window.confirm("Delete this review permanently? This cannot be undone.")) return;

    deleting.add(review.reviewId);
    deleteButton.disabled = true;
    deleteButton.textContent = "Deleting…";
    try {
      // 204 with an empty body: apiFetch answers null for it.
      await apiFetch("/api/reviews/" + review.reviewId, { method: "DELETE" });
      deleting.delete(review.reviewId);
      await loadReviews("Review deleted.", true);
    } catch (error) {
      deleting.delete(review.reviewId);
      if (error.status === 401) return redirectToLogin();
      if (error.status === 404) {
        await loadReviews("This review no longer exists.", true);
        return;
      }
      deleteButton.disabled = false;
      deleteButton.textContent = "Delete";
      if (error.status === 403) {
        setNotice(status, "Only administrators can delete reviews.", true);
      } else if (error.status === 0) {
        setNotice(status, "The review was not deleted: " + error.message + " Try again.", true);
      } else {
        setNotice(status, "The review was not deleted. " + (error.message || "Try again."), true);
      }
    }
  }

  const DEFAULT_PAGE_SIZE = 10;

  function pageLink(label, page, className, ariaLabel) {
    const link = textElement("a", className, label);
    const query = new URLSearchParams(params);
    query.set("page", page);
    link.href = Site.url(pageUrl(query));
    if (ariaLabel) link.setAttribute("aria-label", ariaLabel);
    return link;
  }

  // Previous arrow, numbered pages (1, the last page and the pages around the current one, with … for gaps), next arrow.
  function renderPagination(page, lastPage) {
    const items = [];
    const previousLabel = "Previous page";
    const nextLabel = "Next page";

    if (page > 1) {
      items.push(pageLink("‹", Math.min(page - 1, lastPage), "page-arrow", previousLabel));
    } else {
      const arrow = textElement("span", "page-arrow disabled", "‹");
      arrow.setAttribute("aria-label", previousLabel);
      arrow.setAttribute("aria-disabled", "true");
      items.push(arrow);
    }

    const wanted = new Set([1, lastPage, page - 2, page - 1, page, page + 1, page + 2]);
    let previous = 0;
    for (const number of [...wanted].filter((n) => n >= 1 && n <= lastPage).sort((x, y) => x - y)) {
      if (number - previous > 1) items.push(textElement("span", "page-gap", "…"));
      if (number === page) {
        const current = textElement("span", "page-number current", String(number));
        current.setAttribute("aria-current", "page");
        items.push(current);
      } else {
        items.push(pageLink(String(number), number, "page-number", "Page " + number));
      }
      previous = number;
    }

    if (page < lastPage) {
      items.push(pageLink("›", page + 1, "page-arrow", nextLabel));
    } else {
      const arrow = textElement("span", "page-arrow disabled", "›");
      arrow.setAttribute("aria-label", nextLabel);
      arrow.setAttribute("aria-disabled", "true");
      items.push(arrow);
    }

    pagination.replaceChildren(...items);
  }

  async function loadReviews(message = "", stepBackIfEmpty = false) {
    try {
      const query = new URLSearchParams(params);
      if (!query.has("pageSize")) query.set("pageSize", String(DEFAULT_PAGE_SIZE));
      const suffix = "?" + query.toString();
      const { items, totalCount, page, pageSize } = await apiFetch("/api/reviews" + suffix);
      const lastPage = Math.max(1, Math.ceil(totalCount / pageSize));
      // The last review of the last page was deleted: show the new last page, keeping every filter.
      if (stepBackIfEmpty && !items.length && totalCount > 0 && page > lastPage) {
        params.set("page", String(lastPage));
        window.history.replaceState(null, "", Site.url(pageUrl(params)));
        return loadReviews(message);
      }
      results.replaceChildren(...items.map(renderReview));
      pagination.hidden = lastPage <= 1 && page <= 1;
      renderPagination(page, lastPage);
      const resultsMessage = items.length
        ? totalCount + (totalCount === 1 ? " review found." : " reviews found.")
        : totalCount ? "There are no reviews on this page." : "No reviews match your filters.";
      setNotice(status, message ? message + " " + resultsMessage : resultsMessage);
    } catch (error) {
      if (error.status === 401) return redirectToLogin();
      const fieldMessages = Object.values(error.fieldErrors || {}).flat();
      setNotice(status, fieldMessages.join(" ") || error.message || "Could not load reviews.", true);
    }
  }

  Promise.all([
    loadOptions("departmentId", "/api/departments", (entry) => entry.departmentName, "Department"),
    loadOptions("instructorId", "/api/instructors", (entry) => entry.fullName, "Academic staff"),
  ]).then(([departmentsLoaded, instructorsLoaded]) => {
    const missing = [];
    if (!departmentsLoaded) missing.push("departments");
    if (!instructorsLoaded) missing.push("academic staff");
    if (missing.length) setNotice(catalogStatus, "Could not load " + missing.join(" and ") + ".");
  });
  loadReviews();
})();
