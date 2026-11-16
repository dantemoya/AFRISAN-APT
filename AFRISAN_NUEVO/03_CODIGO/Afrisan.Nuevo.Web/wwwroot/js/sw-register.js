// Registro del service worker, en archivo propio para no usar script inline (permite una CSP estricta sin 'unsafe-inline').
if ('serviceWorker' in navigator) navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).catch(() => {});
