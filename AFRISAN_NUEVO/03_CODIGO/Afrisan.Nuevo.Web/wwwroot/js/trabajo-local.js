// Registros y fotos separados: un listado nunca materializa las imágenes.
window.afrisanLocal = (() => {
    const indice = 'porTipoUsuario';
    const clave = (tipo, usuario, id) => `${tipo}:${usuario}:${id}`;
    const separar = (store, fotos, registro) => {
        const { valor, usuario } = registro;
        for (const foto of valor.fotos || []) {
            fotos.put({ clave: clave('foto', usuario, `${valor.datos.id}:${foto.id}`),
                usuario, borrador: valor.datos.id, valor: foto });
        }
        store.put({ ...registro, valor: { ...valor, fotos: [] } });
    };
    const abrir = () => new Promise((resolve, reject) => {
        const req = indexedDB.open('afrisan-nuevo-trabajo', 2);
        req.onupgradeneeded = () => {
            const db = req.result;
            const registros = db.objectStoreNames.contains('registros')
                ? req.transaction.objectStore('registros')
                : db.createObjectStore('registros', { keyPath: 'clave' });
            registros.createIndex(indice, ['tipo', 'usuario']);
            const fotos = db.createObjectStore('fotos', { keyPath: 'clave' });
            fotos.createIndex('porBorrador', ['usuario', 'borrador']);
            // Actualización transaccional de los borradores anteriores, sin perder fotos.
            const cursor = registros.openCursor();
            cursor.onsuccess = () => {
                const item = cursor.result;
                if (!item) return;
                if (item.value.tipo === 'borrador') separar(registros, fotos, item.value);
                item.continue();
            };
        };
        req.onsuccess = () => {
            req.result.onversionchange = () => req.result.close();
            resolve(req.result);
        };
        req.onerror = () => reject(new Error('No se pudo abrir el almacenamiento local.'));
        req.onblocked = () => reject(new Error('Cierra otras pestañas de AFRISAN para actualizar el almacenamiento local.'));
    });
    const guardar = async (tipo, usuario, id, valor) => {
        const db = await abrir();
        try {
            await new Promise((resolve, reject) => {
                const tx = db.transaction(tipo === 'borrador' ? ['registros', 'fotos'] : ['registros'], 'readwrite');
                const registros = tx.objectStore('registros');
                const registro = { clave: clave(tipo, usuario, id), tipo, usuario, valor };
                if (tipo === 'borrador') {
                    const fotos = tx.objectStore('fotos');
                    const actuales = new Set((valor.fotos || []).map(f => clave('foto', usuario, `${id}:${f.id}`)));
                    const req = fotos.index('porBorrador').openKeyCursor(IDBKeyRange.only([usuario, id]));
                    req.onsuccess = () => {
                        const item = req.result;
                        if (!item) { separar(registros, fotos, registro); return; }
                        if (!actuales.has(item.primaryKey)) fotos.delete(item.primaryKey);
                        item.continue();
                    };
                } else registros.put(registro);
                tx.oncomplete = resolve;
                tx.onerror = () => reject(new Error('No se pudo conservar el borrador/foto. Revisa el espacio del dispositivo.'));
                tx.onabort = tx.onerror;
            });
        } finally { db.close(); }
    };
    const listar = async (tipo, usuario) => {
        const db = await abrir();
        try {
            return await new Promise((resolve, reject) => {
                const valores = [];
                const tx = db.transaction('registros', 'readonly');
                const req = tx.objectStore('registros').index(indice)
                    .openCursor(IDBKeyRange.only([tipo, usuario]));
                req.onsuccess = () => {
                    const item = req.result;
                    if (!item) return;
                    valores.push(item.value.valor);
                    item.continue();
                };
                tx.oncomplete = () => resolve(valores);
                tx.onerror = () => reject(new Error('No se pudieron consultar los registros locales.'));
            });
        } finally { db.close(); }
    };
    const obtener = async (tipo, usuario, id) => {
        const db = await abrir();
        try {
            return await new Promise((resolve, reject) => {
                const tx = db.transaction(tipo === 'borrador' ? ['registros', 'fotos'] : ['registros'], 'readonly');
                let valor = null;
                const req = tx.objectStore('registros').get(clave(tipo, usuario, id));
                req.onsuccess = () => {
                    valor = req.result?.valor ?? null;
                    if (tipo !== 'borrador' || !valor) return;
                    const cursor = tx.objectStore('fotos').index('porBorrador')
                        .openCursor(IDBKeyRange.only([usuario, id]));
                    cursor.onsuccess = () => {
                        const item = cursor.result;
                        if (!item) return;
                        valor.fotos.push(item.value.valor);
                        item.continue();
                    };
                };
                tx.oncomplete = () => resolve(valor);
                tx.onerror = () => reject(new Error('No se pudo recuperar el borrador con sus fotos.'));
            });
        } finally { db.close(); }
    };
    const suscripciones = new Map();
    return {
        guardar, listar, obtener,
        conectado: () => navigator.onLine,
        suscribir: (id, receptor) => {
            const handler = () => receptor.invokeMethodAsync('ConexionRecuperada').catch(() => {});
            window.addEventListener('online', handler); suscripciones.set(id, handler);
        },
        cancelar: id => { const handler = suscripciones.get(id); if (handler) window.removeEventListener('online', handler); suscripciones.delete(id); }
    };
})();
