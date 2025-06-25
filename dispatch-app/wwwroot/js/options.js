(() => {
    const btnDetener = document.getElementById('btnDetener');
    const btnCerrarSesion = document.getElementById('btnCerrarSesion');
    const btnSiguiente = document.getElementById('btnSiguienteDespachar');
    const modalDescansoEl = document.getElementById('modalDescanso');
    const modalDescanso = new bootstrap.Modal(modalDescansoEl);
    const timerStartSeconds = 120;
    let timerSeconds = timerStartSeconds;
    let countdownInterval = null;
    let autoTriggered = false;

    function resetTimer() {
        timerSeconds = timerStartSeconds;
        updateButtonLabel(timerSeconds);
        autoTriggered = false;
        if (countdownInterval !== null) {
            clearInterval(countdownInterval);
            countdownInterval = null;
        }
    }

    function updateButtonLabel(seconds) {
        btnSiguiente.textContent = `Siguiente a despachar (${seconds} seg)`;
    }

    function runCountdown() {
        updateButtonLabel(timerSeconds);
        countdownInterval = setInterval(() => {
            timerSeconds--;
            if (timerSeconds <= 0) {
                clearInterval(countdownInterval);
                countdownInterval = null;
                if (!autoTriggered) {
                    autoTriggered = true;
                    triggerSiguiente();
                }
                return;
            }
            updateButtonLabel(timerSeconds);
        }, 1000);
    }

    function triggerSiguiente() {
        // Disable buttons to prevent multiple triggers
        btnSiguiente.disabled = true;
        btnDetener.disabled = true;
        btnCerrarSesion.disabled = true;
        // Redirect to Dispatch -> Index controller route
        window.location.href = '/Dispatch/Index';
    }

    btnDetener.addEventListener('click', () => {
        modalDescanso.show();
    });

    btnDetener.addEventListener('focus', resetTimer);
    btnCerrarSesion.addEventListener('focus', resetTimer);
    btnSiguiente.addEventListener('focus', resetTimer);

    // Reset timer if Detener or Cerrar Sesion clicked
    btnDetener.addEventListener('click', () => {
        // On detener, reset and pause countdown until modal closes or option selected
        if (countdownInterval) {
            clearInterval(countdownInterval);
            countdownInterval = null;
        }
        updateButtonLabel(timerSeconds);
    });

    btnCerrarSesion.addEventListener('click', () => {
        // Reset timer and stop countdown since user acted
        resetTimer();

        window.location.href = '/Security/Login';
        //alert('Cerrar sesión acción no implementada.');
    });

    modalDescansoEl.addEventListener('hidden.bs.modal', () => {
        // Restart countdown when modal closes without selecting
        if (!autoTriggered && !countdownInterval) {
            runCountdown();
        }
    });

    // Handle time option selection
    document.getElementById('opcionesTiempo').addEventListener('click', (e) => {
        if (e.target.classList.contains('time-option')) {
            const minutes = e.target.getAttribute('data-minutes');
            modalDescanso.hide();
            resetTimer();
            alert(`Has seleccionado un descanso de ${minutes} minutos.`);
            // After selection restart countdown
            runCountdown();
        }
    });

    btnSiguiente.addEventListener('click', () => {
        resetTimer();
        triggerSiguiente();
    });

    // Initialize DataTables and start countdown on DOM ready
    document.addEventListener('DOMContentLoaded', () => {
        $('#tablaOrdenes').DataTable({
            language: {
                url:
                    '//cdn.datatables.net/plug-ins/1.13.4/i18n/es-ES.json',
            },
            pagingType: 'simple',
            pageLength: 5,
            lengthMenu: [5, 10, 25, 50],
            order: [[3, 'desc']],
            columnDefs: [
                { type: 'num', targets: 0 },
                { type: 'string', targets: 1 },
                { type: 'num', targets: 2 },
                {
                    type: 'date',
                    targets: 3,
                    render: function (data, type, row) {
                        if (type === 'sort' || type === 'type') {
                            return data;
                        }
                        const d = new Date(data);
                        return d.toLocaleDateString('es-ES', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                        });
                    },
                },
            ],
            responsive: true,
            info: false,
            searching: true,
        });

        runCountdown();
    });
})();