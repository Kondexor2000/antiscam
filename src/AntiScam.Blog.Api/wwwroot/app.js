const apiRows = document.querySelector("#api-rows");
const toast = document.querySelector("#toast");
const metrics = document.querySelector(".metrics");
const apiDialog = document.querySelector("#api-dialog");
const apiForm = document.querySelector("#api-form");
const keyDialog = document.querySelector("#key-dialog");
const requestDialog = document.querySelector("#request-dialog");
const requestForm = document.querySelector("#request-form");
const adminKeyStorageKey = "antiscam-control-plane-key";
let adminKey = sessionStorage.getItem(adminKeyStorageKey);
let authCancelled = false;
let editingApiId = null;

document.querySelector(".live-note").textContent =
  "Ruch Blog API i gatewaya jest liczony osobno od uruchomienia procesu; metryki nie są współdzielone między instancjami.";
document.querySelector(".gateway-live-pill").innerHTML = "<i></i> GATEWAY · AKTYWNY";
document.querySelector(".metrics").setAttribute("aria-label", "Rzeczywiste metryki Blog API i gatewaya");
document.querySelector(".content-grid")?.remove();
document.querySelector(".events-panel")?.remove();
document.querySelector(".bottom-grid").style.gridTemplateColumns = "1fr";
document.querySelector(".api-panel h2").textContent = "Zarejestrowane API";
document.querySelector(".api-panel .panel-header p").textContent = "Konfiguracja upstreamów i metryki od uruchomienia procesu";
document.querySelector(".api-panel .text-button")?.remove();
document.querySelector(".api-panel thead tr").insertAdjacentHTML("beforeend", "<th>GATEWAY</th><th></th>");
document.querySelector(".region b").textContent = "Gateway uruchomiony";
document.querySelector(".region small").textContent = "Metryki bieżącego procesu";
document.querySelector("footer span:nth-child(2)").textContent = "Metryki procesu · MS-SQL";
document.querySelector(".org-switch b").textContent = "AntiScam";
document.querySelector(".org-switch small").textContent = "Pojedyncza instancja · MS-SQL";
document.querySelector(".user b").textContent = "Administrator";
document.querySelector(".user small").textContent = "Control Plane";
document.querySelector(".avatar").textContent = "CP";
document.querySelector("#page-title").textContent = "Control Plane";
document.querySelector("#page-kicker").textContent = `API GATEWAY · ${new Date().toLocaleDateString("pl-PL")}`;
document.querySelector(".lower-label").nextElementSibling.remove();
document.querySelector(".lower-label").remove();
document.querySelector(".nav-count").setAttribute("aria-label", "Liczba zarejestrowanych API");

async function requestAdmin(url, options = {}) {
  if (!adminKey) {
    if (authCancelled) throw new Error("Wymagany klucz administratora. Odśwież stronę po skonfigurowaniu dostępu.");
    adminKey = window.prompt("Podaj klucz administratora Control Plane:");
    if (!adminKey) {
      authCancelled = true;
      throw new Error("Dostęp wymaga klucza administratora.");
    }
    sessionStorage.setItem(adminKeyStorageKey, adminKey);
  }

  const headers = new Headers(options.headers || {});
  headers.set("X-Control-Plane-Key", adminKey);
  const response = await fetch(url, { ...options, headers });
  if (response.status === 401) {
    adminKey = null;
    authCancelled = true;
    sessionStorage.removeItem(adminKeyStorageKey);
    throw new Error("Klucz administratora jest nieprawidłowy. Odśwież stronę i spróbuj ponownie.");
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    const validationErrors = Object.entries(problem.errors || {})
      .map(([field, messages]) => `${field}: ${messages.join(" ")}`);
    throw new Error(problem.detail || problem.message || validationErrors.join(" ") || `Żądanie nie powiodło się (${response.status}).`);
  }
  if (response.status === 204) return null;
  return response.json();
}

