const test = require('node:test');
const assert = require('node:assert/strict');
const Connection = require('../src/v2/connection-health');

function fixture() {
  let time = 0, nextId = 0;
  const jobs = new Map(), sockets = [], states = [];
  const clock = {
    setTimeout(fn, delay) { const id = ++nextId; jobs.set(id, { fn, at: time + delay }); return id; },
    clearTimeout(id) { jobs.delete(id); },
    setInterval(fn, delay) { const id = ++nextId; jobs.set(id, { fn, at: time + delay, repeat: delay }); return id; },
    clearInterval(id) { jobs.delete(id); },
    advance(duration) {
      const end = time + duration;
      for (let count = 0; count < 10000; count++) {
        const due = [...jobs].filter(([, job]) => job.at <= end).sort((a, b) => a[1].at - b[1].at)[0];
        if (!due) { time = end; return; }
        const [id, job] = due; time = job.at;
        if (job.repeat) job.at += job.repeat; else jobs.delete(id);
        job.fn();
      }
      throw new Error('Timer loop did not settle');
    }
  };
  class Socket {
    constructor() { this.readyState = 0; this.sent = []; sockets.push(this); }
    open() { this.readyState = 1; this.onopen?.({}); }
    message(data) { this.onmessage?.({ data }); }
    send(data) { this.sent.push(data); }
    close() { this.readyState = 3; this.onclose?.({}); }
  }
  const handlers = new Map();
  const events = { addEventListener(n, fn) { handlers.set(n, fn); }, removeEventListener(n) { handlers.delete(n); } };
  const connection = new Connection('wss://test', 'irc', { WebSocket: Socket, clock, now: () => time, events,
    onState: state => states.push(state) });
  return { connection, clock, sockets, states, jobs, handlers, jump(duration) { time += duration; } };
}

test('a channel is not connected until JOIN is acknowledged', () => {
  const f = fixture(); f.sockets[0].open();
  assert.equal(f.connection.state, 'joining');
  f.connection.markJoined(); assert.equal(f.connection.state, 'connected'); f.connection.close();
});
test('a quiet channel responds to heartbeats without being reconnected', () => {
  const f = fixture(); f.sockets[0].open(); f.connection.markJoined();
  f.clock.advance(45000); assert.match(f.sockets[0].sent[0], /^PING/);
  f.sockets[0].message(':server PONG :ttco-heartbeat\r\n'); f.clock.advance(20000);
  assert.equal(f.sockets.length, 1); assert.equal(f.connection.state, 'connected'); f.connection.close();
});
test('a half-open socket reconnects after an unanswered heartbeat', () => {
  const f = fixture(); f.sockets[0].open(); f.connection.markJoined(); f.clock.advance(60000);
  assert.equal(f.connection.state, 'reconnecting'); assert.equal(f.states.at(-1).reason, 'heartbeat-timeout');
  f.clock.advance(2000); assert.equal(f.sockets.length, 2); f.connection.close();
});
test('disconnect resets status and stale socket callbacks cannot affect its replacement', () => {
  const f = fixture(); const old = f.sockets[0]; old.open(); f.connection.markJoined(); old.close();
  assert.equal(f.connection.state, 'reconnecting'); f.clock.advance(2000);
  const replacement = f.sockets[1]; replacement.open(); f.connection.markJoined();
  old.onclose({}); old.onerror({}); old.message('RECONNECT\r\n');
  assert.equal(f.connection.state, 'connected'); assert.equal(f.sockets.length, 2); f.connection.close();
});
test('retry delays increase and stay capped at 30 seconds', () => {
  const f = fixture(); const delays = [];
  for (let i = 0; i < 8; i++) {
    f.sockets.at(-1).close(); const delay = f.states.at(-1).retryInMs; delays.push(delay); f.clock.advance(delay);
  }
  assert.deepEqual(delays, [2000, 4000, 8000, 16000, 30000, 30000, 30000, 30000]); f.connection.close();
});
test('Twitch RECONNECT requests trigger recovery', () => {
  const f = fixture(); f.sockets[0].open(); f.connection.markJoined(); f.sockets[0].message(':tmi.twitch.tv RECONNECT\r\n');
  assert.equal(f.states.at(-1).reason, 'server-requested'); f.connection.close();
});
test('connection and JOIN timeouts recover without waiting for a close event', () => {
  const f = fixture(); f.clock.advance(20000); assert.equal(f.states.at(-1).reason, 'connection-timeout');
  f.clock.advance(2000); f.sockets.at(-1).open(); f.clock.advance(25000);
  assert.equal(f.states.at(-1).reason, 'join-timeout'); f.connection.close();
});
test('closing clears timers, retries and network listeners', () => {
  const f = fixture(); f.sockets[0].close(); f.connection.close(); f.clock.advance(100000);
  assert.equal(f.jobs.size, 0); assert.equal(f.handlers.size, 0); assert.equal(f.sockets.length, 1);
});
test('network restoration retries immediately', () => {
  const f = fixture(); f.sockets[0].close(); f.handlers.get('online')(); f.clock.advance(0);
  assert.equal(f.sockets.length, 2); f.connection.close();
});
test('send failure is recovered rather than escaping into the page', () => {
  const f = fixture(); f.sockets[0].open(); f.sockets[0].send = () => { throw new Error('device disconnected'); };
  assert.equal(f.connection.send('test'), false); assert.equal(f.connection.state, 'reconnecting'); f.connection.close();
});
test('resume from sleep probes the socket before deciding to reconnect', () => {
  const f = fixture(); f.sockets[0].open(); f.connection.markJoined();
  f.clock.advance(45000); f.jump(120000); f.connection.checkHealth();
  assert.equal(f.connection.state, 'connected');
  f.sockets[0].message(':server PONG :ttco-heartbeat\r\n');
  assert.equal(f.connection.pendingPing, null); f.connection.close();
});
test('server traffic without a JOIN acknowledgement cannot stall forever', () => {
  const f = fixture(); f.sockets[0].open();
  f.clock.advance(10000); f.sockets[0].message('PING :server\r\n');
  f.clock.advance(10000); f.sockets[0].message('PING :server\r\n');
  f.clock.advance(5000); assert.equal(f.states.at(-1).reason, 'join-timeout'); f.connection.close();
});
test('failed optional metadata does not prevent chat startup', async () => {
  let ready = 0;
  await Connection.prepareMetadata(() => Promise.reject(new Error('API unavailable')), () => {}, () => ready++);
  assert.equal(ready, 1);
});
test('slow metadata cannot hold chat startup or connect it twice', async () => {
  const f = fixture(); let resolve, ready = 0, applied = 0;
  const lookup = new Promise(done => { resolve = done; });
  const operation = Connection.prepareMetadata(() => lookup, () => applied++, () => ready++, { clock: f.clock, timeoutMs: 1000 });
  await Promise.resolve(); f.clock.advance(1000); assert.equal(ready, 1);
  resolve({ data: [] }); await operation;
  assert.equal(ready, 1); assert.equal(applied, 1); f.connection.close();
});
