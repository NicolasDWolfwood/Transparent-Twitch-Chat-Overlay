(function (root, factory) {
  const Connection = factory();
  if (typeof module === 'object' && module.exports) module.exports = Connection;
  else root.TTCOChatConnection = Connection;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  return class ChatConnection {
    static reportState(details, host, document = globalThis.document) {
      host?.postMessage({ type: 'NativeChatConnection', ...details });
      if (!document?.body) return;
      let badge = document.getElementById('ttco-connection-status');
      if (!badge) {
        badge = document.createElement('div');
        badge.id = 'ttco-connection-status';
        badge.setAttribute('role', 'status');
        badge.style.cssText = 'position:fixed;top:6px;right:6px;z-index:2147483647;padding:4px 7px;border-radius:4px;background:rgba(25,25,25,.85);font:12px Segoe UI,sans-serif;pointer-events:none;';
        document.body.appendChild(badge);
      }
      const labels = { connected: 'Chat connected', connecting: 'Connecting chat…', joining: 'Joining chat…', loading: 'Preparing chat…', reconnecting: 'Chat disconnected — retrying…', closed: 'Chat disconnected', 'invalid-channel': 'Set a valid channel in Settings' };
      badge.textContent = labels[details.state] || 'Connecting chat…';
      badge.style.color = details.state === 'connected' ? '#9ee6ac' : '#ffd478';
    }
    static prepareMetadata(lookup, apply, ready, options = {}) {
      const clock = options.clock || globalThis;
      let finished = false;
      const finish = () => {
        if (finished) return;
        finished = true; clock.clearTimeout(timer); ready(true);
      };
      const timer = clock.setTimeout(finish, options.timeoutMs || 10000);
      return Promise.resolve().then(lookup).then(apply).catch(error => options.onError?.(error)).finally(finish);
    }
    constructor(url, protocol, options = {}) {
      this.url = url; this.protocol = protocol; this.options = options;
      this.clock = options.clock || globalThis;
      this.now = options.now || (() => Date.now());
      this.WebSocket = options.WebSocket || globalThis.WebSocket;
      this.attempt = 0; this.generation = 0; this.stopped = false;
      this.events = options.events || (typeof window !== 'undefined' ? window : null);
      this.resume = () => this.reconnect('network-restored', true);
      this.events?.addEventListener('online', this.resume);
      this.timer = this.clock.setInterval(() => this.checkHealth(), options.checkIntervalMs || 5000);
      this.connect();
    }
    setState(state, reason = '', retryInMs = 0) {
      this.state = state; this.stateSince = this.now();
      this.options.onState?.({ state, reason, retryInMs, attempt: this.attempt });
    }
    connect() {
      if (this.stopped) return;
      this.clock.clearTimeout(this.retry);
      const generation = ++this.generation;
      this.setState('connecting');
      this.lastReceived = this.lastCheck = this.now();
      this.pendingPing = null;
      try { this.socket = new this.WebSocket(this.url, this.protocol); }
      catch { this.reconnect('connection-failed'); return; }
      const socket = this.socket;
      socket.onopen = event => {
        if (generation !== this.generation || this.stopped) return;
        this.lastReceived = this.now();
        this.setState('joining');
        this.onopen?.(event);
      };
      socket.onmessage = event => {
        if (generation !== this.generation || this.stopped) return;
        this.lastReceived = this.now(); this.pendingPing = null;
        if (/(^|\r\n)(?:@[^ ]+ |:[^ ]+ )?RECONNECT(?: |\r\n|$)/.test(String(event.data))) {
          this.reconnect('server-requested'); return;
        }
        this.onmessage?.(event);
      };
      socket.onclose = event => {
        if (generation !== this.generation || this.stopped) return;
        this.onclose?.(event);
        this.reconnect('disconnected');
      };
      socket.onerror = () => {
        if (generation === this.generation && !this.stopped) this.reconnect('connection-failed');
      };
    }
    markJoined() {
      if (this.stopped) return;
      this.attempt = 0;
      this.setState('connected');
    }
    send(value) {
      if (this.stopped || this.socket?.readyState !== 1) return false;
      try { this.socket.send(value); return true; }
      catch { this.reconnect('send-failed'); return false; }
    }
    checkHealth() {
      if (this.stopped || this.state === 'reconnecting') return;
      const now = this.now();
      const resumed = now - this.lastCheck > 30000;
      this.lastCheck = now;
      if (resumed && this.socket?.readyState === 1) {
        this.pendingPing = null; this.lastReceived = now - 45000;
      }
      if (this.state === 'connecting' && now - this.lastReceived > 15000) {
        this.reconnect('connection-timeout'); return;
      }
      if (this.state === 'joining' && now - this.stateSince > 20000) {
        this.reconnect('join-timeout'); return;
      }
      if (this.socket?.readyState !== 1) return;
      if (this.pendingPing !== null && now - this.pendingPing >= 15000) {
        this.reconnect('heartbeat-timeout'); return;
      }
      if (this.pendingPing === null && now - this.lastReceived >= 45000) {
        if (this.send('PING :ttco-heartbeat\r\n')) this.pendingPing = now;
      }
    }
    reconnect(reason, immediate = false) {
      if (this.stopped) return;
      ++this.generation;
      this.clock.clearTimeout(this.retry);
      try { this.socket?.close(); } catch { }
      const delay = immediate ? 0 : Math.min(30000, 2000 * Math.pow(2, Math.min(this.attempt, 4)));
      this.attempt++;
      this.setState('reconnecting', reason, delay);
      this.retry = this.clock.setTimeout(() => this.connect(), delay);
    }
    close() {
      if (this.stopped) return;
      this.stopped = true; ++this.generation;
      this.clock.clearTimeout(this.retry); this.clock.clearInterval(this.timer);
      this.events?.removeEventListener('online', this.resume);
      try { this.socket?.close(); } catch { }
      this.setState('closed');
    }
  };
});