async function loadApis() {
  try {
    const apis = await requestAdmin("/api/gateway/apis");
    apiRows.innerHTML = apis.length
      ? apis.map(api => `
        <tr>
          <td><span class="api-name"><i class="api-symbol">◈</i>${escapeHtml(api.name)}</span><small class="api-target">${escapeHtml(api.upstreamUrl)}</small></td>
          <td><span class="api-version">${escapeHtml(api.version)}</span></td>
          <td>${Number(api.requests).toLocaleString("pl-PL")}</td>
          <td>${api.averageLatencyMs == null ? "—" : `${Number(api.averageLatencyMs).toFixed(1)} ms`}</td>
          <td><span class="status-label"><i></i>${escapeHtml(api.status)}</span></td>
          <td><code>${escapeHtml(api.gatewayUrl)}</code></td>
          <td class="api-actions">
            <button class="text-button" data-test="${escapeHtml(api.id)}">Testuj</button>
            <button class="text-button" data-edit="${escapeHtml(api.id)}">Edytuj</button>
            <button class="text-button" data-rotate="${escapeHtml(api.id)}">Nowy klucz</button>
            <button class="text-button" data-delete="${escapeHtml(api.id)}">Usuń</button>
          </td>
        </tr>`).join("")
      : '<tr><td colspan="7" class="loading">Brak zarejestrowanych API. Wybierz „Dodaj API”, aby skonfigurować pierwszą usługę.</td></tr>';
    document.querySelector(".nav-count").textContent = apis.length;
    apiRows.querySelectorAll("[data-test]").forEach(button => button.addEventListener("click", () => testApi(apis.find(api => api.id === button.dataset.test))));
    apiRows.querySelectorAll("[data-edit]").forEach(button => button.addEventListener("click", () => editApi(apis.find(api => api.id === button.dataset.edit))));
    apiRows.querySelectorAll("[data-rotate]").forEach(button => button.addEventListener("click", () => rotateApiKey(button.dataset.rotate)));
    apiRows.querySelectorAll("[data-delete]").forEach(button => button.addEventListener("click", () => deleteApi(button.dataset.delete)));
  } catch (error) {
    apiRows.innerHTML = `<tr><td colspan="7" class="loading">${escapeHtml(error.message)}</td></tr>`;
  }
}

async function loadMetrics() {
  try {
    const [gateway, blog] = await Promise.all([
      requestAdmin("/api/gateway/metrics"),
      requestAdmin("/api/blog/metrics"),
    ]);
    const averageLatency = Number(gateway.averageLatencyMs);
    metrics.innerHTML = `
      <article class="metric-card"><div class="metric-top"><span>ZAREJESTROWANE API</span><span class="metric-icon blue">⌘</span></div><div class="metric-value">${document.querySelector(".nav-count").textContent}</div><div class="metric-foot">Trwale zapisane w MS-SQL</div></article>
      <article class="metric-card"><div class="metric-top"><span>ŻĄDANIA GATEWAYA</span><span class="metric-icon violet">↗</span></div><div class="metric-value">${Number(gateway.requests).toLocaleString("pl-PL")}</div><div class="metric-foot">Przekazane do upstreamów od ${new Date(gateway.startedAt).toLocaleTimeString("pl-PL")}</div></article>
      <article class="metric-card"><div class="metric-top"><span>ŻĄDANIA BLOG API</span><span class="metric-icon blue">▤</span></div><div class="metric-value">${Number(blog.requests).toLocaleString("pl-PL")}</div><div class="metric-foot">Wpisy i komentarze od ${new Date(blog.startedAt).toLocaleTimeString("pl-PL")}</div></article>
      <article class="metric-card"><div class="metric-top"><span>ŚREDNIE OPÓŹNIENIE</span><span class="metric-icon green">◷</span></div><div class="metric-value">${averageLatency.toFixed(1)}<span class="metric-unit"> ms</span></div><div class="metric-foot">Czas obsługi po stronie gatewaya</div></article>
      <article class="metric-card"><div class="metric-top"><span>BŁĘDY GATEWAYA</span><span class="metric-icon amber">!</span></div><div class="metric-value">${Number(gateway.failedRequests).toLocaleString("pl-PL")}</div><div class="metric-foot">Odpowiedzi HTTP 4xx i 5xx</div></article>`;
  } catch (error) {
    notify(error.message);
  }
}

