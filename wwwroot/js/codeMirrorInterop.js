window.codeMirrorInterop = {
    editors: {},
    dotNetRefs: {},

    initialize: function (dotNetRef, containerId, element, options, initialContent) {
        // Make sure CodeMirror is available
        if (!window.CodeMirror) {
            console.error("CodeMirror is not loaded");
            return;
        }

        // Store the .NET reference
        this.dotNetRefs[containerId] = dotNetRef;

        // Create a custom variable mode
        if (!CodeMirror.modes.variable) {
            CodeMirror.defineMode("variable", function () {
                return {
                    token: function (stream) {
                        if (stream.match("{{")) {
                            while (!stream.match("}}", false) && !stream.eol()) {
                                stream.next();
                            }
                            return "variable";
                        }
                        if (stream.match("}}")) {
                            return "variable";
                        }
                        while (stream.next() != null && !stream.match("{{", false)) { }
                        return null;
                    }
                };
            });
        }

        // Configure editor options
        const editorOptions = {
            value: initialContent || "",
            lineNumbers: options.lineNumbers,
            lineWrapping: options.lineWrapping,
            mode: options.mode || "variable",
            placeholder: options.placeholder,
            viewportMargin: Infinity,
            theme: "default",
            extraKeys: {}
        };

        // For single-line mode, prevent Enter key from adding new lines
        if (options.singleLine) {
            editorOptions.extraKeys["Enter"] = function () { return false; };
        }

        // Create editor
        const editor = CodeMirror(element, editorOptions);

        // Set height for the editor
        if (options.minHeight) {
            editor.getScrollerElement().style.minHeight = options.minHeight + "px";
        }
        
        // Handle single-line mode
        if (options.singleLine) {
            // Adjust the editor's height to fit a single line
            editor.getScrollerElement().style.maxHeight = (options.maxHeight || 28) + "px";
            editor.getWrapperElement().style.height = "auto";
            
            // Hide scrollbars in single-line mode
            editor.getScrollerElement().style.overflowY = "hidden";
            
            // Prevent newlines
            editor.on("beforeChange", function(cm, change) {
                if (change.origin === "+input" && change.text.length > 1) {
                    change.cancel();
                } else if (change.origin === "+input" && /\n/.test(change.text[0])) {
                    change.cancel();
                }
            });
        } else {
            // For multiline mode, dynamically adjust height based on requestSection
            this.adjustEditorHeight(editor, containerId);
            
            // Add resize event listener to adjust height when window resizes
            window.addEventListener('resize', () => {
                this.adjustEditorHeight(editor, containerId);
            });
            
            // Also trigger adjustment when the request-response resize handle is used
            const resizer = document.querySelector('.request-response-resizer');
            if (resizer) {
                const observer = new MutationObserver(() => {
                    this.adjustEditorHeight(editor, containerId);
                });
                observer.observe(resizer, { attributes: true });
            }
        }

        // Store editor instance and variables
        this.editors[containerId] = {
            editor: editor,
            variables: options.variables || {}
        };

        // Set up change event
        editor.on("change", (cm) => {
            const value = cm.getValue();
            this.dotNetRefs[containerId].invokeMethodAsync('OnEditorChanged', value);
        });

        // Set up variable highlighting and tooltips
        this.setupVariableHighlighting(containerId);
        
        // Set up autocomplete
        this.setupAutocomplete(containerId);
    },

    adjustEditorHeight: function(editor, containerId) {
        if (!editor) return;
        
        const requestSection = document.querySelector('.request-section');
        const isMultiline = editor.getWrapperElement().closest('.variable-input-container.multiline');
        
        if (requestSection && isMultiline) {
            // Get available height in request section
            const requestSectionHeight = requestSection.offsetHeight;
            
            // Calculate a reasonable max height based on request section, but ensuring some padding
            const otherElementsHeight = 120; // Approximate height of other elements in the section
            const maxHeight = Math.max(150, requestSectionHeight - otherElementsHeight);
            
            // Set the max height on the scroller
            editor.getScrollerElement().style.maxHeight = maxHeight + 'px';
            
            // Refresh editor to update its internal size measurements
            editor.refresh();
        }
    },

    setContent: function (containerId, content) {
        const editorInstance = this.editors[containerId];
        if (editorInstance && editorInstance.editor) {
            // Only update if content is different to avoid recursive updates
            if (editorInstance.editor.getValue() !== content) {
                editorInstance.editor.setValue(content);
            }
        }
    },

    dispose: function (containerId) {
        if (this.editors[containerId]) {
            // Remove event listeners
            const editor = this.editors[containerId].editor;
            if (editor) {
                editor.off("change");
                
                // Remove resize event listener if we added one
                window.removeEventListener('resize', () => {
                    this.adjustEditorHeight(editor, containerId);
                });
                
                editor.toTextArea(); // Convert back to textarea if needed
            }
            
            // Clean up
            delete this.editors[containerId];
            delete this.dotNetRefs[containerId];
        }
    },

    setupVariableHighlighting: function (containerId) {
        const editorInstance = this.editors[containerId];
        if (!editorInstance || !editorInstance.editor) return;
        
        const editor = editorInstance.editor;
        const dotNetRef = this.dotNetRefs[containerId];

        // Create tooltip element
        const tooltip = document.createElement("div");
        tooltip.className = "cm-variable-tooltip";
        tooltip.style.position = "absolute";
        tooltip.style.backgroundColor = "#f9f9f9";
        tooltip.style.border = "1px solid #ccc";
        tooltip.style.borderRadius = "4px";
        tooltip.style.padding = "4px 8px";
        tooltip.style.fontSize = "14px";
        tooltip.style.zIndex = "100";
        tooltip.style.display = "none";
        document.body.appendChild(tooltip);

        // Set reference to tooltip in editor instance
        editorInstance.tooltip = tooltip;

        // Add mousemove event to show tooltip
        editor.getWrapperElement().addEventListener("mousemove", async function (e) {
            const charCoords = editor.coordsChar({ left: e.clientX, top: e.clientY });

            // Check if we're hovering over text
            if (charCoords.ch >= 0 && editor.getLine(charCoords.line)) {
                const lineText = editor.getLine(charCoords.line);

                // Find if we're inside a {{}} pair
                for (let i = 0; i < lineText.length; i++) {
                    if (lineText.substr(i, 2) === '{{') {
                        let start = i;
                        let end = lineText.indexOf('}}', start);
                        if (end !== -1 && charCoords.ch >= start && charCoords.ch <= end + 1) {
                            // Extract the variable name inside {{}}
                            const variableName = lineText.substring(start + 2, end).trim();

                            // Get the value from .NET component
                            const variableValue = await dotNetRef.invokeMethodAsync('GetVariableValue', variableName);

                            // Show tooltip with the variable name and value
                            tooltip.innerHTML = `${variableName}: <span class="tooltip-value">${variableValue}</span>`;
                            tooltip.style.display = "block";
                            tooltip.style.left = (e.pageX + 10) + "px";
                            tooltip.style.top = (e.pageY + 10) + "px";
                            return;
                        }
                    }
                }
                // No {{}} found at cursor position
                tooltip.style.display = "none";
            } else {
                tooltip.style.display = "none";
            }
        });

        // Hide tooltip when mouse leaves editor
        editor.getWrapperElement().addEventListener("mouseleave", function () {
            tooltip.style.display = "none";
        });
    },

    setupAutocomplete: function (containerId) {
        const editorInstance = this.editors[containerId];
        if (!editorInstance || !editorInstance.editor) return;
        
        const editor = editorInstance.editor;
        const dotNetRef = this.dotNetRefs[containerId];

        editor.on("inputRead", async function (cm, change) {
            if (change.text.length === 1 && change.text[0] === "{") {
                // Check if we already have an opening brace
                const cursor = cm.getCursor();
                const line = cm.getLine(cursor.line);
                const beforeCursor = line.substring(0, cursor.ch - 1);
                
                if (beforeCursor.endsWith("{")) {
                    // We have "{{", start showing suggestions
                    const token = cm.getTokenAt(cursor);
                    const startPos = { line: cursor.line, ch: cursor.ch - 2 };
                    
                    // Get matching variables
                    const variables = await dotNetRef.invokeMethodAsync('GetMatchingVariables', "");
                    
                    // Show completion
                    CodeMirror.showHint(cm, function () {
                        return {
                            list: variables,
                            from: startPos,
                            to: cursor
                        };
                    }, { completeSingle: false });
                }
            }
            else if (change.text.length === 1 && change.origin === "+input") {
                // Check if we're inside a variable declaration
                const cursor = cm.getCursor();
                const line = cm.getLine(cursor.line);
                const beforeCursor = line.substring(0, cursor.ch);
                const lastOpenBrace = beforeCursor.lastIndexOf("{{");
                
                if (lastOpenBrace !== -1 && beforeCursor.indexOf("}}", lastOpenBrace) === -1) {
                    // We're inside a variable declaration, show suggestions filtered by what's typed
                    const prefix = beforeCursor.substring(lastOpenBrace + 2);
                    
                    // Get matching variables
                    const variables = await dotNetRef.invokeMethodAsync('GetMatchingVariables', prefix);
                    
                    if (variables.length > 0) {
                        const startPos = { line: cursor.line, ch: lastOpenBrace + 2 };
                        
                        // Show completion
                        CodeMirror.showHint(cm, function () {
                            return {
                                list: variables,
                                from: startPos,
                                to: cursor
                            };
                        }, { completeSingle: false });
                    }
                }
            }
        });
    }
};
