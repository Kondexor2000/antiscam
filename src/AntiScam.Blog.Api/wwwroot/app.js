const postsElement = document.querySelector("#posts");
const form = document.querySelector("#post-form");
const statusElement = document.querySelector("#form-status");
const workspaceElement = document.querySelector("#workspace");
const postsHeadingElement = document.querySelector("#posts-heading");
const showAllPosts = new URLSearchParams(window.location.search).get("view") === "all";
const authForm = document.querySelector("#auth-form");
const authStatusElement = document.querySelector("#auth-status");
const logoutButton = document.querySelector("#logout-button");
const adminPanel = document.querySelector("#admin-panel");
const adminStatusElement = document.querySelector("#admin-status");
const adminUsersElement = document.querySelector("#admin-users");
const adminPostsElement = document.querySelector("#admin-posts");
let currentUser = JSON.parse(sessionStorage.getItem("antiscamUser") ?? "null");

authForm.addEventListener("submit", async event => {
  event.preventDefault();
  const action = event.submitter?.dataset.action;
  if (!action) { authStatusElement.textContent = "Wybierz logowanie lub rejestrację."; return; }
  const endpoint = action === "register" ? "/api/auth/register" : "/api/auth/login";
  authStatusElement.textContent = action === "register" ? "Rejestrowanie..." : "Logowanie...";
  const response = await fetch(endpoint, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(Object.fromEntries(new FormData(authForm).entries())) });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    const validationMessage = problem?.errors ? Object.values(problem.errors).flat().join(" ") : null;
    authStatusElement.textContent = validationMessage ?? (response.status === 401 ? "Nieprawidłowa nazwa użytkownika lub hasło." : response.status === 403 ? "Konto jest zablokowane." : response.status === 409 ? "Ta nazwa użytkownika jest już zajęta." : `Operacja nie powiodła się (HTTP ${response.status}).`);
    return;
  }
  const result = await response.json();
  if (action === "register") { authStatusElement.textContent = `Konto ${result.userName} utworzone. Zaloguj się.`; return; }
  sessionStorage.setItem("antiscamAccessToken", result.accessToken);
  currentUser = result.user;
  sessionStorage.setItem("antiscamUser", JSON.stringify(currentUser));
  authStatusElement.textContent = `Zalogowano: ${currentUser.userName} (${currentUser.role}).`;
  refreshAuthorModes();
  await loadAdminPanel();
});

logoutButton.addEventListener("click", async () => {
  const token = sessionStorage.getItem("antiscamAccessToken");
  if (!token) { authStatusElement.textContent = "Nie jesteś zalogowany."; return; }
  const response = await fetch("/api/auth/logout", { method: "POST", headers: { Authorization: `Bearer ${token}` } });
  if (!response.ok) { authStatusElement.textContent = `Wylogowanie nie powiodło się (HTTP ${response.status}).`; return; }
  sessionStorage.removeItem("antiscamAccessToken");
  sessionStorage.removeItem("antiscamUser");
  currentUser = null;
  adminPanel.hidden = true;
  authForm.reset();
  authStatusElement.textContent = "Wylogowano.";
  refreshAuthorModes();
});

async function loadAdminPanel() {
  if (currentUser?.role !== "Admin") { adminPanel.hidden = true; return; }
  const token = sessionStorage.getItem("antiscamAccessToken");
  const headers = { Authorization: `Bearer ${token}` };
  const [usersResponse, postsResponse] = await Promise.all([fetch("/api/admin/users", { headers }), fetch("/api/admin/posts", { headers })]);
  if (!usersResponse.ok || !postsResponse.ok) { adminPanel.hidden = true; return; }
  const [users, posts] = await Promise.all([usersResponse.json(), postsResponse.json()]);
  adminUsersElement.innerHTML = users.map(user => `<div class="admin-row"><span>${escapeHtml(user.userName)} · ${escapeHtml(user.role)}${user.isBlocked ? " · zablokowany" : ""}</span>${user.role === "Admin" ? "" : user.isBlocked ? `<button type="button" data-unblock-user="${user.id}">Odblokuj</button>` : `<button type="button" data-block-user="${user.id}">Zablokuj</button>`}</div>`).join("") || "Brak użytkowników.";
  adminPostsElement.innerHTML = posts.map(post => `<div class="admin-row"><span>${escapeHtml(post.title)}${post.isActive ? "" : " · nieaktywny"}</span><button type="button" data-post-action="${post.isActive ? "deactivate" : "restore"}" data-post-id="${post.id}">${post.isActive ? "Ukryj" : "Przywróć"}</button></div>`).join("") || "Brak postów.";
  adminPanel.hidden = false;
}