async function addApi() {
  editingApiId = null;
  apiForm.reset();
  apiForm.elements.version.value = "v1";
  apiForm.elements.enabled.checked = true;
  document.querySelector("#api-dialog-title").textContent = "Dodaj API";
  document.querySelector("#api-form-submit").textContent = "Zarejestruj API";
  clearApiFormError();
  apiDialog.showModal();
  apiForm.elements.name.focus();
}

async function editApi(api) {
  if (!api) return;
  editingApiId = api.id;
  apiForm.elements.name.value = api.name;
  apiForm.elements.version.value = api.version;
  apiForm.elements.upstreamUrl.value = api.upstreamUrl;
  apiForm.elements.enabled.checked = api.status === "Aktywne";
  document.querySelector("#api-dialog-title").textContent = "Edytuj API";
  document.querySelector("#api-form-submit").textContent = "Zapisz zmiany";
  clearApiFormError();
  apiDialog.showModal();
  apiForm.elements.name.focus();
}

apiForm.addEventListener("submit", async event => {
  event.preventDefault();
  clearApiFormError();
  const submit = document.querySelector("#api-form-submit");
  submit.disabled = true;
  const input = {
    name: apiForm.elements.name.value.trim(),
    version: apiForm.elements.version.value.trim(),
    upstreamUrl: apiForm.elements.upstreamUrl.value.trim(),
    enabled: apiForm.elements.enabled.checked,
  };

  try {
    const creating = editingApiId === null;
    const result = await requestAdmin(
      creating ? "/api/gateway/apis" : `/api/gateway/apis/${encodeURIComponent(editingApiId)}`,
      {
        method: creating ? "POST" : "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(input),
      },
    );
    apiDialog.close();
    await refreshDashboard();
    if (creating) showApiKey(result.apiKey, "API zarejestrowane");
    else notify("Konfiguracja API została zapisana.");
  } catch (error) {
    document.querySelector("#api-form-error").textContent = error.message;
    document.querySelector("#api-form-error").hidden = false;
  } finally {
    submit.disabled = false;
  }
});

function clearApiFormError() {
  const error = document.querySelector("#api-form-error");
  error.textContent = "";
  error.hidden = true;
}

function showApiKey(apiKey, title) {
  document.querySelector("#key-dialog-title").textContent = title;
  document.querySelector("#generated-api-key").textContent = apiKey;
  keyDialog.showModal();
}

function testApi(api) {
  if (!api) return;
  requestForm.reset();
  requestForm.elements.method.value = "GET";
  requestForm.elements.path.value = "/";
  requestForm.elements.contentType.value = "application/json";
  document.querySelector("#request-gateway-url").textContent = api.gatewayUrl;
  document.querySelector("#request-dialog-title").textContent = `Testuj: ${api.name}`;
  document.querySelector("#request-form-error").hidden = true;
  document.querySelector("#request-form-error").textContent = "";
  document.querySelector("#request-response").hidden = true;
  requestDialog.dataset.apiId = api.id;
  requestDialog.showModal();
  requestForm.elements.apiKey.focus();
}

