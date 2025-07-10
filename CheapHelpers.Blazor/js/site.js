// Enhanced culture handling for site.js

window.throttleTouchEvents = true; // Global flag to enable/disable throttling
window.lastTouchMoveTime = 0;


// Global culture information object
window.appCulture = {
    current: null,
    isInitialized: false,
    decimalSeparator: '.',
    useCommaDecimalSeparator: false
};

// Enhanced getBoundingClientRect that ensures culture-invariant values
window.getBoundingClientRect = function (element) {
    if (!element) {
        console.error("No element passed to getBoundingClientRect");
        return { left: 0, top: 0, width: 0, height: 0, right: 0, bottom: 0 };
    }

    try {
        const rect = element.getBoundingClientRect();

        // Parse all values to ensure they're proper numbers regardless of culture
        return {
            left: parseFloat(rect.left.toFixed(2)),  // Use toFixed for consistent precision
            top: parseFloat(rect.top.toFixed(2)),
            width: parseFloat(rect.width.toFixed(2)),
            height: parseFloat(rect.height.toFixed(2)),
            right: parseFloat(rect.right.toFixed(2)),
            bottom: parseFloat(rect.bottom.toFixed(2))
        };
    } catch (err) {
        console.error("Error in getBoundingClientRect:", err);
        return { left: 0, top: 0, width: 0, height: 0, right: 0, bottom: 0 };
    }
};

// Ensure any value is a proper number regardless of culture
window.ensureNumber = function (value) {
    if (typeof value === 'number') return value;

    try {
        // If we have culture info, use it
        if (window.appCulture && window.appCulture.isInitialized) {
            return window.parseInvariantNumber(value);
        }

        // Otherwise, just try to parse it directly
        return parseFloat(value) || 0;
    } catch (err) {
        console.error("Error in ensureNumber:", err, "for value:", value);
        return 0;
    }
};

// Update the existing document event handlers
function setupDocumentMouseHandlers(dotNetRef) {
    console.log("Setting up document mouse handlers");

    // Remove any existing event listeners to prevent duplicates
    document.removeEventListener('mouseup', documentMouseUpHandler);
    document.body.removeEventListener('mouseleave', documentMouseLeaveHandler);

    // Store the dotNetRef globally so we can access it in our handlers
    window.plannerDotNetRef = dotNetRef;

    // Add the handlers
    document.addEventListener('mouseup', documentMouseUpHandler);
    document.body.addEventListener('mouseleave', documentMouseLeaveHandler);

    console.log("Document mouse handlers setup complete");
}

// Separate handler functions for easier removal
function documentMouseUpHandler(e) {
    console.log("Document mouseup triggered");
    if (window.plannerDotNetRef) {
        try {
            window.plannerDotNetRef.invokeMethodAsync('HandleDocumentMouseUp')
                .then(() => console.log("HandleDocumentMouseUp completed"))
                .catch(err => console.error("Error calling HandleDocumentMouseUp:", err));
        } catch (err) {
            console.error("Exception in documentMouseUpHandler:", err);
        }
    } else {
        console.warn("No dotNetRef available for mouse up handler");
    }
}

function documentMouseLeaveHandler(e) {
    // Only trigger if mouse leaves to top/left/right edges of the window
    if (e.clientY <= 0 || e.clientX <= 0 || e.clientX >= window.innerWidth) {
        console.log("Document mouseleave triggered");
        if (window.plannerDotNetRef) {
            try {
                window.plannerDotNetRef.invokeMethodAsync('HandleDocumentMouseUp')
                    .then(() => console.log("HandleDocumentMouseUp completed"))
                    .catch(err => console.error("Error calling HandleDocumentMouseUp:", err));
            } catch (err) {
                console.error("Exception in documentMouseLeaveHandler:", err);
            }
        } else {
            console.warn("No dotNetRef available for mouse leave handler");
        }
    }
}

window.updateItemVisualPosition = function (itemId, x, y, rotation) {
    try {
        // Find the DOM element
        const element = document.querySelector(`[data-ui-id="${itemId}"]`);
        if (!element) return false;

        // Use RAF for smoother visual updates
        requestAnimationFrame(() => {
            // Apply visual updates with hardware acceleration
            element.style.transform = `translate3d(${x}px, ${y}px, 0) rotate(${ro});tation}deg)`;
        });

        return true;
    } catch (err) {
        console.error("Error updating item position:", err);
        return false;
    }
};

// Initialize culture settings - call this from your Blazor component
window.initializeCulture = function (cultureName) {
    console.log("Initializing culture settings for: " + cultureName);

    // Store the culture name
    window.appCulture = window.appCulture || {};
    window.appCulture.current = cultureName;

    // Detect decimal separator based on culture
    let testNumber = 1.1;
    let formatter = new Intl.NumberFormat(cultureName);
    let formattedNumber = formatter.format(testNumber);

    // Check if this culture uses comma as decimal separator
    window.appCulture.useCommaDecimalSeparator = formattedNumber.indexOf(',') !== -1;
    window.appCulture.decimalSeparator = window.appCulture.useCommaDecimalSeparator ? ',' : '.';

    console.log("Culture settings detected: ", JSON.stringify(window.appCulture));
    console.log("Sample formatting: 1.1 →", formattedNumber);
    window.appCulture.isInitialized = true;

    // Apply some monkey patches to ensure consistent number handling
    patchStringFormatting();

    return window.appCulture;
};

function patchStringFormatting() {
    // Save the original methods
    if (!Number.prototype._originalToString) {
        Number.prototype._originalToString = Number.prototype.toString;

        // Override toString to use invariant culture (period as decimal)
        Number.prototype.toString = function () {
            // Always use a period decimal separator for technical operations
            return this._originalToString.apply(this)
                .replace(',', '.'); // Ensure any commas are converted to periods
        };

        console.log("Applied Number.toString patch");
    }

    // Patch toFixed to ensure consistent decimal handling
    if (!Number.prototype._originalToFixed) {
        Number.prototype._originalToFixed = Number.prototype.toFixed;

        // Override toFixed to use period for internal operations
        Number.prototype.toFixed = function (digits) {
            // Always use period for technical string operations
            return this._originalToFixed.call(this, digits)
                .replace(',', '.'); // Ensure any commas are converted to periods
        };

        console.log("Applied Number.toFixed patch");
    }

    console.log("String formatting patches applied");
}