adminPanel.addEventListener("click", async event => {
  const token = sessionStorage.getItem("antiscamAccessToken");
  const blockButton = event.target.closest("[data-block-user]");
  const unblockButton = event.target.closest("[data-unblock-user]");
  const postButton = event.target.closest("[data-post-action]");
  if (!token || (!blockButton && !unblockButton && !postButton)) return;
  const path = blockButton ? `/api/admin/users/${blockButton.dataset.blockUser}/block` : unblockButton ? `/api/admin/users/${unblockButton.dataset.unblockUser}/unblock` : `/api/admin/posts/${postButton.dataset.postId}/${postButton.dataset.postAction}`;
  const response = await fetch(path, { method: "POST", headers: { Authorization: `Bearer ${token}` } });
  adminStatusElement.textContent = response.ok ? "Zapisano zmianę." : `Operacja nie powiodła się (HTTP ${response.status}).`;
  if (response.ok) { await loadAdminPanel(); await loadPosts(); }
});

async function loadWorkspace() {
  const response = await fetch("/api/workspace");
  const workspace = await response.json();
  workspaceElement.textContent = workspace.exists ? `Połączono z ${workspace.rootPath}` : `Brak folderu ${workspace.rootPath}`;
}

async function loadPosts() {
  const response = await fetch(showAllPosts ? "/api/posts" : "/api/posts/latest");
  if (!response.ok) { postsElement.textContent = "Nie udało się pobrać wpisów."; return; }
  const result = await response.json();
  const posts = showAllPosts ? result : [result];
  postsHeadingElement.textContent = showAllPosts ? "Wszystkie wpisy" : "Najnowszy wpis";
  postsElement.innerHTML = "";
  for (const post of posts) {
    const article = document.createElement("article");
    article.className = "post";
    article.innerHTML = `
      <h3>${escapeHtml(post.title)}</h3>
      <p class="meta">${escapeHtml(post.author)} · ${new Date(post.publishedAt).toLocaleString("pl-PL")}</p>
      <p><strong>${escapeHtml(post.summary)}</strong></p>
      <p class="content">${escapeHtml(post.content)}</p>
      <section class="comments" aria-label="Komentarze">
        <h4>Komentarze</h4><div class="comment-list" data-comment-list></div>
        <form class="comment-form" data-post-id="${post.id}">
          <label>Treść komentarza<textarea name="content" required maxlength="2000"></textarea></label>
          <fieldset class="publication-identity">
            <legend>Publikuj jako</legend>
            <label><input type="radio" name="author-mode" value="guest" checked> Gość</label>
            <label><input type="radio" name="author-mode" value="user" disabled> Zalogowany użytkownik</label>
          </fieldset>
          <label><span class="author-label">Nazwa gościa</span><input name="author" required maxlength="100" value="Czytelnik"></label>
          <button type="submit">Dodaj komentarz</button><p class="comment-status" role="status"></p>
        </form>
      </section>`;
    postsElement.appendChild(article);
    configureAuthorMode(article.querySelector(".comment-form"));
    await loadComments(post.id, article);
  }
}

