import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { test } from 'node:test';

const site = new URL('../sites/', import.meta.url);
const files = ['index.html', 'privacy.html'];
const read = name => readFileSync(new URL(name, site), 'utf8');
const text = value => value
  .replace(/<\/(?:h2|p|li|th|td)>/g, ' ')
  .replace(/<[^>]*>/g, '')
  .replace(/\s+/g, ' ').trim();

for (const file of files) {
  test(`${file}: accessible static document and project-relative links`, () => {
    const html = read(file);
    assert.match(html, /<html lang="zh-Hant">/);
    assert.match(html, /<meta name="viewport" content="width=device-width, initial-scale=1">/);
    assert.equal((html.match(/<h1(?:\s|>)/g) ?? []).length, 1);
    assert.match(html, /<main\b[^>]*id="main"/);
    assert.match(html, /<main\b[^>]*tabindex="-1"/);
    assert.match(html, /class="skip-link" href="#main"/);
    assert.doesNotMatch(html, /<(script|iframe|form)\b/i);
    const ids = [...html.matchAll(/\bid="([^"]+)"/g)].map(match => match[1]);
    assert.equal(new Set(ids).size, ids.length, 'IDs must be unique');

    for (const [, attribute, href] of html.matchAll(/\b(href|src)="([^"]+)"/g)) {
      if (href.startsWith('https:') || href.startsWith('mailto:')) {
        assert.equal(attribute, 'href', 'No external embedded resources');
        continue;
      }
      assert.ok(!href.startsWith('/'), `Root-relative link breaks project sites: ${href}`);
      const target = new URL(href, new URL(file, site));
      assert.ok(target.href.startsWith(site.href), `Link escapes public site: ${href}`);
      const hash = target.hash.slice(1);
      target.hash = '';
      assert.ok(existsSync(target), `Missing local target: ${href}`);
      if (hash) {
        assert.ok(readFileSync(target, 'utf8').includes(`id="${hash}"`), `Missing anchor: ${href}`);
      }
    }
  });
}

test('published policy matches every approved body paragraph, table cell, link and section in order', () => {
  const markdown = readFileSync(new URL('../docs/privacy-policy.md', import.meta.url), 'utf8');
  const body = markdown.split('## 1.')[1].split('\n---')[0];
  const approved = `## 1.${body}`;
  const publicBody = read('privacy.html').match(/<article id="policy-content"[^>]*>([\s\S]*?)<\/article>/)[1];
  const units = approved.split(/\r?\n/).flatMap(line => {
    if (!line.trim() || /^\|\s*-/.test(line)) return [];
    return line.startsWith('|') ? line.split('|').slice(1, -1) : [line];
  }).map(line => line
    .replace(/^#+\s*/, '')
    .replace(/^- /, '')
    .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
    .replace(/\*\*/g, '')
    .replace(/\s+/g, ' ').trim());
  assert.equal(text(publicBody), units.join(' '), 'Policy copy must remain synchronized');
  for (const [, href] of approved.matchAll(/\]\(([^)]+)\)/g)) {
    assert.ok(publicBody.includes(`href="${href}"`), `Missing policy link: ${href}`);
  }
  assert.match(publicBody, /<time datetime="2026-10-08">2026-10-08<\/time>/);
  assert.doesNotMatch(publicBody, /待產品擁有者|內部備註|待完成|尚未發布或生效/);
});

test('site has no external CSS resources or unverified download offer', () => {
  assert.doesNotMatch(read('styles.css'), /@import|url\s*\(/i);
  assert.match(read('index.html'), /Microsoft Store 尚未上架/);
  assert.doesNotMatch(read('index.html'), /立即購買|立即下載/);
  assert.match(read('privacy.html'), /GitHub 隱私權聲明/);
});
