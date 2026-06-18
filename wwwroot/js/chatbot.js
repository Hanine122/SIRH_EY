document.addEventListener('DOMContentLoaded', function () {

    // ── Conversational memory ─────────────────────────────────────────────────
    const conversationCtx = {
        lastIntent: null,
        lastCollaborateurId: null,
        lastCollaborateurNom: null
    };

    const launcher              = document.getElementById('chatbot-launcher');
    const panel                 = document.getElementById('chatbot-panel');
    const closeBtn              = document.getElementById('chatbot-close-btn');
    const launcherIcon          = launcher.querySelector('.chatbot-launcher-icon');
    const closeIcon             = launcher.querySelector('.chatbot-close-icon');
    const inputArea             = document.getElementById('chatbot-input');
    const sendBtn               = document.getElementById('chatbot-send-btn');
    const messagesContainer     = document.getElementById('chatbot-messages');
    const quickPromptsContainer = document.getElementById('chatbot-quick-prompts');

    let isChatOpen = false;

    // ── Toggle panel ──────────────────────────────────────────────────────────
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

    // ── Input events ──────────────────────────────────────────────────────────
    inputArea.addEventListener('input', () => {
        sendBtn.disabled = inputArea.value.trim() === '';
    });
    inputArea.addEventListener('keypress', e => {
        if (e.key === 'Enter' && !sendBtn.disabled) sendMessage();
    });
    sendBtn.addEventListener('click', () => {
        if (!sendBtn.disabled) sendMessage();
    });

    // ── Quick prompts ─────────────────────────────────────────────────────────
    document.querySelectorAll('.quick-prompt-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            inputArea.value = this.getAttribute('data-prompt');
            sendBtn.disabled = false;
            if (quickPromptsContainer) quickPromptsContainer.style.display = 'none';
            sendMessage();
        });
    });

    function scrollToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    // ── Page context detection ────────────────────────────────────────────────
    function detectPageContext() {
        const path = window.location.pathname.toLowerCase();
        if (path.includes('succession')) {
            const match   = path.match(/\/(\d+)/);
            const pageEl  = document.querySelector('[data-succession-id]');
            const id      = (match && match[1]) || (pageEl && pageEl.dataset.successionId) || null;
            return { page: 'succession', contextId: id };
        }
        if (path.includes('talent') || path.includes('ninebox'))
            return { page: 'talent', contextId: null };
        return { page: 'general', contextId: null };
    }

    // ── User message bubble ───────────────────────────────────────────────────
    function appendUserMessage(text) {
        const wrap = document.createElement('div');
        wrap.className = 'chat-message user-message';
        const inner = document.createElement('div');
        inner.className = 'message-content';
        inner.textContent = text;          // textContent = XSS-safe for user input
        wrap.appendChild(inner);
        messagesContainer.appendChild(wrap);
        scrollToBottom();
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    // Safe HTML escape (uses the browser's own escaping)
    function escapeHtml(str) {
        if (!str) return '';
        const d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

    // Extract up to 2 initials from a name
    function getInitials(name) {
        return (name || '').split(/\s+/).slice(0, 2)
            .map(w => w[0] ? w[0].toUpperCase() : '').join('');
    }

    // Markdown-lite: **bold**, *italic*, \n → <br>
    // B4 fix: removed the erroneous • → <br>• substitution (double-break)
    function formatText(text) {
        if (!text) return '';
        return text
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/\n/g, '<br>');
    }

    // Renders 5-dot skill level indicator
    function renderDots(level) {
        let h = '';
        for (let i = 1; i <= 5; i++)
            h += `<span class="ey-dot${i <= Math.round(level) ? ' on' : ''}"></span>`;
        return h;
    }

    // ── Priority 3: Smart enterprise content renderer ─────────────────────────
    //
    // Detects content type from n8n response text and produces
    // the appropriate visual component:
    //   • Person chips  — for talent / succession / promotion lists
    //   • Dept bar list — for headcount distribution
    //   • Skill dots    — for competence breakdowns
    //   • KPI highlight — for single-stat answers
    //   • Simple list   — fallback for unrecognised bullet lists
    //   • Plain text    — for narrative / error messages
    //
    function renderContent(text) {
        if (!text) return '';

        const lines   = text.split('\n').map(l => l.trim()).filter(Boolean);
        const bullets = lines.filter(l => l.startsWith('•'));
        const prose   = lines.filter(l => !l.startsWith('•'));

        // Prose intro rendered above the cards
        const introHtml = prose.length
            ? `<p class="ey-answer-text">${formatText(prose.join('\n'))}</p>`
            : '';

        if (bullets.length === 0) {
            // No bullets — check for KPI numbers (**25**, **75%**, etc.)
            if (/\*\*[\d.,]+%?\*\*/.test(text))
                return renderKpiLine(text);
            return `<p class="ey-answer-text">${formatText(text)}</p>`;
        }

        // Classify bullet list by content
        const isDept   = bullets.some(l => /:\s*\d+\s*(collaborateur|collab|inscription|formation)/i.test(l));
        const isPeople = bullets.some(l => /\([A-Za-zÀ-ÿÇçÉéÈèÊêÀàÙùÔôÎîÏïŒœ\s\-']+\)/.test(l));
        const isSkill  = bullets.some(l => /niveau moy\.|—\s*\d+\s*collab/i.test(l));

        if (isDept)    return introHtml + renderDeptCards(bullets);
        if (isPeople)  return introHtml + renderPersonCards(bullets);
        if (isSkill)   return introHtml + renderSkillCards(bullets);
        return          introHtml + renderSimpleList(bullets);
    }

    // KPI: numbers get large bold treatment
    function renderKpiLine(text) {
        const formatted = text
            .replace(/\*\*([\d.,]+%?)\*\*/g, '<span class="ey-kpi-val">$1</span>')
            .replace(/\n/g, '<br>');
        return `<p class="ey-kpi-text">${formatted}</p>`;
    }

    // Person chips — talent, succession, promotion cards
    function renderPersonCards(bullets) {
        let h = `<div class="ey-person-list">`;
        bullets.forEach(b => {
            const raw   = b.replace(/^•\s*/, '').trim();
            const paren = raw.match(/^(.+?)\s*\(([^)]+)\)(.*)?$/);
            const dash  = !paren && raw.match(/^(.+?)\s*—\s*(.+)$/);

            if (paren) {
                const name  = paren[1].trim();
                const role  = paren[2].trim();
                const extra = (paren[3] || '').replace(/^[\s,·—]+/, '').trim();
                h += personChip(name, role + (extra ? ' · ' + extra : ''));
            } else if (dash) {
                h += personChip(dash[1].trim(), dash[2].trim());
            } else {
                h += personChip(raw, '');
            }
        });
        return h + `</div>`;
    }

    function personChip(name, role) {
        return `<div class="ey-person-chip">
            <div class="ey-person-avatar">${getInitials(name)}</div>
            <div class="ey-person-info">
                <span class="ey-person-name">${escapeHtml(name)}</span>
                ${role ? `<span class="ey-person-role">${escapeHtml(role)}</span>` : ''}
            </div>
        </div>`;
    }

    // Department bar chart — headcount distribution
    function renderDeptCards(bullets) {
        const counts = bullets.map(b => parseInt((b.match(/:\s*(\d+)/) || [])[1]) || 0);
        const max    = Math.max(...counts, 1);
        let h = `<div class="ey-dept-list">`;
        bullets.forEach((b, i) => {
            const raw = b.replace(/^•\s*/, '').trim();
            const ci  = raw.lastIndexOf(':');
            if (ci < 0) { h += `<div class="ey-simple-item">${escapeHtml(raw)}</div>`; return; }
            const name  = raw.substring(0, ci).trim();
            const count = counts[i];
            const pct   = Math.round((count / max) * 100);
            h += `<div class="ey-dept-row">
                <span class="ey-dept-name" title="${escapeHtml(name)}">${escapeHtml(name)}</span>
                <div class="ey-dept-bar"><div class="ey-dept-fill" style="width:${pct}%"></div></div>
                <span class="ey-dept-count">${count}</span>
            </div>`;
        });
        return h + `</div>`;
    }

    // Skill dots — competence breakdowns
    function renderSkillCards(bullets) {
        let h = `<div class="ey-skill-list">`;
        bullets.forEach(b => {
            const raw  = b.replace(/^•\s*/, '').trim();
            const di   = raw.indexOf(' — ');
            const name = di > -1 ? raw.substring(0, di).trim() : raw;
            const meta = di > -1 ? raw.substring(di + 3).trim() : '';
            const lm   = meta.match(/niveau moy\.\s*([\d.]+)\s*\/\s*5/);
            const lvl  = lm ? parseFloat(lm[1]) : null;
            const clean = meta.replace(/\s*\(niveau moy\.[^)]*\)/, '').trim();
            h += `<div class="ey-skill-row">
                <span class="ey-skill-name">${escapeHtml(name)}</span>
                ${lvl !== null ? `<div class="ey-skill-dots">${renderDots(lvl)}</div>` : ''}
                ${clean ? `<span class="ey-skill-meta">${escapeHtml(clean)}</span>` : ''}
            </div>`;
        });
        return h + `</div>`;
    }

    // Generic bullet fallback
    function renderSimpleList(bullets) {
        let h = `<div class="ey-simple-list">`;
        bullets.forEach(b => {
            h += `<div class="ey-simple-item">${formatText(b.replace(/^•\s*/, ''))}</div>`;
        });
        return h + `</div>`;
    }
    function escapeHtml(str) {
    if (str === null || str === undefined) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

    // ── Bot response renderer ─────────────────────────────────────────────────
    //
    // B1 fix: all responses now wrap content in .message-content (gets card styling)
    // B2 fix: action items strip leading → before CSS ::before adds its own
    // Priority 2: handles both {reply} (UC1 stats) and {answer+analysis+actions+suggestions} (UC2 copilot)
    //
    function appendCopilotResponse(data) {
    const wrapper = document.createElement('div');
    wrapper.className = 'chat-message bot-message';

    // Priority 2: normalise both "reply" (n8n UC1) and "answer" (copilot)
    const mainText   = data.answer || data.reply || '';
    const hasCopilot = !!(
        data.analysis ||
        (data.actions     && data.actions.length     > 0) ||
        (data.suggestions && data.suggestions.length > 0) ||
        (data.reasoning    && data.reasoning.length    > 0) ||
        (data.cards        && data.cards.length        > 0)
    );

    const contentDiv = document.createElement('div');

    if (hasCopilot) {
        // ── Full copilot card ──────────────────────────────
        contentDiv.className = 'message-content copilot-card';
        let html = '';

        if (mainText)
            html += `<div class="copilot-answer">${renderContent(mainText)}</div>`;

        if (data.analysis)
            html += `<div class="copilot-analysis">
                        <span class="copilot-icon">💡</span>
                        <div>${formatText(data.analysis)}</div>
                     </div>`;

        // ── Reasoning (pourquoi ce choix) ───────────────────
        if (data.reasoning && data.reasoning.length > 0) {
            html += `<div class="copilot-section-label">Pourquoi ce choix</div>
                     <div class="copilot-reasoning">`;
            data.reasoning.forEach(r => {
                if (!r || typeof r !== 'object') return;
                if (r.collaborateur) {
                    html += `<div class="copilot-reasoning-item">
                                <strong>${escapeHtml(r.collaborateur)}</strong>
                                ${r.rang ? `(rang ${r.rang})` : ''} — ${escapeHtml(r.pourquoi || '')}
                                ${r.lacunes ? `<br><span class="copilot-reasoning-muted">Lacunes : ${escapeHtml(r.lacunes)}</span>` : ''}
                             </div>`;
                } else if (r.critere) {
                    html += `<div class="copilot-reasoning-item">
                                <strong>${escapeHtml(r.critere)}</strong> — ${escapeHtml(r.detail || '')}
                             </div>`;
                }
            });
            html += `</div>`;
        }

        if (data.actions && data.actions.length > 0) {
            html += `<div class="copilot-section-label">Actions recommandées</div>
                     <div class="copilot-actions">`;
            data.actions.forEach(a => {
                if (a) {
                    const clean = a.replace(/^\s*→\s*/, '').trim();
                    html += `<div class="copilot-action-item">${formatText(clean)}</div>`;
                }
            });
            html += `</div>`;
        }

        if (data.suggestions && data.suggestions.length > 0) {
            html += `<div class="copilot-section-label">Approfondir</div>
                     <div class="copilot-suggestions">`;
            data.suggestions.forEach(s => {
                if (!s) return;
                const parts = s.split('|');
                const label = escapeHtml(parts[0].trim());
                const ctxId = parts[1] ? escapeHtml(parts[1].trim()) : '';
                // Skip si un ID était attendu (pipe présent) mais vide
                if (s.includes('|') && !ctxId) return;
                html += `<button type="button" class="copilot-suggestion-btn"
                                 data-suggestion="${escapeHtml(s)}"
                                 data-context-id="${ctxId}"
                                 onclick="window._copilotSend(this)">${label}</button>`;
            });
            html += `</div>`;
        }

        contentDiv.innerHTML = html;

    } else {
        // ── Simple card (stats / plain text) ──────────────
        contentDiv.className = 'message-content';
        contentDiv.innerHTML = renderContent(mainText);
    }

    wrapper.appendChild(contentDiv);
    messagesContainer.appendChild(wrapper);
    scrollToBottom();

    if (data.contextId) conversationCtx.lastCollaborateurId = data.contextId;
    if (data.title)     conversationCtx.lastIntent          = data.title;

    // Mémoire de session pour le prochain envoi
    if (data.executionHistory) {
        window._sessionHistory = data.executionHistory;
    }
}

    // ── Suggestion click handler (global) ─────────────────────────────────────
    // B3 fix: calls detectPageContext() directly instead of referencing
    //         the local `pageContext` variable from sendMessage() that was out of scope.
   window._copilotSend = function (btn) {
    const raw  = btn.getAttribute('data-suggestion') || btn.textContent.trim();
    const text = raw.split('|')[0].trim();

    // Priorité : ID du bouton cliqué > page > mémoire de session
    const explicitContextId = btn.getAttribute('data-context-id');
    const ctx = detectPageContext();
    const contextId = (explicitContextId && explicitContextId.trim() !== '')
        ? explicitContextId.trim()
        : (ctx.contextId || conversationCtx.lastCollaborateurId || null);

    inputArea.value = text;
    sendBtn.disabled = false;
    window._pendingContextId = contextId;
    sendMessage();
};

    // ── Typing indicator ──────────────────────────────────────────────────────
    function showTypingIndicator() {
        const el = document.createElement('div');
        el.className = 'chat-message bot-message typing-indicator-container';
        el.id = 'typing-indicator';
        el.innerHTML = `<div class="typing-indicator">
            <div class="typing-dot"></div>
            <div class="typing-dot"></div>
            <div class="typing-dot"></div>
        </div>`;
        messagesContainer.appendChild(el);
        scrollToBottom();
    }

    function removeTypingIndicator() {
        const el = document.getElementById('typing-indicator');
        if (el) el.remove();
    }

    // ── Send message ──────────────────────────────────────────────────────────
    async function sendMessage() {
        const text = inputArea.value.trim();
        if (!text) return;

        if (quickPromptsContainer && quickPromptsContainer.style.display !== 'none')
            quickPromptsContainer.style.display = 'none';

        appendUserMessage(text);
        inputArea.value  = '';
        sendBtn.disabled = true;
        showTypingIndicator();

        const pageCtx  = detectPageContext();
        const contextId = window._pendingContextId || pageCtx.contextId || null;
        window._pendingContextId = null;

        try {
            const res = await fetch('/api/chatbot/ask', {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: text, page: pageCtx.page, contextId })
            });
            removeTypingIndicator();

            if (res.ok) {
                appendCopilotResponse(await res.json());
            } else {
                appendCopilotResponse({ reply: 'Erreur lors de la communication avec le serveur.' });
            }
        } catch (err) {
            console.error('Chatbot error:', err);
            removeTypingIndicator();
            appendCopilotResponse({ reply: 'Impossible de joindre le service IA pour le moment.' });
        }
    }

});
