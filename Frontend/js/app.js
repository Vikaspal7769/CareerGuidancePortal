
const $=s=>document.querySelector(s);
const $$=s=>[...document.querySelectorAll(s)];
let currentSection="dashboard";

function showAuth(mode="login"){
  $("#auth").classList.remove("hidden"); $("#app").classList.add("hidden");
  $("#loginForm").classList.toggle("hidden",mode!=="login");
  $("#registerForm").classList.toggle("hidden",mode!=="register");
  $("#authTitle").textContent=mode==="login"?"Welcome back":"Create your account";
  $("#authSub").textContent=mode==="login"?"Sign in to continue your personalized career journey.":"Build your profile and unlock personalized recommendations.";
}
function showApp(){ $("#auth").classList.add("hidden"); $("#app").classList.remove("hidden"); loadDashboard(); }
function nav(section){
  currentSection=section;
  $$(".nav button").forEach(b=>b.classList.toggle("active",b.dataset.section===section));
  $$(".section").forEach(s=>s.classList.add("hidden"));
  $("#section-"+section)?.classList.remove("hidden");
  $("#pageTitle").textContent=({dashboard:"Dashboard",profile:"My Profile",careers:"AI Career Match",courses:"Courses",colleges:"Colleges",mentors:"Mentors",notifications:"Notifications"})[section]||"Dashboard";
  ({dashboard:loadDashboard,profile:loadProfile,careers:loadCareers,courses:loadCourses,colleges:loadColleges,mentors:loadMentors,notifications:loadNotifications}[section]||loadDashboard)();
}
async function loadDashboard(){
  try{
    const p=await api("/api/profile");
    $("#userName").textContent=p.name||"Student"; $("#avatar").textContent=initials(p.name);
    $("#welcomeName").textContent=p.name?.split(" ")[0]||"there";
    const complete=["education","college","skills","interests","preferredLocation","careerGoal"].filter(k=>p[k]).length;
    $("#completion").textContent=Math.round(complete/6*100)+"%";
    $("#completionBar").style.width=(complete/6*100)+"%";
    $("#profileHint").textContent=complete===6?"Profile complete":"Complete your profile for better AI matches";
  }catch{showAuth()}
}
async function loadProfile(){
  const p=await api("/api/profile");
  ["name","education","college","skills","interests","preferredLocation","careerGoal"].forEach(k=>$("#p_"+k).value=p[k]||"");
}
async function loadCareers(){
  const box=$("#careerGrid"); box.innerHTML='<div class="empty">Generating your personalized matches…</div>';
  try{
    const d=await api("/api/recommendations"); const arr=d.careers||[];
    box.innerHTML=arr.map((x,i)=>`<article class="card"><div class="icon">✦</div><h4>${x.title||x.name||"Career Path"}</h4><p class="muted">${x.description||x.reason||"Personalized recommendation based on your profile."}</p><div class="tags">${(x.skills||x.requiredSkills||[]).slice(0,5).map(s=>`<span class="tag">${s}</span>`).join("")}</div></article>`).join("")||'<div class="empty">Complete your profile to get recommendations.</div>';
  }catch(e){box.innerHTML=`<div class="empty">${e.message}</div>`}
}
async function loadCourses(){
  const box=$("#courseGrid"),arr=await api("/api/courses");
  box.innerHTML=arr.map(x=>`<article class="card"><div class="icon">📚</div><h4>${x.name}</h4><p class="muted">${x.description}</p><div class="tags"><span class="tag">${x.provider}</span><span class="tag">${x.level}</span><span class="tag">${x.duration}</span></div><a class="btn btn-light" href="${x.url}" target="_blank">View course</a></article>`).join("");
}
async function loadColleges(){
  const box=$("#collegeGrid"),arr=await api("/api/colleges");
  box.innerHTML=arr.map(x=>`<article class="card"><div class="icon">🎓</div><h4>${x.name}</h4><p class="muted">${x.description}</p><div class="tags"><span class="tag">${x.city}</span><span class="tag">${x.type}</span>${x.programs.slice(0,3).map(s=>`<span class="tag">${s}</span>`).join("")}</div></article>`).join("");
}
async function loadMentors(){
  const box=$("#mentorGrid"),arr=await api("/api/mentors");
  box.innerHTML=arr.map(x=>`<article class="card"><div class="avatar">${initials(x.name)}</div><h4 style="margin-top:12px">${x.name}</h4><p class="muted">${x.role} · ${x.company}</p><p class="muted">${x.bio}</p><div class="tags">${x.expertise.map(s=>`<span class="tag">${s}</span>`).join("")}</div><button class="btn btn-light" onclick="requestMentor(${x.id})">Request session</button></article>`).join("");
}
async function requestMentor(id){try{const x=await api(`/api/mentors/${id}/request`,{method:"POST"});toast(x.message)}catch(e){toast(e.message)}}
async function loadNotifications(){
  const box=$("#notificationList"),arr=await api("/api/notifications");
  box.innerHTML=arr.length?arr.map(x=>`<div class="card"><strong>${x.title}</strong><p class="muted">${x.message}</p></div>`).join(""):'<div class="empty">No notifications yet.</div>';
}
document.addEventListener("DOMContentLoaded",()=>{
  $("#loginSwitch").onclick=()=>showAuth("register"); $("#registerSwitch").onclick=()=>showAuth("login");
  $("#loginForm").onsubmit=async e=>{e.preventDefault();try{const d=await api("/api/auth/login",{method:"POST",body:JSON.stringify({email:$("#loginEmail").value,password:$("#loginPassword").value})});localStorage.setItem("career_token",d.token);showApp()}catch(x){$("#authMsg").textContent=x.message;$("#authMsg").className="msg error"}};
  $("#registerForm").onsubmit=async e=>{e.preventDefault();try{const d=await api("/api/auth/register",{method:"POST",body:JSON.stringify({name:$("#regName").value,email:$("#regEmail").value,password:$("#regPassword").value})});localStorage.setItem("career_token",d.token);showApp()}catch(x){$("#authMsg").textContent=x.message;$("#authMsg").className="msg error"}};
  $("#profileForm").onsubmit=async e=>{e.preventDefault();const keys=["name","education","college","skills","interests","preferredLocation","careerGoal"];const body={};keys.forEach(k=>body[k]=$("#p_"+k).value);try{await api("/api/profile",{method:"PUT",body:JSON.stringify(body)});toast("Profile updated successfully");loadDashboard()}catch(x){toast(x.message)}};
  $$(".nav button").forEach(b=>b.onclick=()=>nav(b.dataset.section));
  $("#logout").onclick=logout;
  if(token()) showApp(); else showAuth("login");
});
