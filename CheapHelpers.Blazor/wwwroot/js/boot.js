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
    const maximumRetryCount = 3;
    const retryIntervalMilliseconds = 1000 * 3;
    const reconnectModal = document.getElementById('reconnect-modal');

    const startReconnectionProcess = () => {
        reconnectModal.style.display = 'block';

        let isCanceled = false;

        (async () => {
            for (let i = 0; i < maximumRetryCount; i++) {
                reconnectModal.innerText = `Attempting to reconnect: ${i + 1} of ${maximumRetryCount}`;

                await new Promise(resolve => setTimeout(resolve, retryIntervalMilliseconds));

                if (isCanceled) {
                    return;
                }

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
                    // Didn't reach the server; try again.
                }
            }

            // Retried too many times; reload the page.
            location.reload();
        })();

        return {
            cancel: () => {
                isCanceled = true;
                reconnectModal.style.display = 'none';
            },
        };
    };

    let currentReconnectionProcess = null;

    Blazor.start({
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
    });
})();