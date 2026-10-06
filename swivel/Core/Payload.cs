using System.Text.Json;

namespace Swivel.Core;

/// <summary>The landing payload: hidden ad probe plus browser fingerprint script.</summary>
public static class Payload
{
    const string Head = """
<!doctype html><html><head><meta charset="utf-8">
<meta http-equiv="refresh" content="4;url=/r?ab=nojs">
<style>body{margin:0;background:#0a0a0a}
.ad,.ads,.adsbox,.advertisement,#ad{position:absolute;left:-9999px;height:1px;width:1px}
</style></head><body>
<div class="ad adsbox advertisement" id="ad1" style="height:12px;width:300px">&nbsp;</div>
""";

    const string Script = """
<script>(function(){
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
})();</script></body></html>
""";

    /// <summary>Full page served to visitors, in UTF-8.</summary>
    public static string Page => Head + Script;

    static string Text(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString() ?? "",
        JsonValueKind.Number => e.GetRawText(),
        JsonValueKind.True or JsonValueKind.False => e.GetRawText(),
        _ => "",
    };

    static Func<JsonElement, string?> Clipped(int width) => e =>
    {
        var text = Text(e);
        return text.Length > width ? text[..width] : text;
    };

    static Func<JsonElement, string?> Suffix(string suffix) => e =>
    {
        var text = Text(e);
        return text.Length == 0 ? text : text + suffix;
    };

    static string Yes(JsonElement e) =>
        e.ValueKind == JsonValueKind.True ? "yes" : "";

    static readonly (string Label, string Key, Func<JsonElement, string?> Format)[] Fields =
    [
        ("canvas", "cv", Text),
        ("gpu", "gpu", Clipped(60)),
        ("gpu vendor", "gpuv", Clipped(40)),
        ("audio", "audio", Text),
        ("fonts", "fonts", Clipped(80)),
        ("platform", "plat", Text),
        ("languages", "langs", Text),
        ("cpu cores", "cores", Text),
        ("device mem", "mem", Suffix("GB")),
        ("touch pts", "touch", Text),
        ("screen", "scr", Text),
        ("avail scr", "scr_av", Text),
        ("viewport", "win", Text),
        ("pixel ratio", "dpr", Text),
        ("color depth", "cd", Text),
        ("pixel depth", "depth", Text),
        ("orientation", "orient", Text),
        ("timezone", "tz", Text),
        ("tz offset", "tzo", Suffix("m")),
        ("locale", "loc", Text),
        ("math fp", "mth", Text),
        ("storage", "stor", Text),
        ("dark mode", "dark", Yes),
        ("reduced motion", "rm", Yes),
        ("high contrast", "hc", Yes),
        ("connection", "conn", Text),
        ("downlink", "dl", Suffix("mbps")),
        ("local IP", "webrtc", Text),
        ("battery", "bat", Text),
        ("charging", "bat_ch", Text),
        ("cams", "cams", Text),
        ("mics", "mics", Text),
        ("speakers", "spk", Text),
        ("pdf viewer", "pdf", Yes),
        ("vendor", "vendor", Clipped(40)),
    ];

    /// <summary>Flatten a decoded fingerprint into ordered label/value pairs.</summary>
    public static List<(string Label, string Value)> Rows(JsonElement fingerprint)
    {
        var rows = new List<(string, string)>();
        if (fingerprint.ValueKind != JsonValueKind.Object)
            return rows;
        foreach (var (label, key, format) in Fields)
        {
            if (!fingerprint.TryGetProperty(key, out var value))
                continue;
            var text = format(value);
            if (!string.IsNullOrEmpty(text))
                rows.Add((label, text));
        }
        return rows;
    }
}