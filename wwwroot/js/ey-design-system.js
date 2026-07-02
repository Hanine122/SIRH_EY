/* ═══════════════════════════════════════════════════════════════════════════
   EY ENTERPRISE DESIGN SYSTEM — Runtime v2.0
   Interactive behaviors, Toast system, Tabs, Dropdowns, Ripple, Skeleton
   ═══════════════════════════════════════════════════════════════════════════ */
(function (global) {
    'use strict';

    /* ── Utilities ──────────────────────────────────────────────────────── */
    function on(el, evt, sel, fn) {
        if (typeof sel === 'function') { el.addEventListener(evt, sel); return; }
        el.addEventListener(evt, function (e) {
            var t = e.target.closest(sel);
            if (t && el.contains(t)) fn.call(t, e);
        });
    }
    function qs(sel, ctx)  { return (ctx || document).querySelector(sel); }
    function qsa(sel, ctx) { return Array.from((ctx || document).querySelectorAll(sel)); }

    /* ══════════════════════════════════════════════════════════════════════
       TOAST SYSTEM
       EY.toast(message, type?, duration?)
       type: 'success' | 'error' | 'warning' | 'info' | 'ai' (default 'info')
       ══════════════════════════════════════════════════════════════════════ */
    var ICONS = {
        success: 'fa-check-circle',
        error:   'fa-exclamation-circle',
        warning: 'fa-exclamation-triangle',
        info:    'fa-info-circle',
        ai:      'fa-robot'
    };

    function getContainer() {
        var c = qs('.ds-toast-container');
        if (!c) {
            c = document.createElement('div');
            c.className = 'ds-toast-container';
            document.body.appendChild(c);
        }
        return c;
    }

    function toast(message, type, duration) {
        type     = type     || 'info';
        duration = duration || 4000;
        var container = getContainer();
        var el = document.createElement('div');
        el.className = 'ds-toast ds-toast-' + type;
        el.innerHTML =
            '<i class="fas ' + (ICONS[type] || ICONS.info) + ' ds-toast-icon"></i>' +
            '<span class="ds-toast-message">' + message + '</span>' +
            '<button class="ds-toast-dismiss" aria-label="Fermer">&times;</button>';

        container.appendChild(el);

        el.querySelector('.ds-toast-dismiss').addEventListener('click', function () {
            dismissToast(el);
        });

        if (duration > 0) {
            setTimeout(function () { dismissToast(el); }, duration);
        }
        return el;
    }

    function dismissToast(el) {
        if (!el || el._leaving) return;
        el._leaving = true;
        el.classList.add('leaving');
        el.addEventListener('animationend', function () { el.remove(); }, { once: true });
        setTimeout(function () { if (el.parentNode) el.remove(); }, 600);
    }

    /* ══════════════════════════════════════════════════════════════════════
       TABS  (.ds-tab-list → [data-ds-target="#panel-id"])
       ══════════════════════════════════════════════════════════════════════ */
    function initTabs(container) {
        var lists = qsa('.ds-tab-list', container);
        lists.forEach(function (list) {
            var tabs = qsa('.ds-tab', list);
            tabs.forEach(function (tab) {
                tab.setAttribute('role', 'tab');
                tab.setAttribute('tabindex', tab.classList.contains('active') ? '0' : '-1');
                tab.addEventListener('click', function () {
                    activateTab(tab, list);
                });
                tab.addEventListener('keydown', function (e) {
                    var idx = tabs.indexOf(tab);
                    if (e.key === 'ArrowRight') { e.preventDefault(); activateTab(tabs[(idx + 1) % tabs.length], list); tabs[(idx + 1) % tabs.length].focus(); }
                    if (e.key === 'ArrowLeft')  { e.preventDefault(); activateTab(tabs[(idx - 1 + tabs.length) % tabs.length], list); tabs[(idx - 1 + tabs.length) % tabs.length].focus(); }
                });
            });
        });
    }

    function activateTab(tab, list) {
        var tabs   = qsa('.ds-tab', list);
        var target = tab.dataset.dsTarget;
        var wrap   = list.closest('.ds-tabs') || document;

        tabs.forEach(function (t) {
            t.classList.remove('active');
            t.setAttribute('tabindex', '-1');
            t.setAttribute('aria-selected', 'false');
        });
        tab.classList.add('active');
        tab.setAttribute('tabindex', '0');
        tab.setAttribute('aria-selected', 'true');

        if (target) {
            qsa('.ds-tab-panel', wrap).forEach(function (p) { p.hidden = true; });
            var panel = qs(target, wrap) || qs(target);
            if (panel) {
                panel.hidden = false;
                panel.classList.add('ds-animate-fade-in');
                panel.addEventListener('animationend', function () {
                    panel.classList.remove('ds-animate-fade-in');
                }, { once: true });
            }
        }
        wrap.dispatchEvent(new CustomEvent('ds:tab-change', { detail: { tab: tab, target: target }, bubbles: true }));
    }

    /* ══════════════════════════════════════════════════════════════════════
       DROPDOWNS  (.ds-dropdown → [data-ds-toggle="dropdown"])
       ══════════════════════════════════════════════════════════════════════ */
    function initDropdowns(container) {
        on(container || document, 'click', '[data-ds-toggle="dropdown"]', function (e) {
            e.stopPropagation();
            var trigger = this;
            var dropdown = trigger.closest('.ds-dropdown');
            var menu = qs('.ds-dropdown-menu', dropdown);
            if (!menu) return;

            var isOpen = menu.classList.contains('open');
            closeAllDropdowns();
            if (!isOpen) {
                menu.classList.add('open');
                trigger.setAttribute('aria-expanded', 'true');
                positionDropdown(dropdown, menu);
            }
        });
    }

    function positionDropdown(dropdown, menu) {
        var rect = dropdown.getBoundingClientRect();
        var below = window.innerHeight - rect.bottom > 200;
        menu.style.top = below ? '' : 'auto';
        menu.style.bottom = below ? '' : 'calc(100% + 6px)';
    }

    function closeAllDropdowns() {
        qsa('.ds-dropdown-menu.open').forEach(function (m) {
            m.classList.remove('open');
            var t = m.closest('.ds-dropdown')?.querySelector('[data-ds-toggle="dropdown"]');
            if (t) t.setAttribute('aria-expanded', 'false');
        });
    }

    document.addEventListener('click', closeAllDropdowns);
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeAllDropdowns();
    });

    /* ══════════════════════════════════════════════════════════════════════
       RIPPLE EFFECT  (.ds-ripple-wrap or .ds-btn)
       ══════════════════════════════════════════════════════════════════════ */
    function addRipple(e) {
        var el = this;
        var rect = el.getBoundingClientRect();
        var size = Math.max(rect.width, rect.height);
        var x = (e.clientX || rect.left + rect.width / 2) - rect.left - size / 2;
        var y = (e.clientY || rect.top  + rect.height / 2) - rect.top  - size / 2;
        var ripple = document.createElement('span');
        ripple.className = 'ds-ripple';
        ripple.style.cssText = 'width:' + size + 'px;height:' + size + 'px;left:' + x + 'px;top:' + y + 'px';
        el.appendChild(ripple);
        ripple.addEventListener('animationend', function () { ripple.remove(); }, { once: true });
    }

    function initRipples(container) {
        on(container || document, 'click', '.ds-btn-primary, .ds-btn-ai, .ds-btn-dark, .ds-btn-blue, .ds-ripple-wrap', addRipple);
    }

    /* ══════════════════════════════════════════════════════════════════════
       SKELETON AUTO-REVEAL
       Elements with [data-ds-skeleton-reveal] get revealed on load
       ══════════════════════════════════════════════════════════════════════ */
    function initSkeletons() {
        var skels = qsa('[data-ds-skeleton]');
        skels.forEach(function (sk) {
            var delay = parseInt(sk.dataset.dsSkeletonDelay || '800', 10);
            setTimeout(function () {
                var target = sk.dataset.dsSkeletonReveal;
                sk.style.transition = 'opacity .3s ease';
                sk.style.opacity = '0';
                setTimeout(function () {
                    sk.style.display = 'none';
                    if (target) {
                        var revealed = qs(target);
                        if (revealed) {
                            revealed.hidden = false;
                            revealed.classList.add('ds-animate-fade-in');
                        }
                    }
                }, 300);
            }, delay);
        });
    }

    /* ══════════════════════════════════════════════════════════════════════
       TOOLTIPS  ([data-ds-tooltip="text"])
       ══════════════════════════════════════════════════════════════════════ */
    var tipEl = null;

    function initTooltips() {
        on(document, 'mouseenter', '[data-ds-tooltip]', function (e) {
            var trigger = this;
            var text = trigger.dataset.dsTooltip;
            if (!text) return;

            tipEl = document.createElement('div');
            tipEl.className = 'ds-tooltip';
            tipEl.textContent = text;
            tipEl.style.cssText =
                'position:fixed;z-index:600;background:#1E293B;color:#fff;' +
                'font-size:11px;font-weight:600;padding:5px 10px;border-radius:6px;' +
                'pointer-events:none;white-space:nowrap;opacity:0;' +
                'transition:opacity .15s ease;font-family:var(--font-sans,sans-serif);' +
                'letter-spacing:.02em;box-shadow:0 4px 12px rgba(0,0,0,.25);';
            document.body.appendChild(tipEl);

            var rect = trigger.getBoundingClientRect();
            var pos  = trigger.dataset.dsTooltipPos || 'top';

            setTimeout(function () {
                if (!tipEl) return;
                var tw = tipEl.offsetWidth, th = tipEl.offsetHeight;
                var tx, ty;
                if (pos === 'bottom') { tx = rect.left + rect.width / 2 - tw / 2; ty = rect.bottom + 6; }
                else if (pos === 'left') { tx = rect.left - tw - 6; ty = rect.top + rect.height / 2 - th / 2; }
                else if (pos === 'right') { tx = rect.right + 6;  ty = rect.top + rect.height / 2 - th / 2; }
                else { tx = rect.left + rect.width / 2 - tw / 2; ty = rect.top - th - 6; }

                tx = Math.max(8, Math.min(tx, window.innerWidth  - tw - 8));
                ty = Math.max(8, Math.min(ty, window.innerHeight - th - 8));
                tipEl.style.left = tx + 'px';
                tipEl.style.top  = ty + 'px';
                tipEl.style.opacity = '1';
            }, 0);
        }, true);

        on(document, 'mouseleave', '[data-ds-tooltip]', function () {
            if (tipEl) { tipEl.remove(); tipEl = null; }
        }, true);
    }

    /* ══════════════════════════════════════════════════════════════════════
       CONFIRM DIALOG  (.ds-confirm-btn [data-ds-confirm="message"])
       ══════════════════════════════════════════════════════════════════════ */
    function initConfirm() {
        on(document, 'click', '[data-ds-confirm]', function (e) {
            var msg = this.dataset.dsConfirm || 'Confirmer cette action ?';
            if (!confirm(msg)) e.preventDefault();
        });
    }

    /* ══════════════════════════════════════════════════════════════════════
       AUTO-ANIMATE  — elements with [data-ds-animate] fade up on viewport entry
       ══════════════════════════════════════════════════════════════════════ */
    function initAnimateOnScroll() {
        if (!window.IntersectionObserver) return;
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    var el = entry.target;
                    var anim = el.dataset.dsAnimate || 'ds-animate-fade-up';
                    el.classList.add(anim);
                    obs.unobserve(el);
                }
            });
        }, { threshold: .12 });
        qsa('[data-ds-animate]').forEach(function (el) { obs.observe(el); });
    }

    /* ══════════════════════════════════════════════════════════════════════
       PROGRESS BAR ANIMATE   [data-ds-progress="75"]
       ══════════════════════════════════════════════════════════════════════ */
    function initProgressBars() {
        if (!window.IntersectionObserver) {
            qsa('.ds-progress-bar[data-ds-progress]').forEach(function (b) {
                b.style.width = b.dataset.dsProgress + '%';
            });
            return;
        }
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    var bar = entry.target;
                    setTimeout(function () {
                        bar.style.width = (bar.dataset.dsProgress || 0) + '%';
                    }, parseInt(bar.dataset.dsProgressDelay || '0', 10));
                    obs.unobserve(bar);
                }
            });
        }, { threshold: .2 });
        qsa('.ds-progress-bar[data-ds-progress]').forEach(function (b) {
            b.style.width = '0%';
            obs.observe(b);
        });
    }

    /* ══════════════════════════════════════════════════════════════════════
       COUNTER ANIMATE   [data-ds-counter="1234"]
       ══════════════════════════════════════════════════════════════════════ */
    function animateCounter(el) {
        var target  = parseFloat(el.dataset.dsCounter);
        var suffix  = el.dataset.dsCounterSuffix || '';
        var prefix  = el.dataset.dsCounterPrefix || '';
        var duration = parseInt(el.dataset.dsCounterDuration || '1200', 10);
        var start   = Date.now();
        var from    = 0;

        function tick() {
            var elapsed = Date.now() - start;
            var progress = Math.min(elapsed / duration, 1);
            var eased = 1 - Math.pow(1 - progress, 3);
            var current = from + (target - from) * eased;
            el.textContent = prefix + (Number.isInteger(target) ? Math.round(current) : current.toFixed(1)) + suffix;
            if (progress < 1) requestAnimationFrame(tick);
        }
        requestAnimationFrame(tick);
    }

    function initCounters() {
        if (!window.IntersectionObserver) {
            qsa('[data-ds-counter]').forEach(function (el) {
                el.textContent = (el.dataset.dsCounterPrefix || '') + el.dataset.dsCounter + (el.dataset.dsCounterSuffix || '');
            });
            return;
        }
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    animateCounter(entry.target);
                    obs.unobserve(entry.target);
                }
            });
        }, { threshold: .4 });
        qsa('[data-ds-counter]').forEach(function (el) {
            el.textContent = (el.dataset.dsCounterPrefix || '') + '0' + (el.dataset.dsCounterSuffix || '');
            obs.observe(el);
        });
    }

    /* ══════════════════════════════════════════════════════════════════════
       AI STREAM SIMULATION   [data-ds-stream="text"]
       Simulates token streaming on element; fires 'ds:stream-done' when done
       ══════════════════════════════════════════════════════════════════════ */
    function streamText(el, text, speed) {
        speed = speed || 20;
        el.classList.add('ds-ai-stream');
        el.textContent = '';
        var i = 0;
        function step() {
            if (i < text.length) {
                el.textContent += text[i++];
                setTimeout(step, speed + Math.random() * speed * .5);
            } else {
                el.classList.add('done');
                el.dispatchEvent(new CustomEvent('ds:stream-done', { bubbles: true }));
            }
        }
        step();
    }

    /* ══════════════════════════════════════════════════════════════════════
       COPY TO CLIPBOARD   [data-ds-copy="text or selector"]
       ══════════════════════════════════════════════════════════════════════ */
    function initCopyButtons() {
        on(document, 'click', '[data-ds-copy]', function () {
            var btn    = this;
            var source = btn.dataset.dsCopy;
            var text   = qs(source) ? (qs(source).value || qs(source).textContent) : source;
            if (!text) return;
            navigator.clipboard.writeText(text.trim()).then(function () {
                var orig = btn.innerHTML;
                btn.innerHTML = '<i class="fas fa-check"></i>';
                setTimeout(function () { btn.innerHTML = orig; }, 1500);
                toast('Copié dans le presse-papiers', 'success', 2000);
            });
        });
    }

    /* ══════════════════════════════════════════════════════════════════════
       DISMISS ALERTS   [data-ds-dismiss="alert"]
       ══════════════════════════════════════════════════════════════════════ */
    function initDismiss() {
        on(document, 'click', '[data-ds-dismiss]', function () {
            var target = this.dataset.dsDismiss;
            var el = target === 'self' ? this : this.closest('.' + target) || this.parentElement;
            if (!el) return;
            el.style.transition = 'opacity .2s ease, transform .2s ease';
            el.style.opacity = '0';
            el.style.transform = 'translateY(-4px)';
            setTimeout(function () { el.remove(); }, 220);
        });
    }

    /* ══════════════════════════════════════════════════════════════════════
       AUTO-RESIZE TEXTAREAS
       ══════════════════════════════════════════════════════════════════════ */
    function initAutoResize() {
        on(document, 'input', 'textarea.ds-auto-resize, .ds-chat-input', function () {
            this.style.height = 'auto';
            this.style.height = Math.min(this.scrollHeight, 200) + 'px';
        });
    }

    /* ══════════════════════════════════════════════════════════════════════
       SEARCH FILTER   [data-ds-filter-input] → [data-ds-filter-target]
       Filters rows/items containing the typed text
       ══════════════════════════════════════════════════════════════════════ */
    function initSearch() {
        on(document, 'input', '[data-ds-filter-input]', function () {
            var input  = this;
            var sel    = input.dataset.dsFilterInput || input.dataset.dsFilterTarget;
            var items  = qsa(sel || '[data-ds-filter-item]');
            var q      = input.value.toLowerCase().trim();
            var empty  = qs(input.dataset.dsFilterEmpty);
            var visible = 0;
            items.forEach(function (item) {
                var match = !q || item.textContent.toLowerCase().includes(q);
                item.style.display = match ? '' : 'none';
                if (match) visible++;
            });
            if (empty) empty.hidden = visible > 0;
        });
    }

    /* ══════════════════════════════════════════════════════════════════════
       MODAL  [data-ds-modal-open="id"] / [data-ds-modal-close]
       ══════════════════════════════════════════════════════════════════════ */
    function initModals() {
        on(document, 'click', '[data-ds-modal-open]', function () {
            var id = this.dataset.dsModalOpen;
            var modal = qs('#' + id + ', [data-ds-modal-id="' + id + '"]');
            if (modal) openModal(modal);
        });
        on(document, 'click', '[data-ds-modal-close]', function () {
            var modal = this.closest('[data-ds-modal-id], .ds-modal');
            if (modal) closeModal(modal);
        });
        on(document, 'click', '.ds-modal-backdrop', function (e) {
            if (e.target === this) closeModal(this.querySelector('.ds-modal-inner') || this);
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                var open = qs('.ds-modal.open');
                if (open) closeModal(open);
            }
        });
    }

    function openModal(modal) {
        modal.classList.add('open');
        modal.hidden = false;
        document.body.style.overflow = 'hidden';
        var focusable = modal.querySelector('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
        if (focusable) setTimeout(function () { focusable.focus(); }, 50);
    }

    function closeModal(modal) {
        modal.classList.remove('open');
        setTimeout(function () { modal.hidden = true; }, 250);
        document.body.style.overflow = '';
    }

    /* ══════════════════════════════════════════════════════════════════════
       FORM VALIDATION HELPERS
       ══════════════════════════════════════════════════════════════════════ */
    function initFormValidation() {
        on(document, 'blur', '.ds-input[required]', function () {
            var el = this;
            var group = el.closest('.ds-form-group');
            if (!group) return;
            var err = group.querySelector('.ds-error');
            if (el.value.trim() === '') {
                el.classList.add('is-error');
                if (err) err.hidden = false;
            } else {
                el.classList.remove('is-error');
                el.classList.add('is-valid');
                if (err) err.hidden = true;
            }
        }, true);
    }

    /* ══════════════════════════════════════════════════════════════════════
       INIT — runs on DOMContentLoaded
       ══════════════════════════════════════════════════════════════════════ */
    function init() {
        initTabs(document);
        initDropdowns(document);
        initRipples(document);
        initSkeletons();
        initTooltips();
        initConfirm();
        initAnimateOnScroll();
        initProgressBars();
        initCounters();
        initCopyButtons();
        initDismiss();
        initAutoResize();
        initSearch();
        initModals();
        initFormValidation();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    /* ══════════════════════════════════════════════════════════════════════
       PUBLIC API
       ══════════════════════════════════════════════════════════════════════ */
    global.EY = {
        toast:       toast,
        streamText:  streamText,
        openModal:   openModal,
        closeModal:  closeModal,
        activateTab: activateTab,
        initTabs:    initTabs,
        initDropdowns: initDropdowns,
        initRipples:   initRipples,
        initProgressBars: initProgressBars,
        initCounters:     initCounters,
        animateCounter:   animateCounter,

        /* Convenience: show a named notification */
        notify: function (msg, type) { return toast(msg, type || 'info', 4500); },

        /* Re-init all behaviors on newly inserted DOM */
        reInit: function (container) {
            initTabs(container);
            initDropdowns(container);
            initRipples(container);
            initProgressBars();
            initCounters();
        }
    };

}(window));