requestForm.addEventListener("submit", async event => {
  event.preventDefault();
  const errorElement = document.querySelector("#request-form-error");
  const resultElement = document.querySelector("#request-response");
  errorElement.hidden = true;
  resultElement.hidden = true;

  const apiId = requestDialog.dataset.apiId;
  const method = requestForm.elements.method.value;
  const path = requestForm.elements.path.value.trim();
  const clientKey = requestForm.elements.apiKey.value.trim();
  if (!clientKey) {
    errorElement.textContent = "Wpisz klucz klienta API (X-API-Key).";
    errorElement.hidden = false;
    return;
  }
  if (!path || path.startsWith("//") || /^[a-z][a-z\d+.-]*:/i.test(path) || path.includes("#")) {
    errorElement.textContent = "Podaj ścieżkę względną, np. /scan albo /items?id=1. Adres URL i fragment # nie są dozwolone.";
    errorElement.hidden = false;
    return;
  }

  const body = requestForm.elements.body.value;
  if (body && ["GET", "HEAD"].includes(method)) {
    errorElement.textContent = `Metoda ${method} nie obsługuje treści żądania.`;
    errorElement.hidden = false;
    return;
  }

  const headers = new Headers({ "X-API-Key": clientKey });
  const contentType = requestForm.elements.contentType.value.trim();
  try {
    if (body && contentType) headers.set("Content-Type", contentType);
  } catch {
    errorElement.textContent = "Nieprawidłowa wartość Content-Type.";
    errorElement.hidden = false;
    return;
  }
  const protectedHeaders = new Set([
    "connection", "content-length", "cookie", "host", "keep-alive", "origin", "referer", "proxy-authenticate",
    "proxy-authorization", "te", "trailer", "transfer-encoding", "upgrade",
    "x-api-key", "x-control-plane-key",
  ]);
  for (const line of requestForm.elements.headers.value.split(/\r?\n/)) {
    if (!line.trim()) continue;
    const separator = line.indexOf(":");
    if (separator < 1) {
      errorElement.textContent = `Nieprawidłowy nagłówek: ${line}`;
      errorElement.hidden = false;
      return;
    }
    const name = line.slice(0, separator).trim();
    const value = line.slice(separator + 1).trim();
    if (protectedHeaders.has(name.toLowerCase())) {
      errorElement.textContent = `Nagłówka ${name} nie można ustawić ręcznie.`;
      errorElement.hidden = false;
      return;
    }
    try {
      headers.set(name, value);
    } catch {
      errorElement.textContent = `Nieprawidłowy nagłówek: ${name}`;
      errorElement.hidden = false;
      return;
    }
  }

  const gatewayPrefix = `/gateway/${encodeURIComponent(apiId)}/`;
  const requestPath = path.replace(/^\/+/, "");
  let target;
  try {
    const pathOnly = requestPath.split("?", 1)[0];
    if (pathOnly.includes("\\")) throw new Error("Backslash is not allowed.");
    for (const segment of pathOnly.split("/")) {
      const decodedSegment = decodeURIComponent(segment);
      if (decodedSegment === "." || decodedSegment === "..") throw new Error("Path traversal is not allowed.");
    }
    target = new URL(`${gatewayPrefix}${requestPath}`, window.location.origin);
  } catch {
    errorElement.textContent = "Nieprawidłowa ścieżka. Użyj względnej ścieżki upstream bez segmentów . lub ...";
    errorElement.hidden = false;
    return;
  }
  if (target.origin !== window.location.origin || !target.pathname.startsWith(gatewayPrefix)) {
    errorElement.textContent = "Ścieżka musi pozostać w obrębie wybranego API Gateway.";
    errorElement.hidden = false;
    return;
  }

  const submit = document.querySelector("#request-submit");
  submit.disabled = true;
  submit.textContent = "Wysyłanie…";
  document.querySelector("#request-response-status").textContent = "";
  document.querySelector("#request-response-meta").textContent = "";
  document.querySelector("#request-response-body").textContent = "";
  document.querySelector("#request-response-headers pre").textContent = "";
  const startedAt = performance.now();
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 30000);
  try {
    const response = await fetch(target, {
      method,
      headers,
      body: body || undefined,
      signal: controller.signal,
    });
    const elapsedMs = performance.now() - startedAt;
    const responseBody = method === "HEAD" || response.status === 204 ? "" : await response.text();
    const responseStatus = document.querySelector("#request-response-status");
    responseStatus.textContent = `${response.status} ${response.statusText}`;
    responseStatus.className = response.ok ? "response-success" : "response-failure";
    document.querySelector("#request-response-meta").textContent = `${method} ${target.pathname}${target.search} · ${elapsedMs.toFixed(0)} ms`;
    document.querySelector("#request-response-headers pre").textContent =
      [...response.headers].map(([name, value]) => `${name}: ${value}`).join("\n") || "Brak nagłówków.";
    document.querySelector("#request-response-body").textContent = formatResponseBody(responseBody, response.headers.get("content-type"));
    resultElement.hidden = false;
  } catch (error) {
    errorElement.textContent = error.name === "AbortError"
      ? "Upłynął limit czasu żądania (30 s)."
      : `Nie udało się wykonać żądania: ${error.message}`;
    errorElement.hidden = false;
  } finally {
    clearTimeout(timeoutId);
    submit.disabled = false;
    submit.textContent = "Wyślij żądanie";
  }
});

