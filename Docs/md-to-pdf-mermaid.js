#!/usr/bin/env node
// Converts Markdown files with Mermaid diagrams to PDF.
// Uses md-to-pdf for Markdown→HTML, then Puppeteer with explicit Mermaid wait.
//
// Usage: node md-to-pdf-mermaid.js <file.md> [file2.md ...]

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const http = require('http');
const puppeteer = require(require.resolve('puppeteer', {
    paths: ['/Users/jedwards/.nvm/versions/node/v20.20.0/lib/node_modules/md-to-pdf/node_modules'],
}));

const MERMAID_CDN = 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js';

const MERMAID_INIT_SCRIPT = `
<script src="${MERMAID_CDN}"></script>
<script>
    document.addEventListener('DOMContentLoaded', function() {
        mermaid.initialize({ startOnLoad: false, theme: 'default' });

        var codeBlocks = document.querySelectorAll('pre code.mermaid, pre code.language-mermaid');
        for (var i = 0; i < codeBlocks.length; i++) {
            var el = codeBlocks[i];
            var pre = el.parentElement;
            var div = document.createElement('div');
            div.className = 'mermaid';
            div.textContent = el.textContent;
            pre.replaceWith(div);
        }

        var mermaidDivs = document.querySelectorAll('.mermaid');
        if (mermaidDivs.length > 0) {
            mermaid.run({ nodes: mermaidDivs }).then(function() {
                document.body.setAttribute('data-mermaid-done', 'true');
            }).catch(function(err) {
                console.error('Mermaid error:', err);
                document.body.setAttribute('data-mermaid-done', 'true');
            });
        } else {
            document.body.setAttribute('data-mermaid-done', 'true');
        }
    });
</script>
`;

function startServer(dir) {
    return new Promise((resolve) => {
        const server = http.createServer((req, res) => {
            const filePath = path.join(dir, decodeURIComponent(req.url));
            if (fs.existsSync(filePath)) {
                const ext = path.extname(filePath);
                const types = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript' };
                res.writeHead(200, { 'Content-Type': types[ext] || 'application/octet-stream' });
                fs.createReadStream(filePath).pipe(res);
            } else {
                res.writeHead(404);
                res.end('Not found');
            }
        });
        server.listen(0, () => resolve(server));
    });
}

async function convertFile(mdPath) {
    const baseName = path.basename(mdPath, '.md');
    const dir = path.dirname(path.resolve(mdPath));
    const htmlPath = path.join(dir, baseName + '.html');
    const pdfPath = path.join(dir, baseName + '.pdf');

    // Step 1: Use md-to-pdf to generate HTML
    console.log(`[${baseName}] Generating HTML...`);
    execSync(`md-to-pdf --as-html "${mdPath}"`, { stdio: 'pipe' });

    // Step 2: Inject Mermaid scripts into the HTML
    let html = fs.readFileSync(htmlPath, 'utf8');

    // Remove any previously injected mermaid scripts from md-to-pdf config
    html = html.replace(/<script[^>]*mermaid[^>]*>[\s\S]*?<\/script>/gi, '');

    // Inject mermaid CDN + init script before </body>
    html = html.replace('</body>', MERMAID_INIT_SCRIPT + '\n</body>');
    fs.writeFileSync(htmlPath, html);

    // Step 3: Serve via local HTTP to avoid file:// CORS issues, open in Puppeteer
    const server = await startServer(dir);
    const port = server.address().port;

    console.log(`[${baseName}] Rendering Mermaid diagrams...`);
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();

    page.on('console', msg => {
        if (msg.type() === 'error') console.error(`  [browser] ${msg.text()}`);
    });

    await page.goto(`http://localhost:${port}/${baseName}.html`, {
        waitUntil: 'networkidle0',
        timeout: 60000,
    });

    // Wait for Mermaid rendering to complete
    await page.waitForFunction(
        () => document.body.getAttribute('data-mermaid-done') === 'true',
        { timeout: 30000 }
    );

    console.log(`[${baseName}] Generating PDF...`);
    await page.pdf({
        path: pdfPath,
        format: 'A4',
        margin: { top: '20mm', bottom: '20mm', left: '15mm', right: '15mm' },
        printBackground: true,
    });

    await browser.close();
    server.close();

    // Clean up HTML
    fs.unlinkSync(htmlPath);

    const size = (fs.statSync(pdfPath).size / 1024).toFixed(0);
    console.log(`[${baseName}] Done → ${pdfPath} (${size} KB)`);
}

(async () => {
    const files = process.argv.slice(2);
    if (files.length === 0) {
        console.error('Usage: node md-to-pdf-mermaid.js <file.md> [file2.md ...]');
        process.exit(1);
    }

    for (const file of files) {
        await convertFile(file);
    }
})();
