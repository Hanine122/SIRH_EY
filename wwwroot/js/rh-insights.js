(function () {
    'use strict';

    const API_ENDPOINT = '/api/rhinsights/matching/';
    const spinner = document.getElementById('rh-spinner');
    const skeleton = document.getElementById('rh-skeleton');
    const emptyState = document.getElementById('rh-empty-state');
    const detailTarget = document.getElementById('insight-detail-target');
    const alertCards = document.querySelectorAll('.rh-alert-card');
    const promotionEndpoint = '/api/rhinsights/promotion-readiness';
    const workforceImpactEndpoint = '/api/rhinsights/workforce-impact';
    const drawer = document.getElementById('tic-detail-drawer');
    const drawerTitle = document.getElementById('tic-drawer-title');

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showSpinner() {
        openDrawer('Analyse de succession');
        if (spinner) spinner.classList.add('active');
        if (skeleton) skeleton.classList.add('active');
        if (emptyState) emptyState.style.display = 'none';
        if (detailTarget) detailTarget.style.display = 'none';
    }

    function hideSpinner() {
        if (spinner) spinner.classList.remove('active');
        if (skeleton) skeleton.classList.remove('active');
    }

    function showDetail() {
        if (detailTarget) detailTarget.style.display = 'block';
    }

    function openDrawer(title) {
        if (drawerTitle && title) drawerTitle.textContent = title;
        if (drawer) {
            drawer.classList.add('open');
            drawer.setAttribute('aria-hidden', 'false');
        }
    }

    function closeDrawer() {
        if (drawer) {
            drawer.classList.remove('open');
            drawer.setAttribute('aria-hidden', 'true');
        }
    }

    function setActiveCard(card) {
        alertCards.forEach(c => c.classList.remove('active'));
        if (card) card.classList.add('active');
    }

    function renderPartantInfo(partant) {
        const statutClass = partant.statut === 2 ? 'rh-badge-passation' : 'rh-badge-vacant';
        const statutLabel = partant.statut === 2 ? 'En Passation' : 'Vacant';
        
        return `
            <div class="p-4 border-bottom bg-light">
                <h4 class="mb-2">
                    <i class="fas fa-chart-line me-2"></i>
                    Analyse de succession pour le poste de ${escapeHtml(partant.poste)} - ${escapeHtml(partant.departement)}
                </h4>
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <h6 class="mb-1">${escapeHtml(partant.prenom)} ${escapeHtml(partant.nom)}</h6>
                        <span class="${statutClass}">${statutLabel}</span>
                    </div>
                    <div>
                        <small class="fw-semibold">Compétences requises (${partant.competencesRequises.length}) :</small>
                        <div class="mt-1">
                            ${partant.competencesRequises.map(c => 
                                `<span class="badge bg-secondary me-1 mb-1">${escapeHtml(c)}</span>`
                            ).join('')}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    function renderCandidateCard(candidat, index) {
        const transversalBadge = candidat.profilTransversal 
            ? `<span class="rh-transversal-badge ms-2">Transversal</span>` 
            : '';
        
        const scoreClass = candidat.scoreMatching >= 75 ? '' 
            : candidat.scoreMatching >= 50 ? 'medium' 
            : 'low';

        return `
            <div class="rh-candidate-card">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <h6 class="mb-1 fw-semibold">
                            ${index + 1}. ${escapeHtml(candidat.prenom)} ${escapeHtml(candidat.nom)}
                            ${transversalBadge}
                        </h6>
                        <p class="text-muted mb-1">${escapeHtml(candidat.poste)}</p>
                        <small class="text-muted">
                            <i class="fas fa-building"></i> ${escapeHtml(candidat.departement)} · 
                            <i class="fas fa-user-graduate"></i> ${escapeHtml(candidat.grade)}
                        </small>
                    </div>
                    <div class="text-end">
                        <div class="rh-score-badge ${scoreClass}">
                            ${candidat.scoreMatching}%
                        </div>
                        <small class="text-muted">${candidat.nbCompetencesCommunes}/${candidat.competencesPossedees.length + candidat.competencesManquantes.length} compétences</small>
                    </div>
                </div>

                <!-- Plan de formation recommandé -->
                ${candidat.formationsRecommandees.length > 0 ? `
                    <div class="mt-3">
                        <small class="fw-semibold">Feuille de Route de Transition :</small>
                        <div class="mt-2">
                            ${candidat.formationsRecommandees.map(f => `
                                <div class="rh-formation-item">
                                    <i class="fas fa-graduation-cap me-2"></i>Pour ${escapeHtml(candidat.prenom)} ${escapeHtml(candidat.nom)} : Suivre le cours ${escapeHtml(f)}
                                </div>
                            `).join('')}
                        </div>
                    </div>
                ` : ''}
            </div>
        `;
    }

    function renderDetail(data) {
        const partantHtml = renderPartantInfo(data.partant);
        
        const top3Candidats = data.candidats.slice(0, 3);
        const candidatsHtml = top3Candidats.map((c, i) => renderCandidateCard(c, i)).join('');

        // Cross-comparison table
        const allCompetences = data.partant.competencesRequises;
        const comparisonTableHtml = `
            <div class="p-4 border-top">
                <h5 class="mb-3">
                    <i class="fas fa-table me-2"></i>Tableau Comparatif Croisé
                </h5>
                <div class="table-responsive">
                    <table class="table table-bordered table-sm">
                        <thead class="table-light">
                            <tr>
                                <th>Compétence Requise</th>
                                ${top3Candidats.map(c => `
                                    <th class="text-center">
                                        ${escapeHtml(c.prenom)} ${escapeHtml(c.nom)}
                                        <br>
                                        <small class="text-muted">${c.scoreMatching}%</small>
                                    </th>
                                `).join('')}
                            </tr>
                        </thead>
                        <tbody>
                            ${allCompetences.map(comp => `
                                <tr>
                                    <td class="fw-semibold">${escapeHtml(comp)}</td>
                                    ${top3Candidats.map(c => {
                                        const hasCompetence = c.competencesPossedees.some(p => p.toLowerCase() === comp.toLowerCase());
                                        return `
                                            <td class="text-center">
                                                ${hasCompetence 
                                                    ? '<span class="text-success fw-bold">✔️ OK</span>' 
                                                    : '<span class="text-danger fw-bold">❌ À combler</span>'}
                                            </td>
                                        `;
                                    }).join('')}
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>
        `;

        const html = `
            ${partantHtml}
            <div class="p-4">
                <h5 class="mb-3">
                    <i class="fas fa-users me-2"></i>Top ${top3Candidats.length} Remplaçants Potentiels
                </h5>
                ${candidatsHtml}
            </div>
            ${comparisonTableHtml}
        `;

        detailTarget.innerHTML = html;
    }

    async function loadMatchingData(collaborateurId) {
        showSpinner();
        
        try {
            const response = await fetch(`${API_ENDPOINT}${collaborateurId}`, {
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`Erreur HTTP: ${response.status}`);
            }

            const data = await response.json();
            renderDetail(data);
            showDetail();
        } catch (error) {
            console.error('Erreur lors du chargement des données:', error);
            detailTarget.innerHTML = `
                <div class="p-4 text-center text-danger">
                    <i class="fas fa-exclamation-triangle fa-2x mb-2"></i>
                    <h6 class="mb-2">Erreur lors du chargement des données</h6>
                    <p class="text-muted mb-0">Impossible de récupérer les informations de matching. Veuillez réessayer.</p>
                    <small class="text-muted">${escapeHtml(error.message)}</small>
                    <button class="btn btn-outline-danger btn-sm mt-3" onclick="location.reload()">
                        <i class="fas fa-redo me-2"></i>Réessayer
                    </button>
                </div>
            `;
            showDetail();
        } finally {
            hideSpinner();
        }
    }

    function init() {
        initModuleNavigation();
        initDetailDrawer();
        initKpiCounters();
        initPromotionSimulator();
        initWorkforceImpactSimulator();

        alertCards.forEach(card => {
            card.addEventListener('click', function() {
                const collaborateurId = this.getAttribute('data-id');
                if (collaborateurId) {
                    setActiveCard(this);
                    loadMatchingData(collaborateurId);
                }
            });
        });
    }

    function initModuleNavigation() {
        const navItems = document.querySelectorAll('[data-tic-module]');
        const panels = document.querySelectorAll('[data-tic-module-panel]');
        const shortcuts = document.querySelectorAll('[data-tic-jump]');

        function activate(moduleName) {
            navItems.forEach(item => item.classList.toggle('active', item.getAttribute('data-tic-module') === moduleName));
            panels.forEach(panel => panel.classList.toggle('active', panel.getAttribute('data-tic-module-panel') === moduleName));
        }

        navItems.forEach(item => {
            item.addEventListener('click', function () {
                activate(this.getAttribute('data-tic-module'));
            });
        });

        shortcuts.forEach(item => {
            item.addEventListener('click', function () {
                activate(this.getAttribute('data-tic-jump'));
            });
        });
    }

    function initDetailDrawer() {
        document.querySelectorAll('[data-tic-close-drawer]').forEach(button => {
            button.addEventListener('click', closeDrawer);
        });

        document.querySelectorAll('.tic-open-detail').forEach(card => {
            card.addEventListener('click', function () {
                const title = this.getAttribute('data-detail-title') || 'Insight details';
                const body = this.getAttribute('data-detail-body') || 'Aucune information complementaire disponible.';
                openDrawer(title);
                if (emptyState) emptyState.style.display = 'none';
                if (detailTarget) {
                    detailTarget.style.display = 'block';
                    detailTarget.innerHTML = `
                        <div class="p-4">
                            <div class="tic-mini-label mb-2">Executive detail</div>
                            <h4 class="fw-bold mb-3">${escapeHtml(title)}</h4>
                            <div class="tic-recommendation">${escapeHtml(body)}</div>
                        </div>
                    `;
                }
            });
        });
    }

    function initPromotionSimulator() {
        const collabSearch = document.getElementById('promotion-collab-search');
        const collabSelect = document.getElementById('promotion-collab-select');
        const targetSelect = document.getElementById('promotion-target-select');
        const runButton = document.getElementById('promotion-run-btn');

        if (!collabSelect || !targetSelect || !runButton) return;

        if (collabSearch) {
            collabSearch.addEventListener('input', function () {
                const query = this.value.trim().toLowerCase();
                Array.from(collabSelect.options).forEach(option => {
                    const haystack = option.getAttribute('data-search') || option.textContent.toLowerCase();
                    option.hidden = query.length > 0 && !haystack.includes(query);
                });

                const visibleOption = Array.from(collabSelect.options).find(option => !option.hidden);
                if (visibleOption && collabSelect.selectedOptions[0]?.hidden) {
                    collabSelect.value = visibleOption.value;
                }
            });
        }

        runButton.addEventListener('click', function () {
            loadPromotionReadiness(collabSelect.value, targetSelect.value);
        });

        collabSelect.addEventListener('change', function () {
            loadPromotionReadiness(collabSelect.value, targetSelect.value);
        });

        targetSelect.addEventListener('change', function () {
            loadPromotionReadiness(collabSelect.value, targetSelect.value);
        });
    }

    function initWorkforceImpactSimulator() {
        const collabSearch = document.getElementById('impact-collab-search');
        const collabSelect = document.getElementById('impact-collab-select');
        const runButton = document.getElementById('impact-run-btn');

        if (!collabSelect || !runButton) return;

        if (collabSearch) {
            collabSearch.addEventListener('input', function () {
                filterSelectOptions(collabSelect, this.value);
            });
        }

        runButton.addEventListener('click', function () {
            loadWorkforceImpact(collabSelect.value);
        });

        collabSelect.addEventListener('change', function () {
            loadWorkforceImpact(collabSelect.value);
        });
    }

    function filterSelectOptions(select, queryText) {
        const query = (queryText || '').trim().toLowerCase();
        Array.from(select.options).forEach(option => {
            const haystack = option.getAttribute('data-search') || option.textContent.toLowerCase();
            option.hidden = query.length > 0 && !haystack.includes(query);
        });

        const visibleOption = Array.from(select.options).find(option => !option.hidden);
        if (visibleOption && select.selectedOptions[0]?.hidden) {
            select.value = visibleOption.value;
        }
    }

    async function loadWorkforceImpact(collaborateurId) {
        const runButton = document.getElementById('impact-run-btn');
        if (!collaborateurId) return;

        setImpactLoading(true, runButton);

        try {
            const response = await fetch(workforceImpactEndpoint, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ collaborateurId: Number(collaborateurId) })
            });

            if (!response.ok) {
                throw new Error(`Erreur HTTP: ${response.status}`);
            }

            renderWorkforceImpact(await response.json());
        } catch (error) {
            console.error('Erreur simulation workforce impact:', error);
            setText('impact-executive-insight', 'Impossible de charger la simulation organisationnelle. Verifiez les donnees et reessayez.');
        } finally {
            setImpactLoading(false, runButton);
        }
    }

    function setImpactLoading(isLoading, button) {
        if (!button) return;
        button.disabled = isLoading;
        button.innerHTML = isLoading
            ? '<span class="spinner-border spinner-border-sm me-2"></span>Simulation...'
            : '<i class="fas fa-random me-2"></i>Simuler l\'impact';
    }

    function renderWorkforceImpact(data) {
        setText('impact-executive-insight', data.executiveInsight);
        setText('impact-continuity', `${Math.round(data.continuityRisk)}%`);
        setText('impact-operational', `${Math.round(data.operationalImpact)}%`);
        setText('impact-fragility', `${Math.round(data.departmentFragility)}%`);
        setText('impact-dependency', `${Math.round(data.strategicDependencyScore)}%`);
        setText('impact-risk-level', data.riskLevel);
        setText('impact-role', `${data.collaborateurNom} - ${data.role}`);

        setBarWidth('impact-continuity-bar', data.continuityRisk);
        setBarWidth('impact-operational-bar', data.operationalImpact);
        setBarWidth('impact-fragility-bar', data.departmentFragility);

        renderImpactDepartments(data.departmentExposure || []);
        renderBadgeList('impact-skills-lost', data.competenciesLost || []);
        renderImpactSuccessors(data);
        renderImpactActions(data.recommendedActions || []);
    }

    function setBarWidth(id, value) {
        const element = document.getElementById(id);
        if (element) element.style.width = `${Math.max(3, Math.min(100, value || 0))}%`;
    }

    function renderImpactDepartments(departments) {
        const target = document.getElementById('impact-departments');
        if (!target) return;

        const items = departments.length
            ? departments
            : [{ department: 'RH', impactedCollaborators: 1, exposureScore: 58, signal: 'Dependance a qualifier' }];

        target.innerHTML = items.map(dept => `
            <div class="mb-3">
                <div class="d-flex justify-content-between small fw-semibold">
                    <span>${escapeHtml(dept.department)}</span>
                    <span>${Math.round(dept.exposureScore)}%</span>
                </div>
                <div class="impact-meter my-1"><div class="impact-meter-fill" style="width:${Math.max(3, Math.min(100, dept.exposureScore))}%"></div></div>
                <div class="small tic-soft-text">${dept.impactedCollaborators} collaborateur(s) impactes | ${escapeHtml(dept.signal)}</div>
            </div>
        `).join('');
    }

    function renderImpactSuccessors(data) {
        const target = document.getElementById('impact-successors');
        if (!target) return;

        const successors = [
            ...(data.immediateSuccessors || []),
            ...(data.partialSuccessors || []),
            ...(data.highPotentialAlternatives || [])
        ].slice(0, 6);

        if (!successors.length) {
            target.innerHTML = `
                <div class="col-12">
                    <div class="promotion-empty-note">Aucun successeur immediat detecte. Lancer une revue de mobilite et un plan de formation urgent.</div>
                </div>
            `;
            return;
        }

        target.innerHTML = successors.map(successor => `
            <div class="col-md-6">
                <div class="successor-card">
                    <div class="d-flex justify-content-between">
                        <div>
                            <div class="fw-bold">${escapeHtml(successor.nomComplet)}</div>
                            <div class="small tic-soft-text">${escapeHtml(successor.poste)} | ${escapeHtml(successor.departement)}</div>
                        </div>
                        <span class="badge bg-dark">${Math.round(successor.readinessScore)}%</span>
                    </div>
                    <div class="small mt-2">${escapeHtml(successor.successorType)} | ${successor.sharedCompetencies} competences communes</div>
                </div>
            </div>
        `).join('');
    }

    function renderImpactActions(actions) {
        const target = document.getElementById('impact-actions');
        if (!target) return;

        const items = actions.length
            ? actions
            : [{ title: 'Renforcer la succession', description: 'Qualifier deux relais internes et documenter les savoirs critiques.', priority: 'High' }];

        target.innerHTML = items.map(action => `
            <div class="tic-recommendation mb-2">
                <div class="d-flex justify-content-between gap-2">
                    <strong>${escapeHtml(action.title)}</strong>
                    <span class="badge bg-light text-dark border">${escapeHtml(action.priority)}</span>
                </div>
                <div class="small">${escapeHtml(action.description)}</div>
            </div>
        `).join('');
    }

    async function loadPromotionReadiness(collaborateurId, targetKey) {
        const runButton = document.getElementById('promotion-run-btn');
        if (!collaborateurId) return;

        setPromotionLoading(true, runButton);

        try {
            const response = await fetch(promotionEndpoint, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    collaborateurId: Number(collaborateurId),
                    targetKey: targetKey || ''
                })
            });

            if (!response.ok) {
                throw new Error(`Erreur HTTP: ${response.status}`);
            }

            renderPromotionReadiness(await response.json());
        } catch (error) {
            console.error('Erreur simulation promotion:', error);
            const summary = document.getElementById('promotion-summary');
            if (summary) {
                summary.innerHTML = `
                    <div class="tic-mini-label text-white-50 mb-2">Executive AI summary</div>
                    <div>Impossible de charger la simulation pour le moment. Verifiez les donnees collaborateur et reessayez.</div>
                `;
            }
        } finally {
            setPromotionLoading(false, runButton);
        }
    }

    function setPromotionLoading(isLoading, button) {
        if (!button) return;
        button.disabled = isLoading;
        button.innerHTML = isLoading
            ? '<span class="spinner-border spinner-border-sm me-2"></span>Simulation...'
            : '<i class="fas fa-play me-2"></i>Simuler la readiness';
    }

    function renderPromotionReadiness(data) {
        setText('promotion-readiness-value', `${Math.round(data.readinessPercentage)}%`);
        setText('promotion-person', data.collaborateurNom);
        setText('promotion-role', `${data.currentRole} -> ${data.targetRole}`);
        setText('promotion-compatibility', `${Math.round(data.compatibilityScore)}%`);
        setText('promotion-potential', `${Math.round(data.promotionPotential)}%`);
        setText('promotion-time', `${data.estimatedMonthsMin}-${data.estimatedMonthsMax}m`);

        const gauge = document.getElementById('promotion-gauge');
        if (gauge) {
            gauge.style.setProperty('--readiness', `${Math.max(0, Math.min(100, data.readinessPercentage))}%`);
        }

        const summary = document.getElementById('promotion-summary');
        if (summary) {
            summary.innerHTML = `
                <div class="tic-mini-label text-white-50 mb-2">Executive AI summary</div>
                <div>${escapeHtml(data.executiveSummary)}</div>
            `;
        }

        renderBadgeList('promotion-transversal', data.transversalSkills);
        renderBadgeList('promotion-leadership', data.leadershipIndicators);
        renderGapMatrix(data.missingCompetencies || []);
        renderFormationRecommendations(data.recommendedFormations || []);
    }

    function renderBadgeList(containerId, items) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const values = items && items.length ? items : ['Signal a qualifier'];
        container.innerHTML = values
            .map(item => `<span class="badge bg-light text-dark border me-1 mb-1">${escapeHtml(item)}</span>`)
            .join('');
    }

    function renderGapMatrix(gaps) {
        const target = document.getElementById('promotion-gap-matrix');
        if (!target) return;

        if (!gaps.length) {
            target.innerHTML = `
                <tr>
                    <td colspan="5">
                        <div class="promotion-empty-note">Aucun gap critique detecte. Une validation manager reste recommandee avant promotion.</div>
                    </td>
                </tr>
            `;
            return;
        }

        target.innerHTML = gaps.map(gap => {
            const badge = gap.severity === 'High'
                ? 'bg-danger'
                : gap.severity === 'Medium'
                    ? 'bg-warning text-dark'
                    : gap.severity === 'Low'
                        ? 'bg-info text-dark'
                        : 'bg-success';

            return `
                <tr>
                    <td class="fw-semibold">${escapeHtml(gap.competence)}</td>
                    <td class="text-center">${gap.currentLevel}/5</td>
                    <td class="text-center">${gap.requiredLevel}/5</td>
                    <td class="text-center">${gap.gap}</td>
                    <td><span class="badge ${badge}">${escapeHtml(gap.priorityLabel)}</span></td>
                </tr>
            `;
        }).join('');
    }

    function renderFormationRecommendations(formations) {
        const target = document.getElementById('promotion-formations');
        if (!target) return;

        const items = formations.length
            ? formations
            : [{ formationTitre: 'Mentoring manager avance', targetCompetence: 'Leadership', readinessGain: 8, estimatedWeeks: 4, progressionImpact: 'Consolidation avant promotion' }];

        target.innerHTML = items.map(formation => `
            <div class="tic-recommendation mb-2">
                <div class="fw-bold">${escapeHtml(formation.formationTitre)}</div>
                <div class="small tic-soft-text">${escapeHtml(formation.targetCompetence)} | +${formation.readinessGain}% readiness | ${formation.estimatedWeeks} semaine(s)</div>
                <div class="small">${escapeHtml(formation.progressionImpact)}</div>
            </div>
        `).join('');
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) element.textContent = value || '';
    }

    function initKpiCounters() {
        const counters = document.querySelectorAll('.js-kpi-counter[data-counter]');
        counters.forEach(counter => {
            const target = Number(counter.getAttribute('data-counter'));
            if (!Number.isFinite(target)) return;

            const suffix = counter.getAttribute('data-suffix') || '';
            const duration = 850;
            const start = performance.now();

            function tick(now) {
                const progress = Math.min(1, (now - start) / duration);
                const eased = 1 - Math.pow(1 - progress, 3);
                const value = Math.round(target * eased);
                counter.textContent = `${value}${suffix}`;

                if (progress < 1) {
                    requestAnimationFrame(tick);
                } else {
                    counter.textContent = `${Math.round(target)}${suffix}`;
                }
            }

            requestAnimationFrame(tick);
        });
    }

    document.addEventListener('DOMContentLoaded', init);
})();