// Add these new functions to your site.js file
window.parseCoordinates = function (coordString) {
    if (!coordString) return { x: 0, y: 0 };

    console.log("Parsing coordinates from:", coordString);

    try {
        // First, clean up the string
        coordString = coordString.trim();

        // Handle different formats
        let x = 0, y = 0;

        // Handle "(x,y)" format
        const parenMatch = coordString.match(/\(([^,]+)[,;]([^)]+)\)/);
        if (parenMatch) {
            x = window.parseInvariantNumber(parenMatch[1]);
            y = window.parseInvariantNumber(parenMatch[2]);
            console.log("Parsed from parentheses format:", x, y);
            return { x, y };
        }

        // Handle "x,y" format
        const commaMatch = coordString.match(/^([^,;]+)[,;]([^,;]+)$/);
        if (commaMatch) {
            x = window.parseInvariantNumber(commaMatch[1]);
            y = window.parseInvariantNumber(commaMatch[2]);
            console.log("Parsed from comma format:", x, y);
            return { x, y };
        }

        // Handle problematic "260,5,50" format (from comma decimal cultures)
        // which should be interpreted as x=260.5, y=50
        const specialMatch = coordString.match(/^(\d+),(\d+),(\d+)$/);
        if (specialMatch) {
            // In this case, the first two parts form the x value with a decimal
            x = window.parseInvariantNumber(specialMatch[1] + '.' + specialMatch[2]);
            y = window.parseInvariantNumber(specialMatch[3]);
            console.log("Parsed from special comma-decimal format:", x, y);
            return { x, y };
        }

        console.warn("Could not parse coordinates, using default 0,0");
        return { x: 0, y: 0 };
    } catch (err) {
        console.error("Error parsing coordinates:", err);
        return { x: 0, y: 0 };
    }
};

// Culture-aware mouse position calculation
window.calculateMousePosition = function (clientX, clientY, bounds, mouseOffsetX, mouseOffsetY,
    currentX, currentY, rotation, width, height, maxWidth, maxHeight) {
    try {
        // Ensure all input values are proper numbers
        clientX = window.parseInvariantNumber(clientX);
        clientY = window.parseInvariantNumber(clientY);
        bounds.left = window.parseInvariantNumber(bounds.left);
        bounds.top = window.parseInvariantNumber(bounds.top);
        mouseOffsetX = window.parseInvariantNumber(mouseOffsetX);
        mouseOffsetY = window.parseInvariantNumber(mouseOffsetY);
        currentX = window.parseInvariantNumber(currentX);
        currentY = window.parseInvariantNumber(currentY);
        rotation = window.parseInvariantNumber(rotation);
        width = window.parseInvariantNumber(width);
        height = window.parseInvariantNumber(height);
        maxWidth = window.parseInvariantNumber(maxWidth);
        maxHeight = window.parseInvariantNumber(maxHeight);

        // Calculate mouse position relative to floor plan
        const mouseX = clientX - bounds.left;
        const mouseY = clientY - bounds.top;

        // Calculate new position
        let newX = mouseX - mouseOffsetX;
        let newY = mouseY - mouseOffsetY;

        // Get current item dimensions based on rotation
        const rotationNormalized = Math.round(rotation / 90) * 90 % 360;
        const isRotated = (rotationNormalized === 90 || rotationNormalized === 270);
        const itemWidth = isRotated ? height : width;
        const itemHeight = isRotated ? width : height;

        // Ensure item stays within bounds
        newX = Math.max(0, Math.min(newX, maxWidth - itemWidth));
        newY = Math.max(0, Math.min(newY, maxHeight - itemHeight));

        // Return the calculated position with explicit conversions to ensure no floating point errors
        return {
            x: parseFloat(newX.toFixed(2)),
            y: parseFloat(newY.toFixed(2))
        };
    } catch (err) {
        console.error("Error in calculateMousePosition:", err);
        return { x: currentX, y: currentY };
    }
};

// Culture-aware mouse offset calculation
window.calculateMouseOffset = function (clientX, clientY, bounds, itemX, itemY) {
    try {
        // Ensure all input values are proper numbers
        clientX = window.parseInvariantNumber(clientX);
        clientY = window.parseInvariantNumber(clientY);
        bounds.left = window.parseInvariantNumber(bounds.left);
        bounds.top = window.parseInvariantNumber(bounds.top);
        itemX = window.parseInvariantNumber(itemX);
        itemY = window.parseInvariantNumber(itemY);

        // Calculate relative position in the floor plan
        const relativeX = clientX - bounds.left;
        const relativeY = clientY - bounds.top;

        // Calculate offset from the item's top-left corner
        const offsetX = relativeX - itemX;
        const offsetY = relativeY - itemY;

        // Return with explicit conversions to avoid floating point errors
        return {
            offsetX: parseFloat(offsetX.toFixed(2)),
            offsetY: parseFloat(offsetY.toFixed(2))
        };
    } catch (err) {
        console.error("Error in calculateMouseOffset:", err);
        return { offsetX: 0, offsetY: 0 };
    }
};

