// Local UI fixture with production assets and in-memory test data. No production services.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../../Jellyfin.Plugin.WatchCircle/Web/js');
const id = n => `${String(n).padStart(8, '0')}-1111-2222-3333-444444444444`;
let admin = true, answered = false, kept = false, protectedTitle = false;
const media = { ItemId: id(1), Kind: 'Series', Name: 'Zwischen den Sternen', Bytes: 36400000000, Paths: ['/test/series'], Problem: null, Members: [], Collections: [], LibraryIds: [id(9)] };
const entry = () => ({ Id: id(2), Media: media, Present: true, Protected: protectedTitle, Baseline: '2026-03-01T00:00:00Z', LastInteraction: '2026-05-12T19:30:00Z', LastKind: 'Keep', LastUserId: id(3),
    NominationId: kept || protectedTitle ? null : id(4), NominatedAt: '2026-09-15T00:00:00Z', DeleteAt: kept || protectedTitle ? null : '2026-10-15T00:00:00Z', Error: null });
const nomination = () => ({ Id: id(2), ItemId: id(1), RootItemId: id(1), Name: media.Name, Kind: 'Series', NominationId: id(4), DeleteAt: '2026-10-15T00:00:00Z',
    Answer: answered ? 'Indifferent' : null, AnswerAt: answered ? '2026-09-30T00:00:00Z' : null });
const replies = () => [{ UserId: id(3), UserName: 'Anna', EntryId: id(2), NominationId: id(5), Answer: 'Keep', At: '2026-05-12T00:00:00Z', Active: false }];
const settings = { Enabled: true, AutomaticDeletion: false, InactivityMonths: 3, WarningDays: 30, IntervalHours: 24, LibraryIds: [id(9)],
    Radarr: { Url: 'http://radarr.example:7878', ApiKey: '', HasApiKey: true, Paths: [], AddImportExclusion: false },
    Sonarr: { Url: 'http://sonarr.example:8989', ApiKey: '', HasApiKey: true, Paths: [{ Jellyfin: '/media', Arr: '/series' }], AddImportExclusion: false } };
