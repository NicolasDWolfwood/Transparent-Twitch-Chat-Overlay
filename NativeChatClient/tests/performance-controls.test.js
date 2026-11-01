const test = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../TransparentTwitchChatWPF/Resources/browser-performance.js'), 'utf8');
function fixture(fps) {
  const pending = new Map(), elements = new Map(); let id = 0;
  const document = {
    documentElement: { appendChild(el) { elements.set(el.id, el); } },
    getElementById(key) { return elements.get(key); }, createElement() { return {}; }, addEventListener() {}
  };
  const window = {
    requestAnimationFrame(fn) { const key = ++id; pending.set(key, fn); return key; },
    cancelAnimationFrame(key) { pending.delete(key); }, dispatchEvent() {}
  };
  vm.runInNewContext(source, { window, document, CustomEvent: class {}, setTimeout });
  window.ttcoPerformance.configure({ maximumFps: fps });
  return { window, elements, frame(time) { const batch = [...pending.values()]; pending.clear(); for (const fn of batch) fn(time); } };
}
test('animation limit serves multiple widgets fairly without touching network timers', () => {
  const f = fixture(15); let first = 0, second = 0;
  function a() { first++; f.window.requestAnimationFrame(a); }
  function b() { second++; f.window.requestAnimationFrame(b); }
  f.window.requestAnimationFrame(a); f.window.requestAnimationFrame(b);
  for (let i = 0; i < 60; i++) f.frame(i * 1000 / 60);
  assert.equal(first, 15); assert.equal(second, 15);
});
test('unlimited mode preserves every display frame and cancellation', () => {
  const f = fixture(0); let count = 0;
  function update() { count++; f.window.requestAnimationFrame(update); }
  const cancelled = f.window.requestAnimationFrame(() => { throw new Error('cancel failed'); });
  f.window.cancelAnimationFrame(cancelled); f.window.requestAnimationFrame(update);
  for (let i = 0; i < 60; i++) f.frame(i * 1000 / 60);
  assert.equal(count, 60);
});
test('reduced motion is reversible and does not overwrite a page stylesheet', () => {
  const f = fixture(0);
  f.window.ttcoPerformance.configure({ reducedMotion: true, maximumFps: 30 });
  assert.match(f.elements.get('ttco-reduced-motion').textContent, /animation:none/);
  f.window.ttcoPerformance.configure({ reducedMotion: false, maximumFps: 0 });
  assert.equal(f.elements.get('ttco-reduced-motion').textContent, '');
});
