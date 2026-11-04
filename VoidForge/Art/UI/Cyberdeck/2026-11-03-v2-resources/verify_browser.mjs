// Render/interaction QA with installed Chrome and built-in Node CDP WebSocket.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {spawn} from 'node:child_process';
const root=path.dirname(fileURLToPath(import.meta.url));
const project=path.resolve(root,'../../../..');
const profile=path.join(project,'Library','ResourcesCatalogueChrome20261103');
fs.mkdirSync(profile,{recursive:true});
const active=path.join(profile,'DevToolsActivePort');
if(fs.existsSync(active))fs.unlinkSync(active);
const chrome=spawn('C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',[
 '--headless=new','--disable-gpu','--no-first-run','--no-default-browser-check',
 '--disable-extensions','--allow-file-access-from-files','--remote-allow-origins=*',
 '--remote-debugging-port=0',`--user-data-dir=${profile}`,'about:blank'],{windowsHide:true,stdio:'ignore'});
let socket;
const errors=[];const checks=[];let id=0;const pending=new Map();
const delay=ms=>new Promise(r=>setTimeout(r,ms));
async function send(method,params={}){return new Promise((resolve,reject)=>{
 const n=++id,timer=setTimeout(()=>{pending.delete(n);reject(Error(method+' timed out'));},60000);
 pending.set(n,{resolve:r=>{clearTimeout(timer);resolve(r);},reject});socket.send(JSON.stringify({id:n,method,params}));
});}
async function evaluate(expression){const r=await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw Error(JSON.stringify(r.exceptionDetails));return r.result.value;}
function check(name,pass,actual){checks.push({name,pass,actual});if(!pass)throw Error(name+': '+JSON.stringify(actual));}
try{
 for(let i=0;i<100&&!fs.existsSync(active);i++)await delay(100);
 const port=fs.readFileSync(active,'utf8').split('\n')[0];
 const targets=await (await fetch(`http://127.0.0.1:${port}/json`)).json();
 socket=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);
 socket.onmessage=e=>{const r=JSON.parse(e.data);if(r.id){const p=pending.get(r.id);if(p){pending.delete(r.id);r.error?p.reject(Error(JSON.stringify(r.error))):p.resolve(r.result);}}else if(r.method==='Runtime.exceptionThrown')errors.push(r.params.exceptionDetails);};
 await new Promise((resolve,reject)=>{socket.onopen=resolve;socket.onerror=reject;});
 await send('Runtime.enable');await send('Page.enable');
 await send('Emulation.setDeviceMetricsOverride',{width:1680,height:1400,deviceScaleFactor:1,mobile:false});
 await send('Page.navigate',{url:pathToFileURL(path.join(root,'index.html')).href});
 for(let i=0;i<100;i++){if(await evaluate('document.readyState==="complete" && typeof DATA!=="undefined"'))break;await delay(100);}
 await evaluate('Promise.all([...document.images].map(i=>{i.loading="eager";return i.decode().catch(()=>null)}))');
 check('initial 54 item cards',await evaluate('document.querySelectorAll(".card").length')===54);
 const bad=await evaluate('[...document.images].filter(i=>!i.naturalWidth).map(i=>i.src)');check('all displayed images load',bad.length===0,bad);
 await evaluate('document.querySelector(\'[data-family="recettes"]\').click()');
 check('component filter 21',await evaluate('document.querySelectorAll(".card").length')===21);
 await evaluate('document.querySelector(\'[data-id="component-motor"]\').click(); document.querySelector("#qty").value=10;document.querySelector("#qty").dispatchEvent(new Event("input"));document.querySelector("#eff").value="3";document.querySelector("#eff").dispatchEvent(new Event("change"));');
 const motor=await evaluate('[...document.querySelectorAll("#detail td:last-child")].map(t=>t.textContent)');
 const parse=s=>Number(s.replace(/[^0-9,.-]/g,'').replace(',','.'));
 check('motor x3, quantity10 refined66.667/16.667 ore95.238/41.667',motor.length===4&&motor.map(parse).every((n,i)=>Math.abs(n-[66.667,16.667,95.238,41.667][i])<.0001),motor);
 await evaluate('document.querySelector("#qty").value=1;document.querySelector("#eff").value="1";selected="component-reactor";draw();');
 const reactor=await evaluate('[...document.querySelectorAll("#detail table")][1].textContent');
 check('reactor stone coproduct credited, no extra iron ore',reactor.includes('Pierre brute')&&!reactor.includes('Minerai de fer')&&reactor.includes('50 kg'),reactor);
 await evaluate('family="blocs";selected="block-assembler";draw();');
 const assembler=await evaluate('[...document.querySelectorAll("#detail table")][1].textContent');
 check('assembler BOM expands to 4190Fe 150Ni 30Co 82Si',/4\s?190/.test(assembler)&&assembler.includes('150 kg')&&assembler.includes('30 kg')&&assembler.includes('82 kg'),assembler);
 await evaluate('family="all";selected="component-plates";draw();');
 await evaluate('Promise.all([...document.images].map(i=>i.decode()))');
 const screen=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});fs.writeFileSync(path.join(root,'gallery-review.png'),Buffer.from(screen.data,'base64'));
 await send('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
 check('mobile no horizontal overflow',await evaluate('document.documentElement.scrollWidth<=innerWidth'));
 await send('Emulation.setDeviceMetricsOverride',{width:1680,height:1800,deviceScaleFactor:1,mobile:false});
 for(const [category,item] of [['minerais','ore-gold'],['produits','ingot-cobalt'],['recettes','component-thruster']]){
  await evaluate(`family=${JSON.stringify(category)};selected=${JSON.stringify(item)};draw();Promise.all([...document.images].map(i=>i.decode()))`);
  const capture=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});
  fs.writeFileSync(path.join(root,`review-${category}.png`),Buffer.from(capture.data,'base64'));
 }
 check('no script exceptions',errors.length===0,errors);
 fs.writeFileSync(path.join(root,'browser-validation.json'),JSON.stringify({success:true,checks,errors},null,2));
 console.log(JSON.stringify({success:true,checks:checks.length}));
}catch(e){fs.writeFileSync(path.join(root,'browser-validation.json'),JSON.stringify({success:false,checks,errors,error:String(e)},null,2));console.error(e);process.exitCode=1;}
finally{if(socket)socket.close();chrome.kill();}
