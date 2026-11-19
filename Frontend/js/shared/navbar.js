// Shared header, navigation and login-state display. Requires js/shared/api.js first:
//   <script src="../../js/shared/api.js"></script>
//   <script src="../../js/shared/navbar.js"></script>
// The header is inserted at the top of <body>; pages do not write any navbar HTML.
//
// `Site` builds links that work from any folder depth:
//   Site.url("Pages/reviews/instructors.html")       -> link to a page, given its path from Frontend/
//   Site.membersUrl("Pages/reviews/reviews.html")    -> the page itself when logged in, otherwise
//                                                       the login page, which returns there afterwards
const Site = (function () {
  const root = document.currentScript.src.replace(/js\/shared\/navbar\.js.*$/, "");

  function url(path) {
    return root + path;
  }

  function loginUrl(nextPath) {
    return url("Pages/auth/login.html") + (nextPath ? "?next=" + encodeURIComponent(nextPath) : "");
  }

  function membersUrl(path) {
    return Session.isLoggedIn ? url(path) : loginUrl(path);
  }

  // Only a page inside this site may be used as the "return here after login" target.
  function isSafeNextPath(path) {
    return typeof path === "string"
      && /^[A-Za-z0-9_\-\/.]+\.html(\?[A-Za-z0-9_\-=&%.+]*)?$/.test(path)
      && !path.startsWith("/")
      && !path.includes("..")
      && !path.includes("//");
  }

  return { root, url, loginUrl, membersUrl, isSafeNextPath };
})();

(function () {
  // Full reviews are for registered users (FR01), so those two links go through login when logged out.
  const LINKS = [
    { label: "Read Reviews", path: "Pages/reviews/reviews.html", membersOnly: true },
    { label: "Write a Review", path: "Pages/reviews/review-form.html", membersOnly: true },
    { label: "Academic Staff", path: "Pages/reviews/instructors.html", membersOnly: false },
  ];

  function element(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  }

  function listItem(child) {
    const item = document.createElement("li");
    item.appendChild(child);
    return item;
  }

  function link(label, href, className) {
    const anchor = element("a", className, label);
    anchor.href = href;
    return anchor;
  }

  function render() {
    const header = element("header", "navbar");
    const inner = element("div", "navbar-inner");
    header.appendChild(inner);

    const brand = link("", Site.url("index.html"), "navbar-brand");
    brand.setAttribute("aria-label", "University Decision Hub home");
    const logo = element("img");
    logo.src = Site.url("assets/hub-book-logo.png");
    logo.alt = "HUB";
    logo.width = 94;
    logo.height = 49;
    brand.appendChild(logo);


    const nav = element("nav");
    nav.setAttribute("aria-label", "Main");
    const drawer = element("dialog", "navbar-drawer");
    drawer.id = "site-navigation";
    drawer.setAttribute("aria-label", "Site navigation");
    const close = element("button", "navbar-close", "Close menu");
    close.type = "button";
    drawer.appendChild(close);
    const toggle = element("button", "navbar-toggle");
    toggle.type = "button";
    toggle.setAttribute("aria-label", "Open navigation menu");
    toggle.setAttribute("aria-controls", drawer.id);
    toggle.setAttribute("aria-expanded", "false");
    for (let i = 0; i < 3; i++) toggle.appendChild(element("span"));
    inner.appendChild(toggle);
    toggle.addEventListener("click", () => {
      drawer.showModal();
      toggle.setAttribute("aria-expanded", "true");
    });
    close.addEventListener("click", () => drawer.close());
    drawer.addEventListener("click", (event) => {
      if (event.target !== drawer) return;
      const bounds = drawer.getBoundingClientRect();
      if (event.clientX > bounds.right || event.clientY > bounds.bottom || event.clientX < bounds.left || event.clientY < bounds.top) drawer.close();
    });
    drawer.addEventListener("close", () => {
      toggle.setAttribute("aria-expanded", "false");
      toggle.focus();
    });
    const links = element("ul", "navbar-links");
    for (const entry of LINKS) {
      const anchor = link(entry.label, entry.membersOnly ? Site.membersUrl(entry.path) : Site.url(entry.path));
      if (window.location.href.split(/[?#]/)[0] === Site.url(entry.path)) anchor.setAttribute("aria-current", "page");
      links.appendChild(listItem(anchor));
    }
    nav.appendChild(links);
    drawer.appendChild(nav);
    header.appendChild(drawer);

    const account = element("ul", "navbar-account");
    const user = Session.user;
    if (user) {
      // The name opens the user's own profile page.
      const name = link(user.fullName, Site.url("Pages/auth/profile.html"), "navbar-user");
      if (user.isAdministrator) name.appendChild(element("span", "navbar-role", "Administrator"));
      account.appendChild(listItem(name));

      const logout = element("button", "", "Log out");
      logout.type = "button";
      logout.addEventListener("click", () => {
        Session.clear();
        window.location.href = Site.url("index.html");
      });
      account.appendChild(listItem(logout));
    } else {
      account.appendChild(listItem(link("Log in", Site.url("Pages/auth/login.html"))));
      account.appendChild(listItem(link("Register", Site.url("Pages/auth/register.html"), "navbar-register")));
    }
    inner.appendChild(account);
    inner.appendChild(brand);

    document.body.prepend(header);
  }

  if (document.body) render();
  else document.addEventListener("DOMContentLoaded", render);
})();