// Culture-aware rotation calculation
window.calculateRotation = function (x, y, width, height, currentRotation, rotationDegrees, maxWidth, maxHeight) {
    try {
        // Ensure all input values are proper numbers
        x = window.parseInvariantNumber(x);
        y = window.parseInvariantNumber(y);
        width = window.parseInvariantNumber(width);
        height = window.parseInvariantNumber(height);
        currentRotation = window.parseInvariantNumber(currentRotation);
        rotationDegrees = window.parseInvariantNumber(rotationDegrees);
        maxWidth = window.parseInvariantNumber(maxWidth);
        maxHeight = window.parseInvariantNumber(maxHeight);

        // Normalize current rotation to 0-360
        currentRotation = Math.round(currentRotation / 90) * 90 % 360;
        if (currentRotation < 0) currentRotation += 360;

        // Get current dimensions based on rotation
        const isCurrentlyRotated = (currentRotation === 90 || currentRotation === 270);
        const currentWidth = isCurrentlyRotated ? height : width;
        const currentHeight = isCurrentlyRotated ? width : height;

        // Calculate center point of current item
        const centerX = x + (currentWidth / 2);
        const centerY = y + (currentHeight / 2);

        // Calculate new rotation angle
        let newRotation = (currentRotation + rotationDegrees) % 360;
        if (newRotation < 0) newRotation += 360;

        // Get new dimensions based on new rotation
        const willBeRotated = (newRotation === 90 || newRotation === 270);
        const newWidth = willBeRotated ? height : width;
        const newHeight = willBeRotated ? width : height;

        // Calculate new top-left position to maintain center
        let newX = centerX - (newWidth / 2);
        let newY = centerY - (newHeight / 2);

        // Ensure item stays within bounds
        newX = Math.max(0, Math.min(newX, maxWidth - newWidth));
        newY = Math.max(0, Math.min(newY, maxHeight - newHeight));

        // Return with explicit conversions
        return {
            x: parseFloat(newX.toFixed(2)),
            y: parseFloat(newY.toFixed(2)),
            rotation: newRotation,
            centerX: parseFloat(centerX.toFixed(2)),
            centerY: parseFloat(centerY.toFixed(2))
        };
    } catch (err) {
        console.error("Error in calculateRotation:", err);
        return { x: x, y: y, rotation: currentRotation, centerX: x + width / 2, centerY: y + height / 2 };
    }
};

// Culture-aware group movement
window.moveGroupItems = function (items, primaryItemId, newX, newY, maxWidth, maxHeight) {
    try {
        // Find the primary item
        const primaryItem = items.find(item => item.id === primaryItemId);
        if (!primaryItem) {
            console.error("Primary item not found in group");
            return { positions: [] };
        }

        // Ensure all values are proper numbers
        newX = window.parseInvariantNumber(newX);
        newY = window.parseInvariantNumber(newY);
        maxWidth = window.parseInvariantNumber(maxWidth);
        maxHeight = window.parseInvariantNumber(maxHeight);

        // Calculate movement delta
        const deltaX = parseFloat((newX - primaryItem.x).toFixed(2));
        const deltaY = parseFloat((newY - primaryItem.y).toFixed(2));

        // Skip negligible movements
        if (Math.abs(deltaX) < 0.5 && Math.abs(deltaY) < 0.5) {
            return {
                positions: items.map(item => ({
                    id: item.id,
                    x: item.x,
                    y: item.y
                }))
            };
        }

        // Check boundary constraints for all items
        let minAllowedDeltaX = Number.NEGATIVE_INFINITY;
        let maxAllowedDeltaX = Number.POSITIVE_INFINITY;
        let minAllowedDeltaY = Number.NEGATIVE_INFINITY;
        let maxAllowedDeltaY = Number.POSITIVE_INFINITY;

        items.forEach(item => {
            // Get dimensions based on rotation
            const rotation = Math.round(item.rotation / 90) * 90 % 360;
            const isRotated = (rotation === 90 || rotation === 270);
            const width = isRotated ? item.length : item.width;
            const height = isRotated ? item.width : item.length;

            // Check boundary constraints
            if (item.x + deltaX < 0) {
                minAllowedDeltaX = Math.max(minAllowedDeltaX, -item.x);
            }

            if (item.x + width + deltaX > maxWidth) {
                maxAllowedDeltaX = Math.min(maxAllowedDeltaX, maxWidth - (item.x + width));
            }

            if (item.y + deltaY < 0) {
                minAllowedDeltaY = Math.max(minAllowedDeltaY, -item.y);
            }

            if (item.y + height + deltaY > maxHeight) {
                maxAllowedDeltaY = Math.min(maxAllowedDeltaY, maxHeight - (item.y + height));
            }
        });

        // Apply constraints
        const adjustedDeltaX = isFinite(minAllowedDeltaX) ?
            Math.max(minAllowedDeltaX, Math.min(deltaX, maxAllowedDeltaX)) : 0;

        const adjustedDeltaY = isFinite(minAllowedDeltaY) ?
            Math.max(minAllowedDeltaY, Math.min(deltaY, maxAllowedDeltaY)) : 0;

        // Calculate new positions for all items
        const positions = items.map(item => {
            return {
                id: item.id,
                x: parseFloat((item.x + adjustedDeltaX).toFixed(2)),
                y: parseFloat((item.y + adjustedDeltaY).toFixed(2))
            };
        });

        return { positions: positions };
    } catch (err) {
        console.error("Error in moveGroupItems:", err);
        return { positions: [] };
    }
};

