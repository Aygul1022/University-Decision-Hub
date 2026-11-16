// The homepage remains readable and navigable when scripting is unavailable.
(function () {
  document.querySelectorAll("[data-members-path]").forEach((link) => {
    link.href = Site.membersUrl(link.dataset.membersPath);
  });

  const preference = window.matchMedia("(prefers-reduced-motion: reduce)");
  const toggle = document.querySelector(".home-motion-toggle");
  let userPaused = false;
  let observer;
  function updateMotion() {
    const paused = preference.matches || userPaused;
    document.body.classList.toggle("home-motion-paused", paused);
    toggle.hidden = preference.matches;
    toggle.setAttribute("aria-pressed", String(userPaused));
    toggle.textContent = userPaused ? "Resume motion" : "Pause motion";
    if (paused) {
      document.querySelectorAll(".home-reveal").forEach((section) => section.classList.add("is-visible"));
    }
  }
  toggle.addEventListener("click", () => { userPaused = !userPaused; updateMotion(); });
  preference.addEventListener("change", updateMotion);
  updateMotion();

  if (!preference.matches && "IntersectionObserver" in window) {
    observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.08 });
    document.querySelectorAll(".home-reveal").forEach((section) => {
      observer.observe(section);
      section.classList.add("home-reveal-ready");
    });
  }
})();
