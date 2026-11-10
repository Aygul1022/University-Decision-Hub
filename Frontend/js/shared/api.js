// Shared API helper. Load this before any page script:
//   <script src="../../js/shared/api.js"></script>
//
// The backend address defaults to the "http" launch profile. To point a browser at another
// address (for example the https profile) run this once in the browser console:
//   localStorage.setItem("udh.apiBaseUrl", "https://localhost:7214")
const API_BASE_URL = localStorage.getItem("udh.apiBaseUrl") || "http://localhost:5154";

// The login state kept in this browser: the token from /api/auth/login and the user's profile.
const Session = {
  save(authResponse) {
    localStorage.setItem("udh.token", authResponse.token);
    localStorage.setItem("udh.tokenExpiresAtUtc", authResponse.expiresAtUtc);
    localStorage.setItem("udh.user", JSON.stringify(authResponse.user));
  },
  clear() {
    localStorage.removeItem("udh.token");
    localStorage.removeItem("udh.tokenExpiresAtUtc");
    localStorage.removeItem("udh.user");
  },
  get token() {
    const expires = Date.parse(localStorage.getItem("udh.tokenExpiresAtUtc") || "");
    if (!expires || expires <= Date.now()) return null;
    return localStorage.getItem("udh.token");
  },
  // { userId, fullName, email, departmentId, studyYear, isAdministrator } or null
  get user() {
    if (!this.token) return null;
    try {
      return JSON.parse(localStorage.getItem("udh.user"));
    } catch {
      return null;
    }
  },
  get isLoggedIn() {
    return this.token !== null;
  },
};

class ApiError extends Error {
  constructor(status, message, fieldErrors) {
    super(message);
    this.status = status;
    // { fieldName: ["message", ...] } for 400 validation responses, otherwise {}
    this.fieldErrors = fieldErrors || {};
  }
}

// apiFetch("/api/courses")                              -> GET, sends the login token if there is one
// apiFetch("/api/auth/login", { method: "POST", body }) -> body is sent as JSON
// Resolves with the parsed response; rejects with ApiError for any non-2xx answer.
async function apiFetch(path, options = {}) {
  const headers = { Accept: "application/json" };
  if (options.body !== undefined) headers["Content-Type"] = "application/json";
  if (Session.token) headers["Authorization"] = "Bearer " + Session.token;

  let response;
  try {
    response = await fetch(API_BASE_URL + path, {
      method: options.method || "GET",
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  } catch {
    throw new ApiError(0, "Could not reach the server at " + API_BASE_URL + ". Is the backend running?");
  }

  const text = await response.text();
  let data = text;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    // plain-text answer; keep it as it is
  }

  if (response.ok) return data;

  if (response.status === 429) {
    throw new ApiError(429, "Too many attempts. Please wait a minute and try again.");
  }

  let message = "Request failed (" + response.status + ").";
  let fieldErrors = {};
  if (typeof data === "string" && data) {
    message = data;
  } else if (data && typeof data === "object") {
    if (data.errors) {
      // ASP.NET Core validation answer: { errors: { FullName: ["..."] } } -> keys lower-camel-cased
      for (const [key, messages] of Object.entries(data.errors)) {
        fieldErrors[key.charAt(0).toLowerCase() + key.slice(1)] = messages;
      }
      message = "Please correct the highlighted fields.";
    } else if (data.title) {
      message = data.title;
    }
  }
  throw new ApiError(response.status, message, fieldErrors);
}