// Culture-aware group rotation
window.rotateGroupItems = function (items, rotationDegrees, pivotX, pivotY, maxWidth, maxHeight) {
    try {
        // Ensure all values are proper numbers
        rotationDegrees = window.parseInvariantNumber(rotationDegrees);
        pivotX = window.parseInvariantNumber(pivotX);
        pivotY = window.parseInvariantNumber(pivotY);
        maxWidth = window.parseInvariantNumber(maxWidth);
        maxHeight = window.parseInvariantNumber(maxHeight);

        // Convert rotation to radians
        const rotationRadians = rotationDegrees * (Math.PI / 180);

        // Calculate new positions for all items
        const positions = items.map(item => {
            // Ensure item values are proper numbers
            const x = window.parseInvariantNumber(item.x);
            const y = window.parseInvariantNumber(item.y);
            const rotation = window.parseInvariantNumber(item.rotation);
            const width = window.parseInvariantNumber(item.width);
            const height = window.parseInvariantNumber(item.height || item.length);

            // Get current center of the item
            const currentRotation = Math.round(rotation / 90) * 90 % 360;
            const isRotated = (currentRotation === 90 || currentRotation === 270);
            const itemWidth = isRotated ? height : width;
            const itemHeight = isRotated ? width : height;

            const itemCenterX = x + (itemWidth / 2);
            const itemCenterY = y + (itemHeight / 2);

            // Calculate vector from pivot to item center
            const vectorX = itemCenterX - pivotX;
            const vectorY = itemCenterY - pivotY;

            // Rotate the vector
            const newVectorX = vectorX * Math.cos(rotationRadians) - vectorY * Math.sin(rotationRadians);
            const newVectorY = vectorX * Math.sin(rotationRadians) + vectorY * Math.cos(rotationRadians);

            // Calculate new center position
            const newCenterX = pivotX + newVectorX;
            const newCenterY = pivotY + newVectorY;

            // Calculate new rotation
            let newRotation = (rotation + rotationDegrees) % 360;
            if (newRotation < 0) newRotation += 360;
            newRotation = Math.round(newRotation / 90) * 90;

            // Get new dimensions based on new rotation
            const willBeRotated = (newRotation === 90 || newRotation === 270);
            const newWidth = willBeRotated ? height : width;
            const newHeight = willBeRotated ? width : height;

            // Calculate new top-left position
            let newX = newCenterX - (newWidth / 2);
            let newY = newCenterY - (newHeight / 2);

            // Ensure item stays within bounds
            newX = Math.max(0, Math.min(newX, maxWidth - newWidth));
            newY = Math.max(0, Math.min(newY, maxHeight - newHeight));

            return {
                id: item.id,
                x: parseFloat(newX.toFixed(2)),
                y: parseFloat(newY.toFixed(2)),
                rotation: newRotation
            };
        });

        return { positions: positions };
    } catch (err) {
        console.error("Error in rotateGroupItems:", err);
        return { positions: [] };
    }
};

// Improve the existing parseInvariantNumber function
window.parseInvariantNumber = function (value) {
    if (typeof value === 'number') return value;
    if (!value && value !== 0) return 0;

    try {
        // Convert to string if not already
        let strValue = String(value).trim();

        // Always treat period as decimal separator for calculations
        // but make sure we handle comma correctly too
        if (strValue.indexOf(',') !== -1 && strValue.indexOf('.') === -1) {
            strValue = strValue.replace(',', '.');
        }

        // Parse using standard JavaScript parser
        const result = parseFloat(strValue);
        return isNaN(result) ? 0 : result;
    } catch (err) {
        console.error("Error in parseInvariantNumber:", err);
        return 0;
    }
};

// Enhanced formatInvariantNumber with better culture handling
window.formatInvariantNumber = function (value, decimals = 2) {
    if (value === null || value === undefined) return '0';

    try {
        // Parse the value first to ensure it's a number
        const num = window.parseInvariantNumber(value);
        if (isNaN(num)) return '0';

        // Format with fixed decimal places
        let formatted = num.toFixed(decimals);

        // Apply cultural decimal separator if needed
        if (window.appCulture.useCommaDecimalSeparator) {
            formatted = formatted.replace('.', ',');
        }

        return formatted;
    } catch (err) {
        console.error("Error in formatInvariantNumber:", err);
        return '0';
    }
};

// Setup planner handlers with culture awareness
window.setupPlannerHandlers = function (dotNetRef) {
    try {
        console.log("Setting up planner handlers with culture awareness");

        // Ensure culture is initialized, default to US if not
        if (!window.appCulture.isInitialized) {
            window.initializeCulture('en-US');
        }

        console.log("Using decimal separator: " + window.appCulture.decimalSeparator);

        // Store reference globally
        window.plannerState = {
            dotNetRef: dotNetRef,
            initialized: false,
            isDragging: false,
            culture: window.appCulture.current
        };

        // Setup document handlers
        setupDocumentMouseHandlers(dotNetRef);
        setupDocumentTouchHandlers(dotNetRef);

        window.plannerState.initialized = true;
        console.log("Planner handlers setup complete");
        return true;
    } catch (err) {
        console.error("Error in setupPlannerHandlers:", err);
        return false;
    }
};

window.touchDragDelay = function (callback, delay = 150) {
    window.touchDragTimer = setTimeout(callback, delay);
};

window.cancelTouchDragDelay = function () {
    if (window.touchDragTimer) {
        clearTimeout(window.touchDragTimer);
        window.touchDragTimer = null;
    }
};

window.isButtonClick = function (event) {
    // Check if the clicked element is a button or inside a button
    let target = event.target;
    while (target != null) {
        if (target.tagName === 'BUTTON' ||
            target.classList.contains('mud-button') ||
            target.classList.contains('mud-icon-button')) {
            return true;
        }
        target = target.parentElement;
    }
    return false;
}

window.isButtonTouch = function (event) {
    if (!event || !event.touches || event.touches.length === 0) {
        console.log("Invalid touch event in isButtonTouch");
        return false;
    }

    try {
        var touch = event.touches[0];

        // Get the element that was touched using elementFromPoint
        var element = document.elementFromPoint(touch.clientX, touch.clientY);
        if (!element) {
            console.log("No element found at touch point");
            return false;
        }

        console.log("Touch target:", element.tagName, element.className);

        // Check if the element or any parent is a button
        while (element) {
            if (element.tagName === 'BUTTON' ||
                element.classList.contains('mud-button') ||
                element.classList.contains('mud-icon-button')) {
                console.log("Button touch detected!");
                return true;
            }
            element = element.parentElement;
        }

        return false;
    } catch (err) {
        console.error("Error in isButtonTouch:", err);
        return false;
    }
};

//disables gestures for ipads
window.setupGestureHandlers = function () {
    document.addEventListener('gesturestart', function (e) {
        e.preventDefault(); // Prevent pinch zoom, etc.
    });

    document.addEventListener('gesturechange', function (e) {
        e.preventDefault();
    });

    document.addEventListener('gestureend', function (e) {
        e.preventDefault();
    });
};

