// Run with `node tests/web/preview-navbar.cjs`, then open http://127.0.0.1:8768.
// Serves the regression fixture and production assets; no Jellyfin server is contacted.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const assets = path.resolve(__dirname, '../../Jellyfin.Plugin.WatchCircle/Web/js');
http.createServer((req, res) => {
    const pathname = new URL(req.url, 'http://localhost').pathname;
    let file;
    if (pathname === '/') file = path.join(__dirname, 'fixtures/navbar.html');
    else if (pathname.startsWith('/WatchCircle/js/')) {
        file = path.resolve(assets, pathname.slice('/WatchCircle/js/'.length));
        if (!file.startsWith(assets + path.sep)) { res.writeHead(403); res.end(); return; }
    }
    if (!file || !fs.existsSync(file)) { res.writeHead(404); res.end(); return; }
    res.setHeader('Content-Type', file.endsWith('.html') ? 'text/html; charset=utf-8' : file.endsWith('.css') ? 'text/css' : 'application/javascript');
    res.end(fs.readFileSync(file));
}).listen(8768, '127.0.0.1', () => process.stdout.write('Navbar fixture: http://127.0.0.1:8768\n'));
