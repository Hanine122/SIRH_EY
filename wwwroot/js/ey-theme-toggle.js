// Header theme toggle — persists preference in localStorage and swaps the
// moon/sun icon. NOTE: this app has no site-wide dark theme yet; the toggle
// only switches a minimal chrome treatment (topbar/sidebar, see
// ey-portal.css "Q5. Topbar Precision") scoped via [data-theme="dark"] on
// <html>. A full dark-mode pass across every page is a separate, larger
// piece of work, out of scope for this header refactor.
(function () {
    var STORAGE_KEY = 'ey-theme';
    var root = document.documentElement;
    var btn = document.getElementById('ey-theme-toggle');
    if (!btn) return;

    var icon = btn.querySelector('i');

    function apply(theme) {
        if (theme === 'dark') {
            root.setAttribute('data-theme', 'dark');
            btn.setAttribute('aria-pressed', 'true');
            if (icon) { icon.classList.remove('fa-moon'); icon.classList.add('fa-sun'); }
        } else {
            root.removeAttribute('data-theme');
            btn.setAttribute('aria-pressed', 'false');
            if (icon) { icon.classList.remove('fa-sun'); icon.classList.add('fa-moon'); }
        }
    }

    apply(localStorage.getItem(STORAGE_KEY) === 'dark' ? 'dark' : 'light');

    btn.addEventListener('click', function () {
        var next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        localStorage.setItem(STORAGE_KEY, next);
        apply(next);
    });
})();
