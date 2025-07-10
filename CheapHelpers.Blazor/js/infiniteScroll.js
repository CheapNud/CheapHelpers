window.infiniteScroll = {
    observer: null,
    mutationObserver: null,
    resizeObserver: null,

    setupInfiniteScroll: function (dotNetObject) {
        if (dotNetObject && typeof dotNetObject.invokeMethodAsync === 'function') {
            const self = this;

            function checkScroll() {

                if (!self.hasMoreItems) {
                    console.log("No more items to load. Stopping infinite scroll.");
                    return;
                }

                const scrollTop = window.scrollY || document.documentElement.scrollTop;
                const windowHeight = window.innerHeight;
                const fullHeight = document.documentElement.scrollHeight;

                console.log("Scroll Position:", scrollTop, "Window Height:", windowHeight, "Full Height:", fullHeight);

                // Check if user is at the bottom (adjusted threshold to ensure it triggers)
                const atBottom = scrollTop + windowHeight >= fullHeight - 50;

                if (atBottom) {
                    console.log("Reached Bottom! Triggering LoadStock...");
                    dotNetObject.invokeMethodAsync("LoadStock").catch(err => console.error("Error invoking LoadStock:", err));
                }
            }

            // Check if the page has enough content to scroll
            if (!this.isPageScrollable()) {
                console.log("Page not scrollable. Triggering LoadStock...");
                dotNetObject.invokeMethodAsync("LoadStock").catch(err => console.error("Error invoking LoadStock:", err));
            }

            // Attach scroll event listener
            window.addEventListener("scroll", checkScroll);
            console.log("Infinite Scroll Initialized!");
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
        window.addEventListener("scroll", function () {
            // Get current scroll position
            const scrollTop = window.scrollY || document.documentElement.scrollTop;
            const windowHeight = window.innerHeight;
            const fullHeight = document.documentElement.scrollHeight;

            // Check if we scrolled to the bottom
            const atBottom = scrollTop + windowHeight >= fullHeight - 5;

            if (atBottom && dotNetObject && dotNetObject.invokeMethodAsync) {
                dotNetObject.invokeMethodAsync("HandleVisibilityChanged", true);
            }
        });
    },

    cleanupMutationObserver: function () {
        if (this.mutationObserver) {
            this.mutationObserver.disconnect();
            this.mutationObserver = null;
        }
    },

    stopObserving: function () {
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