function formatResponseBody(body, contentType) {
  if (!body) return "(pusta odpowiedź)";
  if (contentType?.includes("json")) {
    try {
      return JSON.stringify(JSON.parse(body), null, 2);
    } catch {
      return body;
    }
  }
  return body;
}

async function rotateApiKey(id) {
  if (!window.confirm("Wygenerować nowy klucz? Dotychczasowy klucz przestanie działać.")) return;
  try {
    const result = await requestAdmin(`/api/gateway/apis/${encodeURIComponent(id)}/rotate-key`, { method: "POST" });
    showApiKey(result.apiKey, "Nowy klucz API");
  } catch (error) {
    notify(error.message);
  }
}

async function deleteApi(id) {
  if (!window.confirm("Usunąć API z katalogu? Jego trasa gatewaya przestanie działać.")) return;
  try {
    await requestAdmin(`/api/gateway/apis/${encodeURIComponent(id)}`, { method: "DELETE" });
    await refreshDashboard();
  } catch (error) {
    notify(error.message);
  }
}

async function refreshDashboard() {
  await loadApis();
  await loadMetrics();
}

document.querySelectorAll("[data-nav]").forEach(link => link.addEventListener("click", event => {
  event.preventDefault();
  document.querySelector(`[data-view="${link.dataset.nav}"]`)?.click();
  window.scrollTo({ top: 0, behavior: "smooth" });
}));

document.querySelector("#add-api").addEventListener("click", addApi);
document.querySelectorAll("[data-close-api-dialog]").forEach(button =>
  button.addEventListener("click", () => apiDialog.close()));
document.querySelectorAll("[data-close-key-dialog]").forEach(button =>
  button.addEventListener("click", () => keyDialog.close()));
document.querySelectorAll("[data-close-request-dialog]").forEach(button =>
  button.addEventListener("click", () => {
    requestForm.elements.apiKey.value = "";
    requestDialog.close();
  }));
requestDialog.addEventListener("close", () => {
  requestForm.elements.apiKey.value = "";
});
requestDialog.addEventListener("cancel", () => {
  requestForm.elements.apiKey.value = "";
});
requestDialog.addEventListener("keydown", event => {
  if (event.key === "Escape") {
    event.preventDefault();
    requestForm.elements.apiKey.value = "";
    requestDialog.close();
  }
});
document.querySelector("#copy-api-key").addEventListener("click", async () => {
  try {
    await navigator.clipboard.writeText(document.querySelector("#generated-api-key").textContent);
    notify("Klucz skopiowano do schowka.");
  } catch (error) {
    notify(`Nie udało się skopiować klucza: ${error.message}`);
  }
});
document.querySelector("#time-range")?.remove();

function notify(message) {
  toast.textContent = message;
  toast.classList.add("show");
  clearTimeout(notify.timer);
  notify.timer = setTimeout(() => toast.classList.remove("show"), 4500);
}

function escapeHtml(value) {
  return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#039;");
}

refreshDashboard();
setInterval(refreshDashboard, 5000);