async function loadComments(postId, article) {
  const response = await fetch(`/api/posts/${postId}/comments`);
  const list = article.querySelector("[data-comment-list]");
  if (!response.ok) { list.textContent = "Nie udało się pobrać komentarzy."; return; }
  const comments = await response.json();
  list.innerHTML = comments.length === 0 ? "Brak komentarzy. Dodaj pierwszy." : comments.map(comment => `<article class="comment"><p class="meta">${escapeHtml(comment.author)} · ${new Date(comment.publishedAt).toLocaleString("pl-PL")}</p><p>${escapeHtml(comment.content)}</p></article>`).join("");
}

form.addEventListener("submit", async event => {
  event.preventDefault();
  statusElement.textContent = "Publikowanie...";
  const response = await fetch("/api/posts", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(postPayload(form)) });
  if (!response.ok) {
    if (response.status === 422) { const problem = await response.json(); statusElement.textContent = problem.aiExplanation ?? problem.risk.blockExplanation; return; }
    statusElement.textContent = "Nie udało się opublikować wpisu.";
    return;
  }
  form.reset();
  configureAuthorMode(form);
  statusElement.textContent = "Wpis opublikowany.";
  await loadPosts();
});

postsElement.addEventListener("submit", async event => {
  const commentForm = event.target.closest(".comment-form");
  if (!commentForm) return;
  event.preventDefault();
  const status = commentForm.querySelector(".comment-status");
  status.textContent = "Publikowanie komentarza...";
  const response = await fetch(`/api/posts/${commentForm.dataset.postId}/comments`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(commentPayload(commentForm)) });
  if (!response.ok) {
    if (response.status === 422) { const problem = await response.json(); status.textContent = problem.risk?.blockExplanation ?? "Komentarz został zablokowany przez ochronę antyscamową."; return; }
    status.textContent = "Nie udało się opublikować komentarza.";
    return;
  }
  commentForm.reset();
  configureAuthorMode(commentForm);
  status.textContent = "Komentarz opublikowany.";
  await loadComments(Number(commentForm.dataset.postId), commentForm.closest(".post"));
});

function postPayload(postForm) {
  return { title: postForm.elements.title.value, summary: postForm.elements.summary.value, content: postForm.elements.content.value, author: publicationAuthor(postForm) };
}

function commentPayload(commentForm) {
  return { content: commentForm.elements.content.value, author: publicationAuthor(commentForm) };
}

function publicationAuthor(publicationForm) {
  return selectedAuthorMode(publicationForm) === "user" && currentUser ? currentUser.userName : publicationForm.elements.author.value;
}

function selectedAuthorMode(publicationForm) {
  return publicationForm.querySelector("[name$='author-mode']:checked").value;
}

function configureAuthorMode(publicationForm) {
  const userMode = publicationForm.querySelector("[value=user]");
  const guestMode = publicationForm.querySelector("[value=guest]");
  const author = publicationForm.elements.author;
  const label = publicationForm.querySelector(".author-label");
  userMode.disabled = !currentUser;
  if (!currentUser) guestMode.checked = true;
  const update = () => {
    const publishingAsUser = selectedAuthorMode(publicationForm) === "user" && currentUser;
    author.value = publishingAsUser ? currentUser.userName : (author.value || "Czytelnik");
    author.readOnly = publishingAsUser;
    label.textContent = publishingAsUser ? "Autor (zalogowany użytkownik)" : "Nazwa gościa";
  };
  publicationForm.querySelectorAll("[name$='author-mode']").forEach(input => input.addEventListener("change", update));
  update();
}

function refreshAuthorModes() {
  configureAuthorMode(form);
  postsElement.querySelectorAll(".comment-form").forEach(configureAuthorMode);
}

function escapeHtml(value) {
  return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#039;");
}

configureAuthorMode(form);
if (currentUser) authStatusElement.textContent = `Zalogowano: ${currentUser.userName} (${currentUser.role}).`;
loadAdminPanel();
loadWorkspace();
loadPosts();