// Check if a touch event happened on a button
window.isButtonTouch = function (event) {
    if (!event || !event.touches || event.touches.length === 0) return false;

    var touch = event.touches[0];

    // Get the element at the touch position
    var element = document.elementFromPoint(touch.clientX, touch.clientY);

    // Check if it's a button or inside a button
    while (element != null) {
        if (element.tagName === 'BUTTON' ||
            element.classList.contains('mud-button') ||
            element.classList.contains('mud-icon-button')) {
            return true;
        }
        element = element.parentElement;
    }
    return false;
};

window.debounce = function (func, wait) {
    let timeout;
    return function (...args) {
        const context = this;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), wait);
    };
};

window.calculateTouchPreciseOffset = function (clientX, clientY, bounds, itemX, itemY) {
    try {
        // Parse all inputs to ensure they're proper numbers
        clientX = window.parseInvariantNumber(clientX);
        clientY = window.parseInvariantNumber(clientY);
        bounds.left = window.parseInvariantNumber(bounds.left);
        bounds.top = window.parseInvariantNumber(bounds.top);
        itemX = window.parseInvariantNumber(itemX);
        itemY = window.parseInvariantNumber(itemY);

        // For iPad detection
        const isIPad = /iPad|iPhone|iPod/.test(navigator.userAgent) ||
            (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);

        // Calculate relative position in the floor plan
        let relativeX = clientX - bounds.left;
        let relativeY = clientY - bounds.top;

        // Apply iPad-specific adjustments if needed
        if (isIPad) {
            // Reduce sensitivity slightly for iPads to prevent jumpy movement
            // These values may need tuning based on testing
            relativeX = Math.round(relativeX * 10) / 10; // Round to nearest 0.1px
            relativeY = Math.round(relativeY * 10) / 10;

            console.log("iPad touch precision adjustment applied");
        }

        // Calculate offset from the item's top-left corner
        const offsetX = parseFloat((relativeX - itemX).toFixed(1));
        const offsetY = parseFloat((relativeY - itemY).toFixed(1));

        console.log(`Touch offset calculated: (${offsetX}, ${offsetY}) for item at (${itemX}, ${itemY})`);

        return {
            offsetX: offsetX,
            offsetY: offsetY,
            deviceType: isIPad ? "iPad" : "other"
        };
    } catch (err) {
        console.error("Error in calculateTouchPreciseOffset:", err);
        return { offsetX: 0, offsetY: 0, deviceType: "unknown" };
    }
};

// Culture-aware touch offset calculation
window.calculateTouchOffset = function (clientX, clientY, bounds, itemX, itemY) {
    try {
        // Ensure all input values are proper numbers
        clientX = window.parseInvariantNumber(clientX);
        clientY = window.parseInvariantNumber(clientY);
        bounds.left = window.parseInvariantNumber(bounds.left);
        bounds.top = window.parseInvariantNumber(bounds.top);
        itemX = window.parseInvariantNumber(itemX);
        itemY = window.parseInvariantNumber(itemY);

        // Calculate relative position in the floor plan
        const relativeX = clientX - bounds.left;
        const relativeY = clientY - bounds.top;

        // iPad-specific adjustments 
        if (window.isIPad()) {
            // On iPad, we've found touch coordinates sometimes need adjustment
            // Test with small offsets to see if this helps with the teleporting issue
            relativeX = relativeX * 0.98; // Slight scale adjustment
            relativeY = relativeY * 0.98;

            console.log("iPad touch adjustment applied");
        }

        // Calculate offset from the item's top-left corner
        const offsetX = relativeX - itemX;
        const offsetY = relativeY - itemY;

        // Return with explicit conversions to avoid floating point errors
        return {
            offsetX: parseFloat(offsetX.toFixed(2)),
            offsetY: parseFloat(offsetY.toFixed(2))
        };
    } catch (err) {
        console.error("Error in calculateTouchOffset:", err);
        return { offsetX: 0, offsetY: 0 };
    }
};

window.preventTouchScroll = function (event) {
    if (event) {
        // More aggressive prevention
        if (event.preventDefault) {
            event.preventDefault();
        }

        // Optional: Throttle touch events for smoother movement
        if (window.throttleTouchEvents && event.type === 'touchmove') {
            const now = Date.now();
            if (now - window.lastTouchMoveTime < 16) { // ~60fps
                // Skip this event to avoid processing too many
                if (event.stopPropagation) event.stopPropagation();
                return false;
            }
            window.lastTouchMoveTime = now;
        }

        // Prevent all default behaviors
        return false;
    }
};

