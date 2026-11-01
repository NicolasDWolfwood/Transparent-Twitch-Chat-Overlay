const test = require('node:test');
const assert = require('node:assert/strict');
const Connection = require('../src/v2/connection-health');
test('connection status lives inside the browser page and reuses one accessible badge', () => {
  const elements = new Map(), messages = [];
  const document = {
    getElementById: id => elements.get(id),
    createElement: () => ({ style: {}, attributes: {}, setAttribute(key, value) { this.attributes[key] = value; } }),
    body: { appendChild(element) { elements.set(element.id, element); } }
  };
  const host = { postMessage: message => messages.push(message) };
  Connection.reportState({ state: 'reconnecting' }, host, document);
  const badge = elements.get('ttco-connection-status');
  assert.match(badge.textContent, /disconnected/);
  assert.equal(badge.attributes.role, 'status');
  Connection.reportState({ state: 'connected' }, host, document);
  assert.equal(elements.size, 1);
  assert.equal(badge.textContent, 'Chat connected');
  assert.equal(messages[1].type, 'NativeChatConnection');
  assert.equal(messages[1].state, 'connected');
});
