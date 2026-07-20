// Delegated click handler for .ceb-validate-btn — POSTs to
// /Competences/ValiderCompetence. Shared by Competences/Index.cshtml and
// Collaborateurs/Index.cshtml so both pages call the same endpoint the same
// way. Requires a __RequestVerificationToken hidden field somewhere on the
// page (e.g. @Html.AntiForgeryToken()).
(function () {
    'use strict';

    document.addEventListener('click', async function (e) {
        var btn = e.target.closest('.ceb-validate-btn');
        if (!btn) return;

        var compId = btn.getAttribute('data-comp-id');
        var targetScore = btn.getAttribute('data-target-score');
        var originalText = btn.textContent;
        var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        btn.disabled = true;
        btn.textContent = 'Validation…';

        try {
            var response = await fetch(
                '/Competences/ValiderCompetence?compId=' + encodeURIComponent(compId) + '&note=' + encodeURIComponent(targetScore),
                {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token }
                }
            );
            var result = await response.json();
            if (result.success) {
                window.location.reload();
            } else {
                alert('Erreur : ' + (result.message || 'Une erreur est survenue lors de la validation.'));
                btn.disabled = false;
                btn.textContent = originalText;
            }
        } catch (err) {
            alert('Erreur réseau lors de la validation.');
            btn.disabled = false;
            btn.textContent = originalText;
        }
    });
})();
