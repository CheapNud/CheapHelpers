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

// Reconnection: retries run silently with exponential backoff (about two minutes in total), so
// short drops such as a mobile tab switch never show any UI. A circuit the server no longer knows
// reloads the page at once. Only when every retry fails does #reconnect-modal show a short
// "Connection lost." message with a Refresh button; the page never reloads on its own then.
(() => {
    const maximumRetryCount = 8;
    const baseIntervalMilliseconds = 1000 * 3;
    const maximumIntervalMilliseconds = 1000 * 30;
    const maximumJitterFraction = 0.2;
    const reconnectModalId = 'reconnect-modal';

    const defaultStyleElementId = 'cheaphelpers-reconnect-style';
    // :where() has zero specificity, so any app rule on the element (even a bare `div`) or an
    // inline style overrides these defaults.
    const defaultStyles =
        ':where(#reconnect-modal) { position: fixed; bottom: 16px; right: 16px; z-index: 9999; display: flex; ' +
        'align-items: center; gap: 12px; padding: 10px 16px; border-radius: 8px; ' +
        'border: 1px solid rgba(255,255,255,0.15); background: rgba(30,30,30,0.95); color: #e0e0e0; font-size: 0.85rem; } ' +
        ':where(#reconnect-modal button) { cursor: pointer; color: inherit; }';

    let hasWarnedMissingModal = false;

    const retryInterval = attempt => {
        const interval = Math.min(baseIntervalMilliseconds * 2 ** attempt, maximumIntervalMilliseconds);
        return interval + interval * maximumJitterFraction * Math.random();
    };

    const injectDefaultStyles = () => {
        if (document.getElementById(defaultStyleElementId)) {
            return;
        }

        const styleElement = document.createElement('style');
        styleElement.id = defaultStyleElementId;
        styleElement.textContent = defaultStyles;
        document.head.appendChild(styleElement);
    };

    const showReconnectDialog = () => {
        // Looked up now rather than at load, so markup rendered after this script still works.
        const reconnectModal = document.getElementById(reconnectModalId);
        if (!reconnectModal) {
            if (!hasWarnedMissingModal) {
                hasWarnedMissingModal = true;
                console.warn(`Blazor reconnection failed and no #${reconnectModalId} element exists to show it.`);
            }
            return;
        }

        reconnectModal.innerHTML = '<span>Connection lost.</span><button type="button">Refresh</button>';
        reconnectModal.querySelector('button').addEventListener('click', () => location.reload());

        injectDefaultStyles();
        // The markup hides the element inline; only lift that, so an app's own display value stays.
        if (!reconnectModal.style.display || reconnectModal.style.display === 'none') {
            reconnectModal.style.display = 'flex';
        }
    };

    const hideReconnectDialog = () => {
        const reconnectModal = document.getElementById(reconnectModalId);
        if (reconnectModal) {
            reconnectModal.style.display = 'none';
            reconnectModal.innerHTML = '';
        }
    };

    const startReconnectionProcess = () => {
        let isCanceled = false;

        (async () => {
            for (let attempt = 0; attempt < maximumRetryCount; attempt++) {
                // Attempt first: the common case reconnects on the first try with no wait.
                try {
                    const result = await Blazor.reconnect();
                    if (!result) {
                        // The server answered but the circuit is gone; only a reload recovers.
                        location.reload();
                    }

                    return;
                } catch {
                    // Did not reach the server; wait, then try again.
                }

                if (isCanceled) {
                    return;
                }

                if (attempt < maximumRetryCount - 1) {
                    await new Promise(resolve => setTimeout(resolve, retryInterval(attempt)));

                    if (isCanceled) {
                        return;
                    }
                }
            }

            showReconnectDialog();
        })();

        return {
            cancel: () => {
                isCanceled = true;
                hideReconnectDialog();
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
