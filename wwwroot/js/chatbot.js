

document.addEventListener('DOMContentLoaded', function () {
    // Mémoire conversationnelle courte
const conversationCtx = {
    lastIntent: null,
    lastCollaborateurId: null,
    lastCollaborateurNom: null
};
    const launcher    = document.getElementById('chatbot-launcher');
    const panel       = document.getElementById('chatbot-panel');
    const closeBtn    = document.getElementById('chatbot-close-btn');
    const launcherIcon = launcher.querySelector('.chatbot-launcher-icon');
    const closeIcon   = launcher.querySelector('.chatbot-close-icon');
    const inputArea   = document.getElementById('chatbot-input');
    const sendBtn     = document.getElementById('chatbot-send-btn');
    const messagesContainer     = document.getElementById('chatbot-messages');
    const quickPromptsContainer = document.getElementById('chatbot-quick-prompts');

    let isChatOpen = false;

    // ── Toggle ────────────────────────────────────────────────
    function toggleChat() {
        isChatOpen = !isChatOpen;
        if (isChatOpen) {
            panel.style.display = 'flex';
            launcherIcon.style.display = 'none';
            closeIcon.style.display = 'block';
            inputArea.focus();
            scrollToBottom();
        } else {
            panel.style.display = 'none';
            launcherIcon.style.display = 'block';
            closeIcon.style.display = 'none';
        }
    }

    launcher.addEventListener('click', toggleChat);
    closeBtn.addEventListener('click', toggleChat);

    // ── Input ─────────────────────────────────────────────────
    inputArea.addEventListener('input', function () {
        sendBtn.disabled = inputArea.value.trim() === '';
    });

    inputArea.addEventListener('keypress', function (e) {
        if (e.key === 'Enter' && !sendBtn.disabled) sendMessage();
    });

    sendBtn.addEventListener('click', function () {
        if (!sendBtn.disabled) sendMessage();
    });

    // ── Quick prompts ─────────────────────────────────────────
    document.querySelectorAll('.quick-prompt-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const prompt = this.getAttribute('data-prompt');
            inputArea.value = prompt;
            sendBtn.disabled = false;
            if (quickPromptsContainer) quickPromptsContainer.style.display = 'none';
            sendMessage();
        });
    });

    function scrollToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    // ── Detect page context ───────────────────────────────────
    function detectPageContext() {
        const path = window.location.pathname.toLowerCase();

        if (path.includes('succession')) {
            const match = path.match(/\/(\d+)/);
            const contextId = match ? match[1] : null;
            const pageData = document.querySelector('[data-succession-id]');
            const successionId = contextId
                || (pageData ? pageData.dataset.successionId : null);
            return { page: 'succession', contextId: successionId };
        }

        if (path.includes('talent') || path.includes('ninebox')) {
            return { page: 'talent', contextId: null };
        }

        return { page: 'general', contextId: null };
    }

    // ── User message ──────────────────────────────────────────
    function appendMessage(text, isUser) {
        const msgDiv = document.createElement('div');
        msgDiv.className = `chat-message ${isUser ? 'user-message' : 'bot-message'}`;
        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        contentDiv.textContent = text;
        msgDiv.appendChild(contentDiv);
        messagesContainer.appendChild(msgDiv);
        scrollToBottom();
    }

    // ── Format markdown-lite ──────────────────────────────────
    function formatText(text) {
        if (!text) return '';
        return text
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/•/g, '<br>•')
            .replace(/\n/g, '<br>');
    }

    // ── Copilot response ──────────────────────────────────────
    function appendCopilotResponse(data) {
        const wrapper = document.createElement('div');
        wrapper.className = 'chat-message bot-message';

        // Compatibilité : certains workflows retournent "reply" d'autres "answer"
        const mainText = data.answer || data.reply || '';

        let html = '';

        if (mainText) {
            html += `<div class="copilot-answer">${formatText(mainText)}</div>`;
        }

        if (data.analysis) {
            html += `<div class="copilot-analysis">
                        <span class="copilot-icon">💡</span>
                        <div>${data.analysis}</div>
                     </div>`;
        }

        if (data.actions && data.actions.length > 0) {
            html += `<div class="copilot-section-label">Actions recommandées</div>
                     <div class="copilot-actions">`;
            data.actions.forEach(action => {
                html += `<div class="copilot-action-item">→ ${action}</div>`;
            });
            html += `</div>`;
        }

        if (data.suggestions && data.suggestions.length > 0) {
            html += `<div class="copilot-section-label">Approfondir</div>
                     <div class="copilot-suggestions">`;
            data.suggestions.forEach(suggestion => {
                // Format : "Texte affiché|contextId" ou "Texte affiché"
                const parts     = suggestion.split('|');
                const label     = parts[0].trim();
                const contextId = parts[1] ? parts[1].trim() : '';
                html += `<button class="copilot-suggestion-btn"
                                 data-suggestion="${suggestion}"
                                 data-context-id="${contextId}"
                                 onclick="window._copilotSend(this)">
                           ${label}
                         </button>`;
            });
            html += `</div>`;
            // Mettre à jour la mémoire conversationnelle
if (data.contextId) conversationCtx.lastCollaborateurId = data.contextId;
if (data.title)     conversationCtx.lastIntent = data.title;
        }

        wrapper.innerHTML = html;
        messagesContainer.appendChild(wrapper);
        scrollToBottom();
    }

    // ── Suggestion click handler (global) ─────────────────────
    window._copilotSend = function (btn) {
        const raw       = btn.getAttribute('data-suggestion');
       const contextId = window._pendingContextId
               || pageContext.contextId
               || conversationCtx.lastCollaborateurId
               || null;
window._pendingContextId = null;
        const parts     = raw.split('|');
        const text      = parts[0].trim();

        inputArea.value = text;
        sendBtn.disabled = false;

        // Stocker le contextId pour l'envoi suivant
        window._pendingContextId = contextId || null;

        sendMessage();
    };

    // ── Typing indicator ──────────────────────────────────────
    function showTypingIndicator() {
        const indicator = document.createElement('div');
        indicator.className = 'chat-message bot-message typing-indicator-container';
        indicator.id = 'typing-indicator';
        indicator.innerHTML = `
            <div class="typing-indicator">
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
            </div>`;
        messagesContainer.appendChild(indicator);
        scrollToBottom();
    }

    function removeTypingIndicator() {
        const indicator = document.getElementById('typing-indicator');
        if (indicator) indicator.remove();
    }

    // ── Send message ──────────────────────────────────────────
    async function sendMessage() {
        const messageText = inputArea.value.trim();
        if (!messageText) return;

        if (quickPromptsContainer &&
            quickPromptsContainer.style.display !== 'none') {
            quickPromptsContainer.style.display = 'none';
        }

        appendMessage(messageText, true);
        inputArea.value  = '';
        sendBtn.disabled = true;
        showTypingIndicator();

        // Contexte page + contextId depuis suggestion ou page
        const pageContext = detectPageContext();
        const contextId   = window._pendingContextId
                         || pageContext.contextId
                         || null;
        window._pendingContextId = null; // reset après usage

        try {
            const response = await fetch('/api/chatbot/ask', {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    message:   messageText,
                    page:      pageContext.page,
                    contextId: contextId
                })
            });

            removeTypingIndicator();

            if (response.ok) {
                const data = await response.json();
                appendCopilotResponse(data);
            } else {
                appendMessage('Erreur lors de la communication avec le serveur.', false);
            }
        } catch (error) {
            console.error('Chatbot API error:', error);
            removeTypingIndicator();
            appendMessage('Impossible de joindre le service IA.', false);
        }
    }
});