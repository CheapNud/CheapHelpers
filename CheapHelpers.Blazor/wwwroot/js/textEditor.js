window.richTextEditorFunctions = {
    // Create and initialize the editor
    createEditor: function (content) {
        try {
            // Clear any existing editor
            const container = document.getElementById('editorContainer');
            if (!container) {
                console.error('Editor container not found');
                return false;
            }

            // Clear container
            container.innerHTML = '';

            // Create the editor element
            const editor = document.createElement('div');
            editor.id = 'richTextEditor';
            editor.contentEditable = 'true';
            editor.style.width = '100%';
            editor.style.minHeight = '400px';
            editor.style.padding = '10px';
            editor.style.overflowY = 'auto';
            editor.style.outline = 'none';
            editor.innerHTML = content || '';

            // Add input event listener to sync content back to .NET
            editor.addEventListener('input', function () {
                // We'll poll the content every 300ms during input to prevent too many calls
                if (!editor._inputTimeout) {
                    editor._inputTimeout = setTimeout(function () {
                        editor._inputTimeout = null;
                        DotNet.invokeMethodAsync('CheapHelpers.Blazor', 'UpdateContent', editor.innerHTML);
                    }, 300);
                }
            });

            // Add the editor to the container
            container.appendChild(editor);

            // Force a focus and blur to make sure the editor is properly initialized
            editor.focus();
            setTimeout(() => {
                // Just to ensure everything is rendered
                console.log('Editor initialized with content length: ' + (content ? content.length : 0));
            }, 0);

            return true;
        } catch (e) {
            console.error('Error creating editor:', e);
            return false;
        }
    },

    execCommand: function (command, value) {
        try {
            document.execCommand(command, false, value || null);

            // Manually trigger content update
            const editor = document.getElementById('richTextEditor');
            if (editor) {
                DotNet.invokeMethodAsync('CheapHelpers.Blazor', 'UpdateContent', editor.innerHTML);
            }
            return true;
        } catch (e) {
            console.error('Error executing command:', e);
            return false;
        }
    },

    getContent: function () {
        const editor = document.getElementById('richTextEditor');
        return editor ? editor.innerHTML : '';
    },

    setContent: function (content) {
        const editor = document.getElementById('richTextEditor');
        if (editor) {
            try {
                editor.innerHTML = content;
                return true;
            } catch (e) {
                console.error('Error setting content:', e);
                return false;
            }
        }
        return false;
    }
};
