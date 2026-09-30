// Prepare disposable generated-media fixtures for CleanupLiveArrTests.
// Start the isolated loopback-only services with their own config.xml first.
const fs = require('node:fs'); const path = require('node:path');
const root = path.resolve(__dirname, '../../artifacts/cleanup-live');
function four(s) {return Buffer.from(s,'ascii');}
function u32(...v){const b=Buffer.alloc(v.length*4);v.forEach((n,i)=>b.writeUInt32LE(n>>>0,i*4));return b;}
function chunk(tag,data){return Buffer.concat([four(tag),u32(data.length),data,Buffer.alloc(data.length%2)]);}
function list(tag,data){return chunk('LIST',Buffer.concat([four(tag),data]));}
function avi(file){
 const w=64,h=64,frame=Buffer.alloc(w*h*3,62),frames=360;
 // One simple generated frame every ten seconds: an hour of solid-colour test video.
 const main=u32(10000000,frame.length/10,0,0x10,frames,0,1,frame.length,w,h,0,0,0,0);
 const sh=Buffer.alloc(56);sh.write('vids',0);sh.write('DIB ',4);sh.writeUInt32LE(10,20);sh.writeUInt32LE(1,24);sh.writeUInt32LE(frames,32);sh.writeUInt32LE(frame.length,36);sh.writeUInt32LE(0xffffffff,40);sh.writeUInt16LE(w,52);sh.writeUInt16LE(h,54);
 const fmt=Buffer.concat([u32(40,w,h),Buffer.from([1,0,24,0]),u32(0,frame.length,0,0,0,0)]);
 const hdrl=list('hdrl',Buffer.concat([chunk('avih',main),list('strl',Buffer.concat([chunk('strh',sh),chunk('strf',fmt)]))]));
 const movie=list('movi',Buffer.concat(Array.from({length:frames},()=>chunk('00db',frame))));
 const index=chunk('idx1',Buffer.concat(Array.from({length:frames},(_,i)=>Buffer.concat([four('00db'),u32(0x10,4+i*(frame.length+8),frame.length)]))));
 fs.writeFileSync(file,chunk('RIFF',Buffer.concat([four('AVI '),hdrl,movie,index])));
}
async function service(name,port){
 const data=path.join(root,name+'-data');const xml=fs.readFileSync(path.join(data,'config.xml'),'utf8');const key=xml.match(/<ApiKey>([^<]+)<\/ApiKey>/)[1];
 async function api(route,body,method){const response=await fetch(`http://127.0.0.1:${port}/api/v3/${route}`,{method:method||(body?'POST':'GET'),headers:{'X-Api-Key':key,'Content-Type':'application/json'},body:body?JSON.stringify(body):undefined});const text=await response.text();if(!response.ok)throw Error(name+' '+route+' '+response.status+' '+text.slice(0,500));return text?JSON.parse(text):{};}
 const status=await api('system/status');if(path.resolve(status.appData)!==data)throw Error('Test isolation failed');
 return api;
}
(async()=>{
 const results=[];
 for(const name of ['radarr','sonarr']){
  const api=await service(name,name==='radarr'?17878:18989),series=name==='sonarr';
  const library=path.join(root,series?'test-series':'test-movies');fs.mkdirSync(library,{recursive:true});
  const roots=await api('rootfolder');if(!roots.some(r=>r.path===library))await api('rootfolder',{path:library});
  const profiles=await api('qualityprofile');const mediaType=series?'series':'movie';
  let entries=await api(mediaType),entry=entries[0];
  if(!entry){
   const lookup=await api(mediaType+'/lookup?term='+encodeURIComponent(series?'tvdb:78874':'tmdb:10331'));
   if(!lookup.length)throw Error('Lookup failed');const item=lookup[0];
   entry=await api(mediaType,{...item,qualityProfileId:profiles[0].id,rootFolderPath:library,path:path.join(library,series?'Test Series':'Test Movie'),monitored:false,
    seasonFolder:true,minimumAvailability:'released',addOptions:series?{searchForMissingEpisodes:false,searchForCutoffUnmetEpisodes:false}:{searchForMovie:false}});
  }
  fs.mkdirSync(entry.path,{recursive:true});
  const file=path.join(entry.path,series?'Test.Series.S01E01.1080p.WEB-DL.avi':'Night.of.the.Living.Dead.1968.1080p.WEB-DL.avi');avi(file);
  const management=await api('config/mediamanagement');const recycle=path.join(root,name+'-recycle');fs.mkdirSync(recycle,{recursive:true});
  await api('config/mediamanagement',{...management,recycleBin:recycle},'PUT');
  const command=await api('command',series?{name:'RescanSeries',seriesId:entry.id}:{name:'RescanMovie',movieId:entry.id});
  for (let attempt = 0; attempt < 40; attempt++) {
   const progress = await api('command/' + command.id);
   if (progress.status === 'completed' || progress.status === 'failed') break;
   await new Promise(resolve => setTimeout(resolve,500));
  }
  const imports = await api('manualimport?folder=' + encodeURIComponent(entry.path) + '&filterExistingFiles=false');
  const candidate = imports.find(value => value.path === file);
  if (!candidate) throw Error('Generated media was not detected');
  const importFile = {path:file,quality:candidate.quality || {quality:{id:series?3:8},revision:{version:1,real:0,isRepack:false}},languages:[{id:1,name:'English'}]};
  if (series) {
   let episode;
   for (let attempt = 0; attempt < 40; attempt++) {
    const episodes = await api('episode?seriesId=' + entry.id);
    episode = episodes.find(value => value.seasonNumber === 1 && value.episodeNumber === 1);
    if (episode) break;
    await new Promise(resolve => setTimeout(resolve,500));
   }
   if (!episode) throw Error('Test episode metadata is unavailable');
   importFile.seriesId = entry.id; importFile.episodeIds = [episode.id];
  } else importFile.movieId = entry.id;
  const importedBefore = await api((series?'episodefile?seriesId=':'moviefile?movieId=') + entry.id);
  if (!importedBefore.some(value => value.path === file)) await api('command',{name:'ManualImport',files:[importFile],importMode:'auto'});
  for (let attempt = 0; attempt < 20; attempt++) {
   const imported = await api((series?'episodefile?seriesId=':'moviefile?movieId=') + entry.id);
   if (imported.some(value => value.path === file)) break;
   if (attempt === 19) throw Error('Test import did not finish');
   await new Promise(resolve => setTimeout(resolve,500));
  }
  results.push({name,version:(await api('system/status')).version,id:entry.id,itemId:series?entry.tvdbId:entry.tmdbId,path:entry.path,file,command:command.id});
 }
 fs.writeFileSync(path.join(root,'fixtures.json'),JSON.stringify(results,null,2));console.log(results);
})().catch(error=>{console.error(error.message);process.exitCode=1;});
