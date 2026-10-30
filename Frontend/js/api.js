
const API_BASE = "http://localhost:5050";
function token(){return localStorage.getItem("career_token")}
async function api(path,options={}){
  const headers=new Headers(options.headers||{});
  if(options.body && !headers.has("Content-Type")) headers.set("Content-Type","application/json");
  if(token()) headers.set("Authorization","Bearer "+token());
  const r=await fetch(API_BASE+path,{...options,headers});
  let data=null; try{data=await r.json()}catch{}
  if(!r.ok) throw new Error(data?.message||`Request failed (${r.status})`);
  return data;
}
function toast(message){const x=document.createElement("div");x.className="toast";x.textContent=message;document.body.appendChild(x);setTimeout(()=>x.remove(),2800)}
function initials(name){return (name||"User").split(/\s+/).map(x=>x[0]).join("").slice(0,2).toUpperCase()}
function logout(){localStorage.removeItem("career_token");location.reload()}
