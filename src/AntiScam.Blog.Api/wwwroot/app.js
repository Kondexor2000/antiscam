const postsElement = document.querySelector("#posts");
const form = document.querySelector("#post-form");
const statusElement = document.querySelector("#form-status");
const workspaceElement = document.querySelector("#workspace");

async function loadWorkspace() {
  const response = await fetch("/api/workspace");
  const workspace = await response.json();
  workspaceElement.textContent = workspace.exists
    ? `Połączono z ${workspace.rootPath}`
    : `Brak folderu ${workspace.rootPath}`;
}

async function loadPosts() {
  const response = await fetch("/api/posts");
  const posts = await response.json();

  postsElement.innerHTML = "";
  for (const post of posts) {
    const article = document.createElement("article");
    article.className = "post";
    article.innerHTML = `
      <h3>${escapeHtml(post.title)}</h3>
      <p class="meta">${escapeHtml(post.author)} · ${new Date(post.publishedAt).toLocaleString("pl-PL")}</p>
      <p><strong>${escapeHtml(post.summary)}</strong></p>
      <p class="content">${escapeHtml(post.content)}</p>
    `;
    postsElement.appendChild(article);
  }
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  statusElement.textContent = "Publikowanie...";

  const payload = Object.fromEntries(new FormData(form).entries());
  const response = await fetch("/api/posts", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    if (response.status === 422) {
      const problem = await response.json();
      const explanation = problem.aiExplanation
        ?? problem.risk.blockExplanation
        ?? `Nie opublikowano: wykryto ryzyko ${problem.risk.status} (${problem.risk.riskScore}/100).`;
      statusElement.textContent = explanation;
      return;
    }

    statusElement.textContent = "Nie udało się opublikować wpisu.";
    return;
  }

  form.reset();
  document.querySelector("#author").value = "AntiScam Team";
  statusElement.textContent = "Wpis opublikowany.";
  await loadPosts();
});

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

loadWorkspace();
loadPosts();
