"""The landing-page payload: hidden ad probe plus browser fingerprint JS."""

HEAD = """<!doctype html><html><head><meta charset="utf-8">
<meta http-equiv="refresh" content="4;url=/r?ab=nojs">
<style>body{margin:0;background:#0a0a0a}
.ad,.ads,.adsbox,.advertisement,#ad{position:absolute;left:-9999px;height:1px;width:1px}
</style></head><body>
<div class="ad adsbox advertisement" id="ad1" style="height:12px;width:300px">&nbsp;</div>
"""

JS = r"""<script>(function(){
var o={},el=function(t){return document.createElement(t)};
try{var c=el('canvas'),x=c.getContext('2d');
x.textBaseline='top';x.font='14px Arial';x.fillStyle='#f60';
x.fillRect(125,1,62,20);x.fillStyle='#069';x.fillText('fp',2,15);
o.cv=c.toDataURL().slice(-30);}catch(e){}
try{var g=el('canvas').getContext('webgl'),
d=g.getExtension('WEBGL_debug_renderer_info');
o.gpu=g.getParameter(d.UNMASKED_RENDERER_WEBGL);
o.gpuv=g.getParameter(d.UNMASKED_VENDOR_WEBGL);}catch(e){}
try{var A=window.AudioContext||window.webkitAudioContext,
ac=new A(),osc=ac.createOscillator(),dst=ac.createDynamicsCompressor();
osc.connect(dst);dst.connect(ac.destination);osc.start(0);
o.audio=dst.reduction.toString().slice(0,8);osc.stop();}catch(e){}
try{var fs=['Arial','Courier New','Georgia','Helvetica','Times New Roman',
'Verdana','Tahoma','Trebuchet MS','Impact','Comic Sans MS','Consolas',
'Menlo','Monaco','Segoe UI','Roboto','Noto Sans','DejaVu Sans','Ubuntu'],
base=['monospace','sans-serif','serif'],found=[];
function w(t){var s=el('span');s.style.fontSize='72px';s.style.fontFamily=t;
s.textContent='mmmmmmmmmmlli';s.style.position='absolute';s.style.left='-9999px';
document.body.appendChild(s);var r=s.offsetWidth;s.remove();return r}
for(var i=0;i<fs.length;i++){var d1=false;
for(var j=0;j<base.length;j++){if(w('"'+fs[i]+'",'+base[j])!==w(base[j]))d1=true}
if(d1)found.push(fs[i])}
o.fonts=found.join('|');}catch(e){}
try{o.plat=navigator.platform||'';
o.langs=(navigator.languages||[navigator.language]).join(',');
o.cores=navigator.hardwareConcurrency||0;o.mem=navigator.deviceMemory||0;
o.touch=navigator.maxTouchPoints||0;o.wd=navigator.webdriver?1:0;
o.dnt=navigator.doNotTrack||'';o.cookies=navigator.cookieEnabled?1:0;
o.pdf=navigator.pdfViewerEnabled?1:0;o.vendor=navigator.vendor||'';
o.prods=navigator.product||'';}catch(e){}
try{o.scr=screen.width+'x'+screen.height;
o.scr_av=screen.availWidth+'x'+screen.availHeight;
o.dpr=window.devicePixelRatio||1;o.cd=screen.colorDepth||0;
o.depth=screen.pixelDepth||0;o.orient=(screen.orientation||{}).type||'';
o.win=window.innerWidth+'x'+window.innerHeight;}catch(e){}
try{o.tzo=new Date().getTimezoneOffset();
o.tz=(Intl.DateTimeFormat().resolvedOptions().timeZone)||'';
o.loc=Intl.DateTimeFormat().resolvedOptions().locale||'';}catch(e){}
try{var mm=Math;o.mth=(mm.tan(-1e300)).toString().slice(0,14)+
(mm.sinh?(mm.sinh(1)).toString().slice(0,6):'');}catch(e){}
try{o.stor=(typeof localStorage)+'/'+(typeof indexedDB);}catch(e){}
try{o.dark=(window.matchMedia('(prefers-color-scheme:dark)').matches)?1:0;
o.rm=(window.matchMedia('(prefers-reduced-motion:reduce)').matches)?1:0;
o.hc=(window.matchMedia('(forced-colors:active)').matches)?1:0;}catch(e){}
try{var cn=navigator.connection||{};o.conn=cn.effectiveType||'';
o.dl=cn.downlink||0;}catch(e){}
var probes=['https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js'],
done=0,blocked=0,ab=0;
try{var b=document.getElementById('ad1');
if(b&&(b.offsetHeight===0||getComputedStyle(b).display==='none'))ab=1;}catch(e){}
function fin(){done++;if(done>=probes.length){
if(blocked===probes.length)ab=1;o.ab=ab;send()}}
probes.forEach(function(u){var i=new Image();
var t=setTimeout(function(){blocked++;i.onload=null;i.onerror=null;fin()},1800);
i.onload=function(){clearTimeout(t);fin()};
i.onerror=function(){clearTimeout(t);blocked++;fin()};
i.src=u+'?cb='+Date.now()});
try{var P=window.RTCPeerConnection||window.webkitRTCPeerConnection;
if(P){var p=new P({iceServers:[]}),ips=[];
p.onicecandidate=function(e){
if(e.candidate&&e.candidate.candidate){
var m=e.candidate.candidate.match(/(\d+\.\d+\.\d+\.\d+)/);
if(m&&ips.indexOf(m[1])<0)ips.push(m[1])}};
try{p.createDataChannel('x')}catch(e){}
p.createOffer().then(function(of){return p.setLocalDescription(of)}).catch(function(){});
setTimeout(function(){o.webrtc=ips.join(',')},1200)}}catch(e){}
try{if(navigator.getBattery){navigator.getBattery().then(function(b){
o.bat=Math.round(b.level*100)+'%';o.bat_ch=b.charging?'chg':'dis'})}}catch(e){}
try{if(navigator.mediaDevices&&navigator.mediaDevices.enumerateDevices){
navigator.mediaDevices.enumerateDevices().then(function(l){var c=0,m=0,s=0;
for(var i=0;i<l.length;i++){if(l[i].kind==='videoinput')c++;
if(l[i].kind==='audioinput')m++;if(l[i].kind==='audiooutput')s++}
o.cams=c;o.mics=m;o.spk=s})}}catch(e){}
function send(){var qs='ab=0&rs=abx&fp='+encodeURIComponent(JSON.stringify(o));
window.location='/r?'+qs}
})();</script></body></html>"""