window.isIPad = function () {
    return /iPad|iPhone|iPod/.test(navigator.userAgent) ||
        (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
};

// Utility functions for the planner component
// Calculate the correct position for a rotated item to maintain its center
function calculateRotatedPosition(x, y, width, height, currentRotation, newRotation) {
    // Ensure all inputs are proper numbers by parsing them in a culture-invariant way
    x = window.parseInvariantNumber(x);
    y = window.parseInvariantNumber(y);
    width = window.parseInvariantNumber(width);
    height = window.parseInvariantNumber(height);
    currentRotation = window.parseInvariantNumber(currentRotation);
    newRotation = window.parseInvariantNumber(newRotation);

    // Normalize rotations to 0, 90, 180, 270
    currentRotation = Math.round(currentRotation / 90) * 90 % 360;
    if (currentRotation < 0) currentRotation += 360;

    newRotation = Math.round(newRotation / 90) * 90 % 360;
    if (newRotation < 0) newRotation += 360;

    // Get current effective dimensions
    const isCurrentlyRotated = (currentRotation == 90 || currentRotation == 270);
    const currentWidth = isCurrentlyRotated ? height : width;
    const currentHeight = isCurrentlyRotated ? width : height;

    // Calculate center point of the current item
    const centerX = x + (currentWidth / 2);
    const centerY = y + (currentHeight / 2);

    // Get new effective dimensions
    const willBeRotated = (newRotation == 90 || newRotation == 270);
    const newWidth = willBeRotated ? height : width;
    const newHeight = willBeRotated ? width : height;

    // Calculate new top-left position while maintaining center
    const newX = centerX - (newWidth / 2);
    const newY = centerY - (newHeight / 2);

    return {
        x: newX,
        y: newY,
        width: newWidth,
        height: newHeight,
        centerX: centerX,
        centerY: centerY
    };
}

// Calculate the bounding box of a rotated item
function getRotatedItemBounds(x, y, width, height, rotationDegrees) {
    // Convert to radians
    const rotationRadians = rotationDegrees * Math.PI / 180;

    // Calculate center point
    const centerX = x + width / 2;
    const centerY = y + height / 2;

    // Calculate corners relative to center
    const halfWidth = width / 2;
    const halfHeight = height / 2;

    // Calculate all four corners
    const corners = [
        { x: -halfWidth, y: -halfHeight }, // Top-left
        { x: halfWidth, y: -halfHeight },  // Top-right
        { x: halfWidth, y: halfHeight },   // Bottom-right
        { x: -halfWidth, y: halfHeight }   // Bottom-left
    ];

    // Rotate and translate each corner
    const rotatedCorners = corners.map(corner => {
        const rotatedX = corner.x * Math.cos(rotationRadians) - corner.y * Math.sin(rotationRadians);
        const rotatedY = corner.x * Math.sin(rotationRadians) + corner.y * Math.cos(rotationRadians);

        return {
            x: centerX + rotatedX,
            y: centerY + rotatedY
        };
    });

    // Find min/max X and Y to determine bounding box
    const xValues = rotatedCorners.map(corner => corner.x);
    const yValues = rotatedCorners.map(corner => corner.y);

    const minX = Math.min(...xValues);
    const maxX = Math.max(...xValues);
    const minY = Math.min(...yValues);
    const maxY = Math.max(...yValues);

    return {
        x: minX,
        y: minY,
        width: maxX - minX,
        height: maxY - minY,
        corners: rotatedCorners
    };
}

// Rotate a point around a pivot
function rotatePointAroundPivot(pointX, pointY, pivotX, pivotY, angleDegrees) {
    // Convert to radians
    const angleRadians = angleDegrees * Math.PI / 180;

    // Translate point to origin
    const translatedX = pointX - pivotX;
    const translatedY = pointY - pivotY;

    // Rotate
    const rotatedX = translatedX * Math.cos(angleRadians) - translatedY * Math.sin(angleRadians);
    const rotatedY = translatedX * Math.sin(angleRadians) + translatedY * Math.cos(angleRadians);

    // Translate back
    return {
        x: pivotX + rotatedX,
        y: pivotY + rotatedY
    };
}

// Calculate snap points for a furniture item
function calculateSnapPoints(item) {
    // Get effective dimensions based on rotation
    const isRotated = (Math.round(item.rotation / 90) % 2) === 1;
    const width = isRotated ? item.articleLength : item.articleWidth;
    const height = isRotated ? item.articleWidth : item.articleLength;

    // Calculate center for reference
    const centerX = item.x + width / 2;
    const centerY = item.y + height / 2;

    // Define snap points - use 9-point system (4 corners, 4 midpoints, 1 center)
    return {
        // Corners
        topLeft: { x: item.x, y: item.y, type: 'corner' },
        topRight: { x: item.x + width, y: item.y, type: 'corner' },
        bottomLeft: { x: item.x, y: item.y + height, type: 'corner' },
        bottomRight: { x: item.x + width, y: item.y + height, type: 'corner' },

        // Edge midpoints
        topCenter: { x: centerX, y: item.y, type: 'edge', orientation: 'horizontal' },
        rightCenter: { x: item.x + width, y: centerY, type: 'edge', orientation: 'vertical' },
        bottomCenter: { x: centerX, y: item.y + height, type: 'edge', orientation: 'horizontal' },
        leftCenter: { x: item.x, y: centerY, type: 'edge', orientation: 'vertical' },

        // Center
        center: { x: centerX, y: centerY, type: 'center' },

        // Edges as line segments
        edges: {
            top: { x1: item.x, y1: item.y, x2: item.x + width, y2: item.y, type: 'horizontal' },
            right: { x1: item.x + width, y1: item.y, x2: item.x + width, y2: item.y + height, type: 'vertical' },
            bottom: { x1: item.x, y1: item.y + height, x2: item.x + width, y2: item.y + height, type: 'horizontal' },
            left: { x1: item.x, y1: item.y, x2: item.x, y2: item.y + height, type: 'vertical' }
        }
    };
}

// Check if two furniture items would overlap
function doItemsOverlap(item1, item2, tolerance = 0) {
    // Get effective dimensions based on rotation
    const isRotated1 = (Math.round(item1.rotation / 90) % 2) === 1;
    const width1 = isRotated1 ? item1.articleLength : item1.articleWidth;
    const height1 = isRotated1 ? item1.articleWidth : item1.articleLength;

    const isRotated2 = (Math.round(item2.rotation / 90) % 2) === 1;
    const width2 = isRotated2 ? item2.articleLength : item2.articleWidth;
    const height2 = isRotated2 ? item2.articleWidth : item2.articleLength;

    // Calculate bounding boxes
    const left1 = item1.x;
    const top1 = item1.y;
    const right1 = left1 + width1;
    const bottom1 = top1 + height1;

    const left2 = item2.x;
    const top2 = item2.y;
    const right2 = left2 + width2;
    const bottom2 = top2 + height2;

    // AABB overlap test with tolerance
    return !(right1 <= left2 + tolerance ||
        right2 <= left1 + tolerance ||
        bottom1 <= top2 + tolerance ||
        bottom2 <= top1 + tolerance);
}

// Find best snap position for two items
function findBestSnapPosition(movingItem, targetItem, snapDistance = 10, alignmentMode = 'end') {
    // Get effective dimensions based on rotation for both items
    const isRotatedMoving = (Math.round(movingItem.rotation / 90) % 2) === 1;
    const widthMoving = isRotatedMoving ? movingItem.articleLength : movingItem.articleWidth;
    const heightMoving = isRotatedMoving ? movingItem.articleWidth : movingItem.articleLength;

    const isRotatedTarget = (Math.round(targetItem.rotation / 90) % 2) === 1;
    const widthTarget = isRotatedTarget ? targetItem.articleLength : targetItem.articleWidth;
    const heightTarget = isRotatedTarget ? targetItem.articleWidth : targetItem.articleLength;

    // For better position calculation, get bounding boxes
    const movingBox = {
        left: movingItem.x,
        top: movingItem.y,
        right: movingItem.x + widthMoving,
        bottom: movingItem.y + heightMoving,
        width: widthMoving,
        height: heightMoving,
        centerX: movingItem.x + widthMoving / 2,
        centerY: movingItem.y + heightMoving / 2
    };

    const targetBox = {
        left: targetItem.x,
        top: targetItem.y,
        right: targetItem.x + widthTarget,
        bottom: targetItem.y + heightTarget,
        width: widthTarget,
        height: heightTarget,
        centerX: targetItem.x + widthTarget / 2,
        centerY: targetItem.y + heightTarget / 2
    };

    // Check each possible snap configuration
    let bestSnap = {
        distance: Number.MAX_VALUE,
        x: movingItem.x,
        y: movingItem.y,
        edge: null
    };

    // Check right-to-left snap
    const rightToLeftDist = Math.abs(movingBox.right - targetBox.left);
    if (rightToLeftDist <= snapDistance) {
        // Calculate vertical alignment based on alignment mode
        let alignedY = movingItem.y; // Default = no change

        if (alignmentMode === 'start') {
            alignedY = targetBox.top;
        } else if (alignmentMode === 'center') {
            alignedY = targetBox.centerY - heightMoving / 2;
        } else if (alignmentMode === 'end') {
            alignedY = targetBox.bottom - heightMoving;
        }

        const newDist = rightToLeftDist;
        if (newDist < bestSnap.distance) {
            bestSnap = {
                distance: newDist,
                x: targetBox.left - widthMoving,
                y: alignedY,
                edge: 'right'
            };
        }
    }

    // Check left-to-right snap
    const leftToRightDist = Math.abs(movingBox.left - targetBox.right);
    if (leftToRightDist <= snapDistance) {
        // Calculate vertical alignment
        let alignedY = movingItem.y; // Default = no change

        if (alignmentMode === 'start') {
            alignedY = targetBox.top;
        } else if (alignmentMode === 'center') {
            alignedY = targetBox.centerY - heightMoving / 2;
        } else if (alignmentMode === 'end') {
            alignedY = targetBox.bottom - heightMoving;
        }

        const newDist = leftToRightDist;
        if (newDist < bestSnap.distance) {
            bestSnap = {
                distance: newDist,
                x: targetBox.right,
                y: alignedY,
                edge: 'left'
            };
        }
    }

    // Check bottom-to-top snap
    const bottomToTopDist = Math.abs(movingBox.bottom - targetBox.top);
    if (bottomToTopDist <= snapDistance) {
        // Calculate horizontal alignment
        let alignedX = movingItem.x; // Default = no change

        if (alignmentMode === 'start') {
            alignedX = targetBox.left;
        } else if (alignmentMode === 'center') {
            alignedX = targetBox.centerX - widthMoving / 2;
        } else if (alignmentMode === 'end') {
            alignedX = targetBox.right - widthMoving;
        }

        const newDist = bottomToTopDist;
        if (newDist < bestSnap.distance) {
            bestSnap = {
                distance: newDist,
                x: alignedX,
                y: targetBox.top - heightMoving,
                edge: 'bottom'
            };
        }
    }

    // Check top-to-bottom snap
    const topToBottomDist = Math.abs(movingBox.top - targetBox.bottom);
    if (topToBottomDist <= snapDistance) {
        // Calculate horizontal alignment
        let alignedX = movingItem.x; // Default = no change

        if (alignmentMode === 'start') {
            alignedX = targetBox.left;
        } else if (alignmentMode === 'center') {
            alignedX = targetBox.centerX - widthMoving / 2;
        } else if (alignmentMode === 'end') {
            alignedX = targetBox.right - widthMoving;
        }

        const newDist = topToBottomDist;
        if (newDist < bestSnap.distance) {
            bestSnap = {
                distance: newDist,
                x: alignedX,
                y: targetBox.bottom,
                edge: 'top'
            };
        }
    }

    return bestSnap;
}

function setupDocumentMouseHandlers(dotNetRef) {
    console.log("Setting up document mouse handlers");

    // Remove any existing event listeners to prevent duplicates
    document.removeEventListener('mouseup', documentMouseUpHandler);
    document.body.removeEventListener('mouseleave', documentMouseLeaveHandler);

    // Store the dotNetRef globally so we can access it in our handlers
    window.plannerDotNetRef = dotNetRef;

    // Add the handlers
    document.addEventListener('mouseup', documentMouseUpHandler);
    document.body.addEventListener('mouseleave', documentMouseLeaveHandler);

    console.log("Document mouse handlers setup complete");
}

function setupDocumentTouchHandlers(dotNetRef) {
    console.log("Setting up document touch handlers");

    // Remove any existing event listeners to prevent duplicates
    document.removeEventListener('touchend', documentTouchEndHandler);
    document.removeEventListener('touchcancel', documentTouchCancelHandler);

    // Store the dotNetRef globally so we can access it in our handlers
    window.plannerDotNetRef = dotNetRef;

    // Add the handlers
    document.addEventListener('touchend', documentTouchEndHandler);
    document.addEventListener('touchcancel', documentTouchCancelHandler);

    console.log("Document touch handlers setup complete");
}

function documentTouchEndHandler(e) {
    console.log("Document touchend triggered");
    if (window.plannerDotNetRef) {
        window.plannerDotNetRef.invokeMethodAsync('HandleDocumentTouchEnd')
            .catch(err => console.error("Error calling HandleDocumentTouchEnd:", err));
    }
}

function documentTouchCancelHandler(e) {
    console.log("Document touchcancel triggered");
    if (window.plannerDotNetRef) {
        window.plannerDotNetRef.invokeMethodAsync('HandleDocumentTouchEnd')
            .catch(err => console.error("Error calling HandleDocumentTouchEnd:", err));
    }
}

//what was this for again?
function updateRotatedItemPosition(element, x, y, rotation) {
    if (!element) {
        console.warn("Element not found for position update");
        return;
    }

    // Set the position with transform to maintain rotation
    element.style.transform = `translate(${x}px, ${y}px) rotate(${rotation}deg)`;
}

//euh...
function initializeItemDragAndDrop(dotNetRef, itemId) {
    // Find the element by data attribute
    const element = document.querySelector(`[data-item-id="${itemId}"]`);
    if (!element) {
        console.warn(`Element with data-item-id="${itemId}" not found`);
        return;
    }

    // Clear any existing event listeners to prevent duplicates
    const clone = element.cloneNode(true);
    element.parentNode.replaceChild(clone, element);
    const newElement = clone;

    // Variables for drag state
    let isDragging = false;
    let offsetX, offsetY;
    let currentRotation = 0;

    // Handle mouse down to start dragging
    newElement.addEventListener('mousedown', function (e) {
        // Ignore if clicking on a button or control
        if (e.target.closest('button') || e.target.closest('.mud-icon-button')) {
            return;
        }

        isDragging = true;

        // Get current rotation from the element
        const transform = window.getComputedStyle(newElement).getPropertyValue('transform');
        const matrix = new DOMMatrix(transform);
        currentRotation = Math.round(Math.atan2(matrix.b, matrix.a) * (180 / Math.PI));

        // Calculate offset within element considering rotation
        const rect = newElement.getBoundingClientRect();
        const centerX = rect.left + rect.width / 2;
        const centerY = rect.top + rect.height / 2;

        // Calculate offset from center
        offsetX = e.clientX - centerX;
        offsetY = e.clientY - centerY;

        // Tell .NET that dragging started
        dotNetRef.invokeMethodAsync('OnDragStart', itemId);

        // Add dragging class
        newElement.classList.add('dragging');
        newElement.style.zIndex = '1000';

        e.preventDefault();
    });

    // Use document for mouse move/up for better drag handling
    document.addEventListener('mousemove', function (e) {
        if (!isDragging) return;

        // Calculate position in parent container
        const parentRect = newElement.parentElement.getBoundingClientRect();

        // Calculate center-based position
        const x = e.clientX - parentRect.left - offsetX;
        const y = e.clientY - parentRect.top - offsetY;

        // Update visual position with rotation preserved
        updateRotatedItemPosition(newElement, x, y, currentRotation);

        // Throttle updates to server for performance
        if (window.dragUpdateTimer) clearTimeout(window.dragUpdateTimer);
        window.dragUpdateTimer = setTimeout(() => {
            dotNetRef.invokeMethodAsync('OnDragUpdate', itemId, x, y);
        }, 10);

        e.preventDefault();
    });

    document.addEventListener('mouseup', function (e) {
        if (!isDragging) return;

        isDragging = false;

        // Remove dragging class
        newElement.classList.remove('dragging');
        newElement.style.zIndex = '';

        // Final position update
        dotNetRef.invokeMethodAsync('OnDragEnd', itemId);
    });
}

function mapBoxInit(destination) {
    mapboxgl.accessToken = "pk.eyJ1Ijoic2lsZW50c3Rvcm0iLCJhIjoiY2sxdGJkOWMwMG1icDNtbTVkeTl6aWx0bCJ9.av7ZKsUP6wXcON6yLBlMSg";
    mapboxgl.essential = true;

    map = new mapboxgl.Map({
        container: "map",
        style: "mapbox://styles/mapbox/streets-v12",
    });

    var directions = new MapboxDirections({
        accessToken: mapboxgl.accessToken,
        unit: "metric",
        profile: "mapbox/driving-traffic",
        controls: { profileSwitcher: false, instructions: true },
        alternatives: false,
        congestion: true
    });

    var geolocate = new mapboxgl.GeolocateControl({
        positionOptions: {
            enableHighAccuracy: true
        },
        trackUserLocation: false
    });

    var nav = new mapboxgl.NavigationControl();

    map.on("load", function () {
        console.log("A map load event occurred. " + destination);
        map.addControl(directions, "top-left");
        map.addControl(geolocate, "top-right");
        map.addControl(new mapboxgl.FullscreenControl({ container: document.querySelector("map") }), "top-right");
        map.addControl(nav, "top-right");
        directions.setOrigin("Vilvertstraat 11, 3650 Dilsen-Stokkem");
        directions.setDestination(destination);
    });

    geolocate.on("geolocate", function (e) {
        var lon = e.coords.longitude;
        var lat = e.coords.latitude
        var position = [lon, lat];
        directions.setOrigin(position);
        directions.setDestination(destination);
    });
}

function mapBoxClear() {
    if (map != null) {
        map.remove();
    }
}

function mapsSelector(address) {
    if ((navigator.platform.indexOf("iPhone") != -1) || (navigator.platform.indexOf("iPad") != -1) || (navigator.platform.indexOf("iPod") != -1) || (navigator.platform.indexOf("Mac") != -1)) {
        window.open("maps://maps.google.com/maps?f=d&daddr=".concat(address));
    }
    else {
        window.open("https://maps.google.com/maps?f=d&daddr=".concat(address));
    }
}

function getTimezoneOffset() {
    return new Date().getTimezoneOffset();
}

function writeCookie(name, value, days) {
    var expires;
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toGMTString();
    }
    else {
        expires = "";
    }
    document.cookie = name + "=" + value + expires + "; path=/";
}

function getCookie(cname) {
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var arr = ca[i].split('=');
        if (arr[0] == cname)
            return arr[1]
    }
    return "";
}

function submitForm(form) {
    form.submit();
}

function preventTouchScroll(event) {
    if (event && event.preventDefault) {
        event.preventDefault();
    }
    //event.preventDefault();
    //event.stopPropagation();
}
