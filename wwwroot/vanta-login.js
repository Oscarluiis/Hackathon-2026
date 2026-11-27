// Fondo Vanta NET solo para el login. Three.js y Vanta se cargan bajo demanda.
(() => {
    const THREE_URL = "https://cdnjs.cloudflare.com/ajax/libs/three.js/r134/three.min.js";
    const VANTA_URL = "https://cdn.jsdelivr.net/npm/vanta@latest/dist/vanta.net.min.js";
    let instancia = null, cargando = null, token = 0;

    const cargar = (src) => new Promise((ok, fail) => {
        const s = document.createElement("script");
        s.src = src; s.async = true;
        s.onload = ok; s.onerror = () => fail(new Error("No se pudo cargar " + src));
        document.head.appendChild(s);
    });

    const asegurarLibs = () => cargando ??= (async () => {
        if (!window.THREE) await cargar(THREE_URL);      // Vanta necesita THREE global antes
        if (!window.VANTA?.NET) await cargar(VANTA_URL);
    })().catch(e => { cargando = null; throw e; });

    window.vantaLogin = {
        async init(selector) {
            const mio = ++token;
            await asegurarLibs();
            if (mio !== token || !document.querySelector(selector)) return; // se salió de la página mientras cargaba
            instancia?.destroy();
            instancia = VANTA.NET({
                el: selector,
                mouseControls: true, touchControls: true, gyroControls: false,
                minHeight: 200.0, minWidth: 200.0, scale: 1.0, scaleMobile: 1.0,
                color: 0x95071f, backgroundColor: 0xF7F8FA
            });
        },
        destroy() {
            token++;
            if (instancia) { instancia.destroy(); instancia = null; }
        }
    };
})();
