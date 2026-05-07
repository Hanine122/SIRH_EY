document.addEventListener('DOMContentLoaded', function () {
    const launcher = document.getElementById('chatbot-launcher');
    const panel = document.getElementById('chatbot-panel');
    const closeBtn = document.getElementById('chatbot-close-btn');
    const launcherIcon = launcher.querySelector('.chatbot-launcher-icon');
    const closeIcon = launcher.querySelector('.chatbot-close-icon');
    
    const inputArea = document.getElementById('chatbot-input');
    const sendBtn = document.getElementById('chatbot-send-btn');
    const messagesContainer = document.getElementById('chatbot-messages');
    const quickPromptsContainer = document.getElementById('chatbot-quick-prompts');

    let isChatOpen = false;

    // Toggle Chat Panel
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

    // Input Handling
    inputArea.addEventListener('input', function () {
        sendBtn.disabled = inputArea.value.trim() === '';
    });

    inputArea.addEventListener('keypress', function (e) {
        if (e.key === 'Enter' && !sendBtn.disabled) {
            sendMessage();
        }
    });

    sendBtn.addEventListener('click', function () {
        if (!sendBtn.disabled) {
            sendMessage();
        }
    });

    // Handle Quick Prompts
    document.querySelectorAll('.quick-prompt-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const prompt = this.getAttribute('data-prompt');
            inputArea.value = prompt;
            sendBtn.disabled = false;
            if (quickPromptsContainer) {
                quickPromptsContainer.style.display = 'none';
            }
            sendMessage();
        });
    });

    function scrollToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    function appendMessage(text, isUser) {
        const msgDiv = document.createElement('div');
        msgDiv.className = `chat-message ${isUser ? 'user-message' : 'bot-message'}`;
        
        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        contentDiv.textContent = text; // Safe text insertion
        
        msgDiv.appendChild(contentDiv);
        messagesContainer.appendChild(msgDiv);
        scrollToBottom();
    }

    function showTypingIndicator() {
        const indicator = document.createElement('div');
        indicator.className = 'chat-message bot-message typing-indicator-container';
        indicator.id = 'typing-indicator';
        
        indicator.innerHTML = `
            <div class="typing-indicator">
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
            </div>
        `;
        
        messagesContainer.appendChild(indicator);
        scrollToBottom();
    }

    function removeTypingIndicator() {
        const indicator = document.getElementById('typing-indicator');
        if (indicator) {
            indicator.remove();
        }
    }

    async function sendMessage() {
        const messageText = inputArea.value.trim();
        if (!messageText) return;

        // Hide quick prompts if still visible
        if (quickPromptsContainer && quickPromptsContainer.style.display !== 'none') {
            quickPromptsContainer.style.display = 'none';
        }

        // Add user message to UI
        appendMessage(messageText, true);
        
        // Clear input
        inputArea.value = '';
        sendBtn.disabled = true;

        // Show typing indicator
        showTypingIndicator();

        try {
            // Call ASP.NET MVC Proxy Controller
            const response = await fetch('/api/chatbot/ask', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    // Note: You can add RequestVerificationToken here if using AntiForgery
                },
                body: JSON.stringify({ message: messageText })
            });

            removeTypingIndicator();

            if (response.ok) {
                const data = await response.json();
                appendMessage(data.reply || 'Désolé, aucune réponse reçue.', false);
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
