(() => {
    if (window.ttcoPerformance) return;
    const nativeRequest = window.requestAnimationFrame.bind(window);
    const nativeCancel = window.cancelAnimationFrame.bind(window);
    const callbacks = new Map();
    let nextId = 0, pending = null, lastFrame = -Infinity, maximumFps = 0;
    function schedule() {
        if (pending === null && callbacks.size) pending = nativeRequest(tick);
    }
    function tick(time) {
        pending = null;
        if (!maximumFps || time - lastFrame >= 1000 / maximumFps - 1) {
            lastFrame = time;
            const batch = [...callbacks];
            for (const [id, callback] of batch) {
                if (!callbacks.delete(id)) continue;
                try { callback(time); } catch (error) { setTimeout(() => { throw error; }, 0); }
            }
        }
        schedule();
    }
    window.requestAnimationFrame = callback => {
        if (typeof callback !== 'function') throw new TypeError('Animation callback must be a function');
        const id = ++nextId;
        callbacks.set(id, callback);
        schedule();
        return id;
    };
    window.cancelAnimationFrame = id => {
        callbacks.delete(id);
        if (!callbacks.size && pending !== null) { nativeCancel(pending); pending = null; }
    };
    let preferences = {};
    function applyStyle() {
        if (!document.documentElement) return;
        let style = document.getElementById('ttco-reduced-motion');
        if (!style) {
            style = document.createElement('style');
            style.id = 'ttco-reduced-motion';
            document.documentElement.appendChild(style);
        }
        style.textContent = preferences.reducedMotion
            ? '*,*::before,*::after{animation:none!important;transition:none!important;scroll-behavior:auto!important}' : '';
    }
    window.ttcoPerformance = {
        configure(value) {
            preferences = value || {};
            maximumFps = [0, 15, 30, 60].includes(preferences.maximumFps) ? preferences.maximumFps : 0;
            lastFrame = -Infinity;
            applyStyle();
            window.dispatchEvent(new CustomEvent('ttco-performance', { detail: preferences }));
        },
        get preferences() { return { ...preferences }; }
    };
    document.addEventListener('DOMContentLoaded', applyStyle, { once: true });
})();
