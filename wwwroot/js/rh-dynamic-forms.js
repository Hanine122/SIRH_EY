(function () {
    const endpoints = {
        competencesParGrade: "/Competences/GetCompetencesParGrade",
        postesParDepartement: "/Collaborateurs/GetPostesParDepartement"
    };

    function option(value, label) {
        const element = document.createElement("option");
        element.value = value || "";
        element.textContent = label || value || "";
        return element;
    }

    function checkboxId(value, index) {
        return `comp_${index}_${String(value || "").replace(/[^a-z0-9]+/gi, "_")}`;
    }

    async function getJson(url, params) {
        const query = new URLSearchParams(params);
        const response = await fetch(`${url}?${query}`, {
            headers: { "Accept": "application/json" }
        });

        if (!response.ok) {
            throw new Error(`Request failed: ${response.status}`);
        }

        return response.json();
    }

    function syncHiddenCompetenceName() {
        const hiddenName = document.getElementById("Nom");
        if (!hiddenName) return;

        const firstChecked = document.querySelector(".competence-choice:checked");
        hiddenName.value = firstChecked ? firstChecked.value : "";
    }

    function renderCompetenceChoices(items) {
        const container = document.getElementById("competenceCheckboxList");
        const count = document.getElementById("competenceCount");
        const empty = document.getElementById("emptyCompetenceMessage");
        if (!container) return;

        container.innerHTML = "";
        if (count) count.textContent = `${items.length} item(s)`;
        if (empty) empty.style.display = items.length ? "none" : "";

        items.forEach((item, index) => {
            const id = checkboxId(item.nom, index);
            const col = document.createElement("div");
            col.className = "col-md-6 col-xl-4";
            col.innerHTML = `
                <input class="btn-check competence-choice" type="checkbox" name="selectedCompetences" value="${escapeHtml(item.nom)}" id="${id}" autocomplete="off" />
                <label class="btn competence-check-card w-100 text-start" for="${id}">
                    <span class="fw-semibold d-block">${escapeHtml(item.nom)}</span>
                    <span class="small text-muted">${escapeHtml(item.poste || "Referentiel")} - niveau requis ${item.niveauRequis || 0}/5</span>
                </label>`;
            container.appendChild(col);
        });

        container.querySelectorAll(".competence-choice").forEach(input => {
            input.addEventListener("change", syncHiddenCompetenceName);
        });
        syncHiddenCompetenceName();
    }

    function escapeHtml(value) {
        return String(value || "").replace(/[&<>"']/g, char => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            '"': "&quot;",
            "'": "&#039;"
        }[char]));
    }

    function initGradeCompetences() {
        const gradeSelect = document.getElementById("gradeSelect");
        if (!gradeSelect) return;

        gradeSelect.addEventListener("change", async () => {
            try {
                const items = await getJson(endpoints.competencesParGrade, { grade: gradeSelect.value });
                renderCompetenceChoices(items);
            } catch {
                renderCompetenceChoices([]);
            }
        });

        document.querySelectorAll(".competence-choice").forEach(input => {
            input.addEventListener("change", syncHiddenCompetenceName);
        });
    }

    function initDepartementPostes() {
        document.querySelectorAll(".js-departement-select").forEach(select => {
            select.addEventListener("change", async () => {
                const targetSelector = select.dataset.posteTarget || "#Poste";
                const posteSelect = document.querySelector(targetSelector);
                if (!posteSelect) return;

                posteSelect.innerHTML = "";
                posteSelect.appendChild(option("", "Chargement..."));

                try {
                    const postes = await getJson(endpoints.postesParDepartement, { departement: select.value });
                    posteSelect.innerHTML = "";
                    posteSelect.appendChild(option("", postes.length ? "Selectionnez un poste" : "Aucun poste disponible"));
                    postes.forEach(poste => posteSelect.appendChild(option(poste.value, poste.label)));
                } catch {
                    posteSelect.innerHTML = "";
                    posteSelect.appendChild(option("", "Erreur de chargement"));
                }
            });
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        initGradeCompetences();
        initDepartementPostes();
    });
})();
