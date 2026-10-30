if (location.protocol === "file:") location.replace("http://localhost:5050/admin.html");

const $ = id => document.getElementById(id), KEY = "admin_token";
const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const toast = m => { const t = document.createElement("div"); t.className = "toast"; t.textContent = m; document.body.appendChild(t); setTimeout(() => t.remove(), 2800); };

async function call(path, method = "GET", body) {
  const h = { "Content-Type": "application/json" }, t = localStorage.getItem(KEY);
  if (t) h.Authorization = "Bearer " + t;
  const r = await fetch(path, { method, headers: h, body: body === undefined ? undefined : JSON.stringify(body) });
  let d = null; try { d = await r.json(); } catch { }
  if ((r.status === 401 || r.status === 403) && path !== "/api/admin/login") { logout(); throw new Error("Session expired. Please sign in again."); }
  if (!r.ok) throw new Error(d?.message || "Request failed (" + r.status + ")");
  return d;
}

// field = [key, label, type]  (type: text | area | check)
const CFG = {
  mentors: { title: "Mentors", one: "mentor", path: "/api/admin/mentors", cols: ["name", "role", "company", "available"],
    fields: [["name", "Name"], ["role", "Job role"], ["company", "Company"], ["expertise", "Expertise (comma separated)"], ["bio", "Bio", "area"], ["available", "Available for sessions", "check"]] },
  categories: { title: "Career Categories", one: "category", path: "/api/admin/categories", cols: ["name", "description"],
    fields: [["name", "Name"], ["description", "Description", "area"]] },
  courses: { title: "Courses", one: "course", path: "/api/admin/courses", cols: ["name", "provider", "level", "duration"],
    fields: [["name", "Name"], ["provider", "Provider"], ["duration", "Duration"], ["level", "Level"], ["mode", "Mode"], ["description", "Description", "area"], ["url", "Course link"]] },
  colleges: { title: "Colleges", one: "college", path: "/api/admin/colleges", cols: ["name", "city", "type", "programs"],
    fields: [["name", "Name"], ["city", "City"], ["type", "Type"], ["programs", "Programs (comma separated)"], ["description", "Description", "area"]] }
};
const NAV = [["dash", "Dashboard & Reports"], ["students", "Students"], ["mentors", "Mentors"], ["categories", "Career Categories"],
  ["courses", "Courses"], ["colleges", "Colleges"], ["requests", "Mentor Requests"], ["notify", "Send Notifications"]];

let cur = "", items = [];

async function go(k) {
  cur = k;
  document.querySelectorAll("#nav button").forEach(b => b.classList.toggle("on", b.dataset.k === k));
  $("content").innerHTML = "<p>Loading…</p>";
  try { await (CFG[k] ? crud(k) : LOAD[k]()); }
  catch (e) { $("content").innerHTML = `<p class="err">${esc(e.message)}</p>`; }
}

async function dash() {
  const s = await call("/api/admin/stats"), max = Math.max(1, ...s.last7days.map(x => x.count));
  const card = (n, l) => `<div class="stat"><b>${n}</b><span>${l}</span></div>`;
  $("content").innerHTML = `<div class="head"><h2>Dashboard & Reports</h2></div>
    <div class="grid">${card(s.students, "Students") + card(s.mentors, "Mentors") + card(s.courses, "Courses") + card(s.colleges, "Colleges") + card(s.categories, "Career categories")}</div>
    <div class="grid">${card(s.pending, "Pending mentor requests") + card(s.approved, "Approved") + card(s.rejected, "Rejected")}</div>
    <div class="panel"><h3>New registrations, last 7 days</h3><div class="bars">${s.last7days.map(x => `<div><i style="height:${x.count / max * 100}px"></i>${x.count}<br>${esc(x.day)}</div>`).join("")}</div></div>`;
}

async function students() {
  items = await call("/api/admin/students");
  $("content").innerHTML = `<div class="head"><h2>Students (${items.length})</h2></div><div class="panel"><div class="wrap"><table>
    <tr><th>Name</th><th>Email</th><th>College</th><th>Career goal</th><th>Joined</th><th></th></tr>
    ${items.map(s => `<tr><td>${esc(s.name)}</td><td>${esc(s.email)}</td><td>${esc(s.college)}</td><td>${esc(s.careerGoal)}</td><td>${new Date(s.createdAt).toLocaleDateString()}</td>
    <td><button class="btn d" data-act="delstu" data-id="${s.id}">Delete</button></td></tr>`).join("")}</table></div></div>`;
}

async function requests() {
  items = await call("/api/admin/requests");
  $("content").innerHTML = `<div class="head"><h2>Mentor Requests (${items.length})</h2></div><div class="panel"><div class="wrap"><table>
    <tr><th>Student</th><th>Mentor</th><th>Requested</th><th>Status</th><th></th></tr>
    ${items.map(r => `<tr><td>${esc(r.student)}<br><small>${esc(r.studentEmail)}</small></td><td>${esc(r.mentor)}</td><td>${new Date(r.createdAt).toLocaleDateString()}</td>
    <td><span class="badge ${esc(r.status)}">${esc(r.status)}</span></td>
    <td>${r.status === "Pending" ? `<button class="btn g" data-act="set" data-id="${r.id}" data-s="Approved">Approve</button> <button class="btn d" data-act="set" data-id="${r.id}" data-s="Rejected">Reject</button>` : ""}</td></tr>`).join("")}</table></div></div>`;
}

