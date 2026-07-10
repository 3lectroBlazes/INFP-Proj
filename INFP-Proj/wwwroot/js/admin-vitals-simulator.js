(function () {
    const panel = document.getElementById('vitals-simulator');
    if (!panel) {
        return;
    }

    const patientId = panel.dataset.patientId;
    const simulateUrl = panel.dataset.simulateUrl;
    const tokenInput = document.querySelector('#vitals-simulator-antiforgery input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    const statusBox = document.getElementById('simulation-status');
    const statusText = document.getElementById('simulation-status-text');
    const stopBtn = document.getElementById('simulation-stop');
    const simButtons = panel.querySelectorAll('.sim-btn');

    const DURATION_SECONDS = 60;
    const TICK_SECONDS = 5;

    let timerId = null;
    let elapsed = 0;

    function setButtonsDisabled(disabled) {
        simButtons.forEach(function (btn) {
            btn.disabled = disabled;
        });
    }

    function stopSimulation(reload) {
        if (timerId) {
            clearInterval(timerId);
            timerId = null;
        }
        setButtonsDisabled(false);
        statusBox.hidden = true;
        if (reload) {
            window.location.reload();
        }
    }

    function sendTick(vital, direction) {
        const body = new URLSearchParams();
        body.set('patientId', patientId);
        body.set('vital', vital);
        body.set('direction', direction);
        body.set('__RequestVerificationToken', token);

        return fetch(simulateUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: body.toString()
        });
    }

    function startSimulation(vital, direction, label) {
        if (timerId) {
            return;
        }

        elapsed = 0;
        setButtonsDisabled(true);
        statusBox.hidden = false;

        function tick() {
            sendTick(vital, direction);
            elapsed += TICK_SECONDS;
            const remaining = Math.max(DURATION_SECONDS - elapsed, 0);
            statusText.textContent = 'Simulating ' + label + '... ' + remaining + 's remaining.';

            if (elapsed >= DURATION_SECONDS) {
                stopSimulation(true);
            }
        }

        tick();
        timerId = setInterval(tick, TICK_SECONDS * 1000);
    }

    simButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            startSimulation(btn.dataset.vital, btn.dataset.direction, btn.dataset.label);
        });
    });

    stopBtn.addEventListener('click', function () {
        stopSimulation(true);
    });
})();
