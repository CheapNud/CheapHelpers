window.blazorIsConnected = function () {
    // Check if Blazor exists and has internal components initialized
    if (!Blazor || !Blazor._internal || !Blazor._internal.navigationManager) {
        return false; // Explicitly return false instead of undefined/null
    }

    // Return true only if explicitly connected
    return Blazor._internal.navigationManager._isConnected === true;
};

window.registerConnectionRestoredCallback = function (dotNetRef, methodName) {
    window.connectionCallbacks = window.connectionCallbacks || [];
    window.connectionCallbacks.push({ dotNetRef, methodName });
};

(() => {
    const maximumRetryCount = 8;
    const retryIntervalMilliseconds = 1000 * 3;
    // Fast reconnects (mobile tab switches background the socket and restore it in under a
    // second) must resolve invisibly — the modal only appears when we're still down after the
    // grace period. Popping UI for a sub-second blip is worse than the blip.
    const modalGraceMilliseconds = 1000 * 3;
    const reconnectModal = document.getElementById('reconnect-modal');

    const startReconnectionProcess = () => {
        let isCanceled = false;

        const modalTimer = setTimeout(() => {
            if (!isCanceled && reconnectModal) {
                reconnectModal.style.display = 'block';
            }
        }, modalGraceMilliseconds);

        (async () => {
            for (let i = 0; i < maximumRetryCount; i++) {
                if (reconnectModal) {
                    reconnectModal.innerText = `Attempting to reconnect: ${i + 1} of ${maximumRetryCount}`;
                }

                // Attempt FIRST — the common case reconnects on try one with no wait;
                // onConnectionUp then cancels the pending modal.
                try {
                    const result = await Blazor.reconnect();
                    if (!result) {
                        // The server was reached, but the connection was rejected; reload the page.
                        location.reload();
                        return;
                    }

                    // Successfully reconnected to the server.
                    return;
                } catch {
                    // Didn't reach the server; wait, then try again.
                }

                await new Promise(resolve => setTimeout(resolve, retryIntervalMilliseconds));

                if (isCanceled) {
                    return;
                }
            }

            // Retried too many times; reload the page.
            location.reload();
        })();

        return {
            cancel: () => {
                isCanceled = true;
                clearTimeout(modalTimer);
                if (reconnectModal) {
                    reconnectModal.style.display = 'none';
                }
            },
        };
    };

    let currentReconnectionProcess = null;

    const circuitOptions = {
        configureSignalR: function (builder) {
            builder.withServerTimeout(60 * 1000).withKeepAliveInterval(30 * 1000);
        },
        reconnectionHandler: {
            onConnectionDown: () => currentReconnectionProcess ??= startReconnectionProcess(),
            onConnectionUp: () => {
                currentReconnectionProcess?.cancel();
                currentReconnectionProcess = null;

                // Call all registered reconnection callbacks
                if (window.connectionCallbacks && window.connectionCallbacks.length > 0) {
                    window.connectionCallbacks.forEach(callback => {
                        if (callback.dotNetRef && typeof callback.dotNetRef.invokeMethodAsync === 'function') {
                            callback.dotNetRef.invokeMethodAsync(callback.methodName)
                                .catch(error => console.error("Error invoking reconnection callback:", error));
                        }
                    });
                }
            }
        }
    };

    // Dual-shape start: pre-.NET8 blazor.server.js reads circuit options at the top level;
    // .NET 8+ blazor.web.js reads them nested under `circuit`. Each loader ignores the
    // other's shape, so passing both works on either.
    Blazor.start({ ...circuitOptions, circuit: circuitOptions });
})();