PAGE = HEAD + JS

# Ordered (label, key, formatter) pairs driving the fingerprint readout.
FIELDS = (
    ("canvas", "cv", lambda v: v),
    ("gpu", "gpu", lambda v: str(v)[:60]),
    ("gpu vendor", "gpuv", lambda v: str(v)[:40]),
    ("audio", "audio", lambda v: str(v)),
    ("fonts", "fonts", lambda v: str(v)[:80]),
    ("platform", "plat", lambda v: v),
    ("languages", "langs", lambda v: v),
    ("cpu cores", "cores", lambda v: str(v)),
    ("device mem", "mem", lambda v: "%sGB" % v),
    ("touch pts", "touch", lambda v: str(v)),
    ("screen", "scr", lambda v: v),
    ("avail scr", "scr_av", lambda v: v),
    ("viewport", "win", lambda v: v),
    ("pixel ratio", "dpr", lambda v: str(v)),
    ("color depth", "cd", lambda v: str(v)),
    ("pixel depth", "depth", lambda v: str(v)),
    ("orientation", "orient", lambda v: v),
    ("timezone", "tz", lambda v: v),
    ("tz offset", "tzo", lambda v: "%sm" % v),
    ("locale", "loc", lambda v: v),
    ("math fp", "mth", lambda v: str(v)),
    ("storage", "stor", lambda v: str(v)),
    ("dark mode", "dark", lambda v: "yes" if v else None),
    ("reduced motion", "rm", lambda v: "yes" if v else None),
    ("high contrast", "hc", lambda v: "yes" if v else None),
    ("connection", "conn", lambda v: v),
    ("downlink", "dl", lambda v: "%smbps" % v),
    ("local IP", "webrtc", lambda v: v),
    ("battery", "bat", lambda v: v),
    ("charging", "bat_ch", lambda v: v),
    ("cams", "cams", lambda v: str(v)),
    ("mics", "mics", lambda v: str(v)),
    ("speakers", "spk", lambda v: str(v)),
    ("pdf viewer", "pdf", lambda v: "yes" if v else None),
    ("vendor", "vendor", lambda v: str(v)[:40]),
)


def rows(fp):
    """Flatten a decoded fingerprint dict into ordered label/value pairs."""
    if not isinstance(fp, dict):
        return []
    out = []
    for label, key, fmt in FIELDS:
        if key not in fp:
            continue
        val = fmt(fp[key])
        if val:
            out.append((label, val))
    return out


def parse(fp):
    """Alias for rows for compatibility."""
    return rows(fp)