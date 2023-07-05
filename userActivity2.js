function startIdleTimer(dotNetRef, timeout) {
    let idleTimeout = null;

    function idleCallback() {
        dotNetRef.invokeMethodAsync('UserIdle');
    }

    function resetIdleTimer() {
        if (idleTimeout) {
            clearTimeout(idleTimeout);
        }
        idleTimeout = setTimeout(idleCallback, timeout);
    }

    function registerEventListeners(dotNetRef) {
        document.addEventListener('mousemove', resetIdleTimer);
        document.addEventListener('keydown', resetIdleTimer);
    }

    resetIdleTimer();
    registerEventListeners(dotNetRef);
}

function clearIdleTimer() {
    clearTimeout(idleTimeout);
}

window.startIdleTimer = startIdleTimer;
window.resetIdleTimer = resetIdleTimer;
window.registerEventListeners = registerEventListeners;
