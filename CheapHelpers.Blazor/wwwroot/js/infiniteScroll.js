window.infiniteScroll = {
    observer: null,
    mutationObserver: null,
    resizeObserver: null,
    scrollHandler: null,
    visibilityScrollHandler: null,

    setupInfiniteScroll: function (dotNetObject, methodName) {
        if (dotNetObject && typeof dotNetObject.invokeMethodAsync === 'function') {
            const self = this;
            const callback = methodName || "LoadStock";

            function checkScroll() {
                if (!self.hasMoreItems) {
                    return;
                }

                const scrollTop = window.scrollY || document.documentElement.scrollTop;
                const windowHeight = window.innerHeight;
                const fullHeight = document.documentElement.scrollHeight;

                // Check if user is at the bottom (adjusted threshold to ensure it triggers)
                const atBottom = scrollTop + windowHeight >= fullHeight - 50;

                if (atBottom) {
                    dotNetObject.invokeMethodAsync(callback).catch(err => console.error(`Error invoking ${callback}:`, err));
                }
            }

            // Check if the page has enough content to scroll
            if (!this.isPageScrollable()) {
                dotNetObject.invokeMethodAsync(callback).catch(err => console.error(`Error invoking ${callback}:`, err));
            }

            // Attach scroll event listener, keeping the reference so stopObserving can remove it.
            // Remove any previous one first so repeated setup calls don't stack listeners.
            if (this.scrollHandler) {
                window.removeEventListener("scroll", this.scrollHandler);
            }
            this.scrollHandler = checkScroll;
            window.addEventListener("scroll", checkScroll);
        }
        else {
            console.error("Invalid dotNetObject provided to setupInfiniteScroll");
        }
    },

    updateHasMoreItems: function (value) {
        this.hasMoreItems = value === true;
    },

    isPageScrollable: function () {
        return document.documentElement.scrollHeight > window.innerHeight;
    },

    startObserving: function (dotNetObject) {
        const handler = function () {
            // Get current scroll position
            const scrollTop = window.scrollY || document.documentElement.scrollTop;
            const windowHeight = window.innerHeight;
            const fullHeight = document.documentElement.scrollHeight;

            // Check if we scrolled to the bottom
            const atBottom = scrollTop + windowHeight >= fullHeight - 5;

            if (atBottom && dotNetObject && dotNetObject.invokeMethodAsync) {
                dotNetObject.invokeMethodAsync("HandleVisibilityChanged", true);
            }
        };

        if (this.visibilityScrollHandler) {
            window.removeEventListener("scroll", this.visibilityScrollHandler);
        }
        this.visibilityScrollHandler = handler;
        window.addEventListener("scroll", handler);
    },

    cleanupMutationObserver: function () {
        if (this.mutationObserver) {
            this.mutationObserver.disconnect();
            this.mutationObserver = null;
        }
    },

    stopObserving: function () {
        // Clean up scroll listeners
        if (this.scrollHandler) {
            window.removeEventListener("scroll", this.scrollHandler);
            this.scrollHandler = null;
        }

        if (this.visibilityScrollHandler) {
            window.removeEventListener("scroll", this.visibilityScrollHandler);
            this.visibilityScrollHandler = null;
        }

        // Clean up IntersectionObserver
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }

        // Clean up MutationObserver
        this.cleanupMutationObserver();

        // Clean up ResizeObserver
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
            this.resizeObserver = null;
        }
    },
};