async function notify() {
  const st = await call("/api/admin/students");
  $("content").innerHTML = `<div class="head"><h2>Send Notifications</h2></div><div class="panel" style="max-width:520px">
    <label>Send to</label><select id="nTo"><option value="">All students</option>${st.map(s => `<option value="${s.id}">${esc(s.name)} (${esc(s.email)})</option>`).join("")}</select>
    <label>Title</label><input id="nTitle"><label>Message</label><textarea id="nMsg"></textarea>
    <p><button class="btn p" data-act="send">Send notification</button></p></div>`;
}

const LOAD = { dash, students, requests, notify };

async function crud(k) {
  const c = CFG[k], lab = key => c.fields.find(f => f[0] === key)[1];
  items = await call(c.path);
  $("content").innerHTML = `<div class="head"><h2>${c.title} (${items.length})</h2><button class="btn p" data-act="add">Add ${c.one}</button></div>
    <div class="panel"><div class="wrap"><table><tr>${c.cols.map(x => `<th>${lab(x)}</th>`).join("")}<th></th></tr>
    ${items.map(i => `<tr>${c.cols.map(x => `<td>${typeof i[x] === "boolean" ? (i[x] ? "Yes" : "No") : esc(i[x])}</td>`).join("")}
    <td style="white-space:nowrap"><button class="btn" data-act="edit" data-id="${i.id}">Edit</button> <button class="btn d" data-act="del" data-id="${i.id}">Delete</button></td></tr>`).join("")}</table></div></div>`;
}

function openForm(id) {
  const c = CFG[cur], it = id ? items.find(x => x.id == id) : { available: true };
  $("modal").innerHTML = `<div class="mbox"><h3>${id ? "Edit" : "Add"} ${c.one}</h3>
    ${c.fields.map(([k, l, t]) => t === "check"
      ? `<label class="chk"><input type="checkbox" id="f_${k}" ${it[k] ? "checked" : ""}> ${l}</label>`
      : `<label>${l}</label>` + (t === "area" ? `<textarea id="f_${k}">${esc(it[k])}</textarea>` : `<input id="f_${k}" value="${esc(it[k])}">`)).join("")}
    <p><button class="btn p" data-act="save" data-id="${id || ""}">Save changes</button> <button class="btn" data-act="close">Cancel</button></p></div>`;
  $("modal").classList.remove("hidden");
}
const closeM = () => $("modal").classList.add("hidden");

async function save(id) {
  const c = CFG[cur], b = {};
  c.fields.forEach(([k, , t]) => { const e = $("f_" + k); b[k] = t === "check" ? e.checked : e.value.trim(); });
  if (!b[c.fields[0][0]]) { toast("Name is required."); return; }
  await call(c.path + (id ? "/" + id : ""), id ? "PUT" : "POST", b);
  closeM(); toast("Saved."); go(cur);
}

document.addEventListener("click", async e => {
  const b = e.target.closest("[data-act]"); if (!b) return;
  const a = b.dataset.act, id = b.dataset.id;
  try {
    if (a === "add") openForm();
    else if (a === "edit") openForm(id);
    else if (a === "close") closeM();
    else if (a === "save") await save(id);
    else if (a === "del") { if (confirm("Delete this record?")) { await call(CFG[cur].path + "/" + id, "DELETE"); toast("Deleted."); go(cur); } }
    else if (a === "delstu") { if (confirm("Delete this student and their requests?")) { await call("/api/admin/students/" + id, "DELETE"); toast("Student deleted."); go(cur); } }
    else if (a === "set") { await call("/api/admin/requests/" + id, "PUT", { status: b.dataset.s }); toast("Request " + b.dataset.s.toLowerCase() + "."); go(cur); }
    else if (a === "send") {
      const v = $("nTo").value;
      const d = await call("/api/admin/notifications", "POST", { studentId: v ? +v : null, title: $("nTitle").value, message: $("nMsg").value });
      toast(d.message); $("nTitle").value = ""; $("nMsg").value = "";
    }
    else if (a === "logout") logout();
  } catch (x) { toast(x.message); }
});

$("loginForm").onsubmit = async e => {
  e.preventDefault(); $("err").textContent = "";
  try { const d = await call("/api/admin/login", "POST", { email: $("em").value, password: $("pw").value }); localStorage.setItem(KEY, d.token); start(); }
  catch (x) { $("err").textContent = x.message; }
};
function start() { $("loginView").classList.add("hidden"); $("appView").classList.remove("hidden"); go("dash"); }
function logout() { localStorage.removeItem(KEY); $("appView").classList.add("hidden"); $("loginView").classList.remove("hidden"); closeM(); }

$("nav").innerHTML = NAV.map(([k, l]) => `<button data-k="${k}" onclick="go('${k}')">${l}</button>`).join("");
if (localStorage.getItem(KEY)) start();
