// Actualización periódica de fragmentos (mapa de mesas y tablero de cocina).
// Cada contenedor declara data-refresh-url y data-refresh-seconds (parámetro UI.REFRESCO_SEGUNDOS).
(function () {
    document.querySelectorAll('[data-refresh-url]').forEach(function (contenedor) {
        var url = contenedor.getAttribute('data-refresh-url');
        var segundos = parseInt(contenedor.getAttribute('data-refresh-seconds') || '0', 10);
        if (!url || !segundos) { return; }
        var ultimo = contenedor.innerHTML.trim();

        setInterval(async function () {
            if (document.hidden) { return; }
            // No interrumpir al usuario mientras interactúa
            if (document.querySelector('.modal.show, .dropdown-menu.show')) { return; }
            try {
                var respuesta = await fetch(url, { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                if (respuesta.redirected || !respuesta.ok) { return; }
                var html = (await respuesta.text()).trim();
                if (html !== ultimo) {
                    contenedor.innerHTML = html;
                    ultimo = html;
                }
            } catch (e) { /* sin conexión: se reintenta en el siguiente ciclo */ }
        }, segundos * 1000);
    });
})();