const html = `<!doctype html><html lang="de"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>WatchCircle · Aufräumen · lokale Vorschau</title>
<style>body{margin:0;background:#101114;color:#eee;font:18px Arial}button{font:inherit;cursor:pointer}header.demo{display:flex;padding:16px 3vw;align-items:center;gap:20px;background:#20232b}header.demo strong{flex:1}.wc-navbar-actions{display:flex}.wc-navbar-actions button{background:transparent;color:white;border:0}.preview-info{background:#233140;padding:12px 3vw;font-size:14px}.library{padding:30px 4vw}.shelf{display:flex;gap:24px}.card{width:180px}.cardImageContainer{position:relative;aspect-ratio:2/3;background:linear-gradient(160deg,#42334f,#132b42);border-radius:8px}.cardImageContainer img{width:100%;height:100%;border-radius:8px}.card a{display:block;color:#ddd;text-decoration:none;text-align:center;padding-top:12px}.detailPagePrimaryContent{margin:40px 0;max-width:1000px}#demo-controls{padding:14px 4vw;font-size:14px}#demo-controls button{margin:4px;padding:8px}</style></head>
<body><header class="demo"><strong>Jellyfin · WatchCircle</strong><div class="wc-navbar-actions"><button aria-label="WatchCircle-Personen">♧</button></div><span>●</span></header>
<div class="preview-info">Lokale Testvorschau mit Beispieldaten. Keine Verbindung zu deinem Server.</div>
<section class="library"><h2>Serien</h2><div class="shelf">${[1,6,7].map((n,i)=>`<div class="card" data-id="${id(n)}"><div class="cardImageContainer cardContent"><img alt="" src="/Items/${id(n)}/Images/Primary"></div><a href="#/details?id=${id(n)}">${['Zwischen den Sternen','Nordlicht','Das stille Tal'][i]}</a></div>`).join('')}</div>
<div class="detailPagePrimaryContent"><h2>Zwischen den Sternen</h2><p>Staffel 1 · 20 Folgen</p><p>Eine Forschungsstation empfängt eine Nachricht aus einer unbekannten Galaxie.</p><button type="button" id="feedback-demo">Rückmeldungen öffnen</button></div></section>
<div id="demo-controls"><strong>Nur Vorschau:</strong><button id="reset-demo">Beispieldaten zurücksetzen</button><button id="normal-demo">Normalnutzer</button><button id="admin-demo">Administrator</button><button id="language-demo">Deutsch / English</button></div>
<script>window.ApiClient={getCurrentUserId:()=> '${id(8)}',getUrl:(route,query)=>'/'+route+(query?'?'+new URLSearchParams(query):''),ajax:async options=>{const r=await fetch(options.url,{method:options.type,headers:{'Content-Type':'application/json'},body:options.data});const data=await r.json();if(!r.ok)throw {responseJSON:data};return data;}};
location.hash='/details?id=${id(1)}';
document.getElementById('feedback-demo').onclick=()=>WatchCircleCleanup.show();
document.getElementById('reset-demo').onclick=async()=>{await fetch('/demo/reset');location.reload()};
document.getElementById('normal-demo').onclick=async()=>{await fetch('/demo/normal');location.reload()};
document.getElementById('admin-demo').onclick=async()=>{await fetch('/demo/admin');location.reload()};
document.getElementById('language-demo').onclick=()=>{document.documentElement.lang=document.documentElement.lang==='de'?'en':'de'};
</script><script src="/WatchCircle/js/utils/i18n.js"></script><script src="/WatchCircle/js/utils/assetUrl.js"></script><script src="/WatchCircle/js/components/cleanup/cleanup.js"></script></body></html>`;
http.createServer(async (req, res) => {
    const url = new URL(req.url, 'http://localhost'), route = url.pathname;
    function send(value, code=200) { res.writeHead(code, {'Content-Type':'application/json'}); res.end(JSON.stringify(value)); }
    if (route === '/') { res.writeHead(200,{'Content-Type':'text/html;charset=utf-8'}); res.end(html); return; }
    if (route.startsWith('/demo/')) { if(route.endsWith('normal'))admin=false; if(route.endsWith('admin'))admin=true; if(route.endsWith('reset')) {kept=answered=protectedTitle=false;admin=true;} send({});return; }
    if (route.startsWith('/WatchCircle/js/')) {
        const file = path.resolve(root, route.slice('/WatchCircle/js/'.length));
        if (!file.startsWith(root + path.sep) || !fs.existsSync(file)) { send({},404); return; }
        res.writeHead(200,{'Content-Type':file.endsWith('.css')?'text/css':'application/javascript'});res.end(fs.readFileSync(file));return;
    }
    if (route.startsWith('/Items/')) {
        const number = Number(route.split('/')[2].slice(0,8)), color = number === 1 ? '#45445f' : number === 6 ? '#134853' : '#59442e';
        res.writeHead(200,{'Content-Type':'image/svg+xml'});res.end(`<svg xmlns="http://www.w3.org/2000/svg" width="240" height="360"><rect width="240" height="360" fill="${color}"/><circle cx="150" cy="110" r="65" fill="#acabb6" opacity=".3"/><path d="M0 310L170 140 240 240v120H0" fill="#141d29" opacity=".6"/><text x="20" y="300" fill="white" font-family="sans-serif" font-size="18">${number===1?'ZWISCHEN DEN':number===6?'NORDLICHT':'DAS STILLE TAL'}</text>${number===1?'<text x="20" y="325" fill="white" font-family="sans-serif" font-size="24">STERNEN</text>':''}</svg>`);return;
    }
    if (!route.startsWith('/WatchCircle/Cleanup/')) { send({},404);return; }
    const api = route.slice('/WatchCircle/Cleanup/'.length); let body=''; for await(const chunk of req)body+=chunk; const input = body ? JSON.parse(body) : {};
    if(api==='Status')send({Enabled:settings.Enabled,IsAdmin:admin});
    else if(api==='Pending')send(kept||protectedTitle||answered?[]:[nomination()]);
    else if(api==='Items/Status')send(kept||protectedTitle||!input.includes(id(1))?[]:[nomination()]);
    else if(api.startsWith('Replies/')) {if(kept||protectedTitle)send({Error:'This nomination is no longer open.'},409);else{answered=true;kept=input.Answer==='Keep';send({Success:true});}}
    else if(api.startsWith('Admin/')&&!admin)send({},403);
    else if(api.startsWith('Admin/Protection/Item/'))send({Id:id(2),Protected:protectedTitle,EffectiveProtection:protectedTitle});
    else if(api.startsWith('Admin/Protection/')) {protectedTitle=input.Protected;send({Success:true});}
    else if(api==='Admin/Settings'){if(req.method==='POST')Object.assign(settings,input);send({Settings:settings,Libraries:[{ItemId:id(9),Name:'Filme & Serien'}],Error:null});}
    else if(api.startsWith('Admin/Test/'))send({Version:'Isolierte Vorschau'});
    else if(api==='Admin/Items'||api==='Admin/Evaluate')send({Enabled:true,AutomaticDeletion:false,Entries:[{Entry:entry(),LastUserName:'Anna',EffectiveProtection:protectedTitle,Collections:[],Replies:replies(),Deletions:[],RestoreErrors:[]}]});
    else if(api.startsWith('Admin/Items/'))send({Entry:entry(),Mapping:{Id:42,Path:'/series/Zwischen den Sternen'},Requesters:[{Name:'Anna',ProfileUserId:id(3)}],Users:[
        {Id:id(3),Name:'Anna',IsRequester:true,Open:true,Replies:replies(),Progress:{Started:true,Completed:false,TotalEpisodes:20,CompletedEpisodes:8,Percent:42,Episode:{SeasonIndexNumber:1,EpisodeIndexNumber:9,PlaybackPositionTicks:12000000000,EpisodeRunTimeTicks:30000000000}}},
        {Id:id(8),Name:'Roman',Open:true,Replies:[],Progress:{Started:true,Completed:false,TotalEpisodes:20,CompletedEpisodes:3,Percent:15,Episode:{Played:true,SeasonIndexNumber:1,EpisodeIndexNumber:3}}},
        {Id:id(10),Name:'Alex',Open:true,Replies:[],Progress:{Started:false,Completed:false,TotalEpisodes:20,CompletedEpisodes:0,Percent:0}}
    ]});else send({},404);
}).listen(8769,'127.0.0.1',()=>process.stdout.write('Cleanup preview: http://127.0.0.1:8769\n'));
