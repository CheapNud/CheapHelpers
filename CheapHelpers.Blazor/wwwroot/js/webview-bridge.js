/**
 * Generic MAUI/Blazor Hybrid WebView Bridge
 * Version: 1.0.0
 * Purpose: Extract data from WebView storage/cookies/DOM for Blazor Hybrid apps
 * Supports: MAUI, Photino, Avalonia, any WebView-based Blazor host
 */

(function () {
    'use strict';

    // Generic WebView Bridge configuration
    const defaultConfig = {
        namespace: 'WebViewBridge',           // Global namespace for the bridge
        pollingIntervalMs: 3000,              // How often to check for changes
        storageKeys: [],                      // Keys to monitor in localStorage/sessionStorage
        cookieNames: [],                      // Cookie names to monitor
        domSelectors: [],                     // DOM selectors for data extraction
        debug: false,                         // Enable debug logging
        autoInit: true,                       // Auto-initialize on load
        extractionStrategies: {               // Which strategies to use
            localStorage: true,
            sessionStorage: true,
            cookies: true,
            dom: true,
            url: true,
            globals: true
        }
    };

    // Bridge instance
    window.createWebViewBridge = function (userConfig) {
        const config = Object.assign({}, defaultConfig, userConfig);

        const bridge = {
            config: config,
            currentData: null,
            lastDataJson: null,
            pollingInterval: null,
            lastCheck: null,

            /**
             * Initialize the bridge
             */
            init: function () {
                if (config.debug) {
                    console.log(`[${config.namespace}] Initializing WebView Bridge`);
                    console.log(`[${config.namespace}] Location:`, window.location.href);
                    console.log(`[${config.namespace}] Monitoring keys:`, config.storageKeys);
                }

                this.setupMonitoring();
                this.setupEventListeners();
                this.checkDataState();

                if (config.debug) {
                    console.log(`[${config.namespace}] Bridge initialized and operational`);
                    this.logEnvironmentInfo();
                }
            },

            /**
             * Log environment debug info
             */
            logEnvironmentInfo: function () {
                console.log(`[${config.namespace}] Environment:`);
                console.log('  - URL:', window.location.href);
                console.log('  - LocalStorage keys:', Object.keys(localStorage));
                console.log('  - SessionStorage keys:', Object.keys(sessionStorage));
                console.log('  - Cookies available:', document.cookie ? 'YES' : 'NO');
            },

            /**
             * Setup polling for data changes
             */
            setupMonitoring: function () {
                if (this.pollingInterval) {
                    clearInterval(this.pollingInterval);
                }

                this.pollingInterval = setInterval(() => {
                    this.checkDataState();
                }, config.pollingIntervalMs);

                if (config.debug) {
                    console.log(`[${config.namespace}] Polling active (${config.pollingIntervalMs}ms intervals)`);
                }
            },

            /**
             * Stop monitoring
             */
            stopMonitoring: function () {
                if (this.pollingInterval) {
                    clearInterval(this.pollingInterval);
                    this.pollingInterval = null;
                }
            },

            /**
             * Check if data has changed
             */
            checkDataState: function () {
                try {
                    const extractedData = this.extractData();
                    const dataJson = JSON.stringify(extractedData);

                    // Check if data has changed
                    if (dataJson !== this.lastDataJson) {
                        if (config.debug) {
                            console.log(`[${config.namespace}] Data change detected`);
                            console.log('  New data:', extractedData);
                        }

                        this.currentData = extractedData;
                        this.lastDataJson = dataJson;
                        this.lastCheck = new Date().toISOString();

                        this.onDataChanged(extractedData);
                    } else if (config.debug && Math.random() < 0.05) {
                        // Occasional debug output (5% chance)
                        console.log(`[${config.namespace}] Data check: no changes`);
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] Data check failed:`, error);
                }
            },

            /**
             * Extract data from all configured sources
             */
            extractData: function () {
                const data = {
                    extractedFrom: [],
                    timestamp: new Date().toISOString(),
                    rawData: {}
                };

                try {
                    if (config.extractionStrategies.localStorage) {
                        this.extractFromLocalStorage(data);
                    }
                    if (config.extractionStrategies.sessionStorage) {
                        this.extractFromSessionStorage(data);
                    }
                    if (config.extractionStrategies.cookies) {
                        this.extractFromCookies(data);
                    }
                    if (config.extractionStrategies.dom) {
                        this.extractFromDOM(data);
                    }
                    if (config.extractionStrategies.url) {
                        this.extractFromURL(data);
                    }
                    if (config.extractionStrategies.globals) {
                        this.extractFromGlobals(data);
                    }

                    if (config.debug && data.extractedFrom.length > 0) {
                        console.log(`[${config.namespace}] Extracted from:`, data.extractedFrom);
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] Extraction failed:`, error);
                    data.error = error.message;
                }

                return data;
            },

            /**
             * Extract from localStorage
             */
            extractFromLocalStorage: function (data) {
                try {
                    const localData = {};

                    for (const key of config.storageKeys) {
                        try {
                            const value = localStorage.getItem(key);
                            if (value !== null) {
                                localData[key] = this.tryParseJson(value);
                                data.extractedFrom.push(`local:${key}`);

                                // Also set on data root for convenience
                                if (!data[key]) {
                                    data[key] = localData[key];
                                }
                            }
                        } catch (e) {
                            console.warn(`[${config.namespace}] localStorage access failed for key ${key}:`, e);
                        }
                    }

                    data.rawData.localStorage = localData;
                } catch (error) {
                    console.error(`[${config.namespace}] localStorage extraction failed:`, error);
                }
            },

            /**
             * Extract from sessionStorage
             */
            extractFromSessionStorage: function (data) {
                try {
                    const sessionData = {};

                    for (const key of config.storageKeys) {
                        try {
                            const value = sessionStorage.getItem(key);
                            if (value !== null) {
                                sessionData[key] = this.tryParseJson(value);
                                if (!data.extractedFrom.includes(`session:${key}`)) {
                                    data.extractedFrom.push(`session:${key}`);
                                }

                                // Set on data root if not already set by localStorage
                                if (!data[key]) {
                                    data[key] = sessionData[key];
                                }
                            }
                        } catch (e) {
                            console.warn(`[${config.namespace}] sessionStorage access failed for key ${key}:`, e);
                        }
                    }

                    data.rawData.sessionStorage = sessionData;
                } catch (error) {
                    console.error(`[${config.namespace}] sessionStorage extraction failed:`, error);
                }
            },

            /**
             * Extract from cookies
             */
            extractFromCookies: function (data) {
                try {
                    const cookies = this.parseCookies();
                    data.rawData.cookies = cookies;

                    for (const cookieName of config.cookieNames) {
                        if (cookies[cookieName]) {
                            if (!data[cookieName]) {
                                data[cookieName] = cookies[cookieName];
                            }
                            data.extractedFrom.push(`cookie:${cookieName}`);
                        }
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] Cookie extraction failed:`, error);
                }
            },

            /**
             * Extract from DOM elements
             */
            extractFromDOM: function (data) {
                try {
                    for (const selector of config.domSelectors) {
                        try {
                            const element = document.querySelector(selector);
                            if (element) {
                                const value = element.dataset?.value ||
                                    element.textContent?.trim() ||
                                    element.value ||
                                    element.getAttribute('data-value');

                                if (value && value.length > 0 && value !== 'null') {
                                    const key = selector.replace(/[^a-zA-Z0-9]/g, '_');
                                    if (!data[key]) {
                                        data[key] = this.tryParseJson(value);
                                    }
                                    data.extractedFrom.push(`dom:${selector}`);
                                }
                            }
                        } catch (e) {
                            console.warn(`[${config.namespace}] DOM extraction failed for selector ${selector}:`, e);
                        }
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] DOM extraction failed:`, error);
                }
            },

            /**
             * Extract from URL parameters
             */
            extractFromURL: function (data) {
                try {
                    const params = new URLSearchParams(window.location.search);
                    const hashParams = new URLSearchParams(window.location.hash.substring(1));

                    for (const key of config.storageKeys) {
                        const urlValue = params.get(key) || hashParams.get(key);
                        if (urlValue && !data[key]) {
                            data[key] = this.tryParseJson(urlValue);
                            data.extractedFrom.push(`url:${key}`);
                        }
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] URL extraction failed:`, error);
                }
            },

            /**
             * Extract from global variables
             */
            extractFromGlobals: function (data) {
                try {
                    for (const key of config.storageKeys) {
                        if (window[key] !== undefined && !data[key]) {
                            data[key] = window[key];
                            data.extractedFrom.push(`global:${key}`);
                        }
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] Global variable extraction failed:`, error);
                }
            },

            /**
             * Try to parse JSON value
             */
            tryParseJson: function (value) {
                if (typeof value !== 'string') {
                    return value;
                }

                // Handle WebView's multiple levels of JSON escaping
                let cleaned = value;
                let previousCleaned = '';
                let iterations = 0;
                const maxIterations = 5;

                // Keep unescaping until no more changes or max iterations
                while (cleaned !== previousCleaned && iterations < maxIterations) {
                    previousCleaned = cleaned;

                    // Remove outer quotes if present
                    if (cleaned.startsWith('"') && cleaned.endsWith('"')) {
                        cleaned = cleaned.slice(1, -1);
                    }

                    // Unescape JSON quotes
                    cleaned = cleaned.replace(/\\"/g, '"');

                    // Handle backslash escaping
                    if (cleaned.includes('\\\\')) {
                        cleaned = cleaned.replace(/\\\\/g, '\\');
                    }

                    iterations++;
                }

                // Try to parse as JSON
                if (cleaned.startsWith('{') || cleaned.startsWith('[')) {
                    try {
                        return JSON.parse(cleaned);
                    } catch (e) {
                        // Not valid JSON, return cleaned string
                        return cleaned;
                    }
                }

                return cleaned;
            },

            /**
             * Parse document cookies into object
             */
            parseCookies: function () {
                try {
                    return document.cookie.split(';').reduce((cookies, cookie) => {
                        const [name, ...rest] = cookie.trim().split('=');
                        const value = rest.join('=');
                        if (name && value) {
                            cookies[decodeURIComponent(name)] = decodeURIComponent(value);
                        }
                        return cookies;
                    }, {});
                } catch (error) {
                    console.error(`[${config.namespace}] Cookie parsing failed:`, error);
                    return {};
                }
            },

            /**
             * Data change handler
             */
            onDataChanged: function (data) {
                if (config.debug) {
                    console.log(`[${config.namespace}] Broadcasting data change`);
                }

                // Update DOM attributes for C# access
                this.updateDOMAttributes(data);

                // Dispatch custom event
                this.dispatchDataEvent(data);

                // Callback for C# integration
                this.notifyHost(data);
            },

            /**
             * Update DOM attributes for C# polling
             */
            updateDOMAttributes: function (data) {
                try {
                    const prefix = `data-${config.namespace.toLowerCase()}`;
                    document.body.setAttribute(`${prefix}-timestamp`, Date.now().toString());
                    document.body.setAttribute(`${prefix}-sources`, data.extractedFrom.join(','));
                    document.body.setAttribute(`${prefix}-data`, JSON.stringify(data));

                    if (config.debug) {
                        console.log(`[${config.namespace}] DOM attributes updated for C# access`);
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] Failed to update DOM attributes:`, error);
                }
            },

            /**
             * Dispatch custom event
             */
            dispatchDataEvent: function (data) {
                try {
                    const eventName = `${config.namespace}DataChanged`;
                    const event = new CustomEvent(eventName, {
                        detail: {
                            data: data,
                            timestamp: Date.now()
                        }
                    });
                    document.dispatchEvent(event);

                    if (config.debug) {
                        console.log(`[${config.namespace}] Custom event dispatched:`, eventName);
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] Failed to dispatch event:`, error);
                }
            },

            /**
             * Notify host application (MAUI/Photino/Avalonia)
             */
            notifyHost: function (data) {
                try {
                    const callbackName = `${config.namespace}Callback`;
                    if (window[callbackName]) {
                        window[callbackName](JSON.stringify(data));
                        if (config.debug) {
                            console.log(`[${config.namespace}] Host callback notified`);
                        }
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] Failed to notify host:`, error);
                }
            },

            /**
             * Setup event listeners
             */
            setupEventListeners: function () {
                try {
                    // Navigation change detection
                    this.setupNavigationMonitoring();

                    // Storage change monitoring
                    this.setupStorageMonitoring();

                    // Cookie change monitoring
                    this.setupCookieMonitoring();

                    if (config.debug) {
                        console.log(`[${config.namespace}] Event listeners configured`);
                    }
                } catch (error) {
                    console.error(`[${config.namespace}] Failed to setup event listeners:`, error);
                }
            },

            setupNavigationMonitoring: function () {
                let lastUrl = location.href;
                setInterval(() => {
                    const currentUrl = location.href;
                    if (currentUrl !== lastUrl) {
                        lastUrl = currentUrl;
                        if (config.debug) {
                            console.log(`[${config.namespace}] Navigation detected, checking data`);
                        }
                        setTimeout(() => this.checkDataState(), 1000);
                    }
                }, 2000);

                window.addEventListener('popstate', () => {
                    if (config.debug) {
                        console.log(`[${config.namespace}] Popstate detected, checking data`);
                    }
                    setTimeout(() => this.checkDataState(), 500);
                });
            },

            setupStorageMonitoring: function () {
                try {
                    window.addEventListener('storage', (e) => {
                        if (config.storageKeys.includes(e.key)) {
                            if (config.debug) {
                                console.log(`[${config.namespace}] Storage change detected:`, e.key);
                            }
                            setTimeout(() => this.checkDataState(), 500);
                        }
                    });
                } catch (error) {
                    console.warn(`[${config.namespace}] Storage monitoring not available:`, error);
                }
            },

            setupCookieMonitoring: function () {
                let lastCookies = document.cookie;
                setInterval(() => {
                    const currentCookies = document.cookie;
                    if (currentCookies !== lastCookies) {
                        lastCookies = currentCookies;
                        if (config.debug) {
                            console.log(`[${config.namespace}] Cookie change detected`);
                        }
                        setTimeout(() => this.checkDataState(), 500);
                    }
                }, 2000);
            },

            /**
             * Public API methods
             */
            getCurrentData: function () {
                return this.currentData;
            },

            forceCheck: function () {
                if (config.debug) {
                    console.log(`[${config.namespace}] Forcing data check...`);
                }
                this.checkDataState();
            },

            getLastCheck: function () {
                return this.lastCheck;
            },

            getAllStorageData: function (storageType = 'localStorage') {
                const storage = storageType === 'localStorage' ? localStorage : sessionStorage;
                const data = {};
                for (let i = 0; i < storage.length; i++) {
                    const key = storage.key(i);
                    if (key) {
                        data[key] = this.tryParseJson(storage.getItem(key));
                    }
                }
                return data;
            }
        };

        // Auto-initialize if configured
        if (config.autoInit) {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => bridge.init());
            } else {
                bridge.init();
            }
        }

        // Store in global namespace
        window[config.namespace] = bridge;

        return bridge;
    };

    // Make factory globally available
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = window.createWebViewBridge;
    }

    console.log('✅ WebView Bridge factory loaded. Use createWebViewBridge(config) to initialize.');

})();
