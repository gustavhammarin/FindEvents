(function () {
    'use strict';

    // ─── Dropdown ──────────────────────────────────────────────────────────────

    function initDropdown(root) {
        var name = root.dataset.ddName;
        var placeholder = root.dataset.ddPlaceholder;
        var options = JSON.parse(root.dataset.ddOptions || '[]');
        var selected = JSON.parse(root.dataset.ddSelected || '[]');

        var toggle = root.querySelector('.dd-toggle');
        var span = toggle.querySelector('span');

        function renderHiddenInputs() {
            root.querySelectorAll('input[type=hidden]').forEach(function (el) { el.remove(); });
            selected.forEach(function (val) {
                var inp = document.createElement('input');
                inp.type = 'hidden';
                inp.name = name;
                inp.value = val;
                root.appendChild(inp);
            });
        }

        function updateToggle() {
            if (selected.length === 0) {
                span.textContent = placeholder;
                toggle.classList.remove('dd-active');
            } else {
                span.textContent = placeholder + ' (' + selected.length + ')';
                toggle.classList.add('dd-active');
            }
        }

        function buildMenu() {
            var menu = document.createElement('div');
            menu.className = 'dd-menu';
            // Keep-open multiselect: clicks inside must not reach the
            // document-level outside-click handler that closes all popups.
            menu.addEventListener('click', function (e) { e.stopPropagation(); });

            // Sheet header — only visible on mobile (bottom sheet mode)
            var head = document.createElement('div');
            head.className = 'dd-sheet-head';
            var title = document.createElement('span');
            title.className = 'dd-sheet-title';
            title.textContent = placeholder;
            var done = document.createElement('button');
            done.type = 'button';
            done.className = 'dd-sheet-done';
            done.textContent = 'Klar';
            done.addEventListener('click', closeDropdown);
            head.appendChild(title);
            head.appendChild(done);
            menu.appendChild(head);

            var list = document.createElement('div');
            list.setAttribute('role', 'listbox');
            list.setAttribute('aria-multiselectable', 'true');
            menu.appendChild(list);

            renderList(list);
            return menu;
        }

        // Multi-select: menu stays open, options re-render in place on each toggle
        function renderList(list) {
            list.innerHTML = '';

            if (selected.length > 0) {
                var resetBtn = document.createElement('button');
                resetBtn.type = 'button';
                resetBtn.className = 'dd-option dd-option-reset';
                resetBtn.setAttribute('role', 'option');
                resetBtn.textContent = 'Rensa val';
                resetBtn.addEventListener('click', function () {
                    selected = [];
                    renderHiddenInputs();
                    updateToggle();
                    renderList(list);
                    dispatchChange();
                });
                list.appendChild(resetBtn);

                var div = document.createElement('div');
                div.className = 'dd-divider';
                list.appendChild(div);
            }

            options.forEach(function (opt) {
                var isSel = selected.indexOf(opt) !== -1;
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'dd-option' + (isSel ? ' dd-option-selected' : '');
                btn.setAttribute('role', 'option');
                btn.setAttribute('aria-selected', String(isSel));

                var txtSpan = document.createElement('span');
                txtSpan.textContent = opt;
                btn.appendChild(txtSpan);

                if (isSel) {
                    btn.insertAdjacentHTML('beforeend',
                        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>');
                }

                btn.addEventListener('click', function () {
                    var idx = selected.indexOf(opt);
                    if (idx === -1) {
                        selected.push(opt);
                    } else {
                        selected.splice(idx, 1);
                    }
                    renderHiddenInputs();
                    updateToggle();
                    renderList(list);
                    dispatchChange();
                });

                list.appendChild(btn);
            });
        }

        function openDropdown() {
            closeAllDropdowns();
            root.classList.add('dd-open');
            document.documentElement.classList.add('scroll-locked');

            var backdrop = document.createElement('div');
            backdrop.className = 'dd-backdrop';
            backdrop.addEventListener('click', closeDropdown);
            root.insertBefore(backdrop, root.firstChild);

            root.appendChild(buildMenu());
        }

        function closeDropdown() {
            root.classList.remove('dd-open');
            document.documentElement.classList.remove('scroll-locked');
            var backdrop = root.querySelector('.dd-backdrop');
            if (backdrop) backdrop.remove();
            var menu = root.querySelector('.dd-menu');
            if (menu) menu.remove();
        }

        function dispatchChange() {
            var form = document.getElementById('filters');
            if (form) form.dispatchEvent(new Event('change', { bubbles: true }));
        }

        toggle.addEventListener('click', function (e) {
            e.stopPropagation();
            if (root.classList.contains('dd-open')) {
                closeDropdown();
            } else {
                openDropdown();
            }
        });

        root._closeDropdown = closeDropdown;
    }

    function closeAllDropdowns() {
        document.querySelectorAll('.dd-root.dd-open').forEach(function (r) {
            if (r._closeDropdown) r._closeDropdown();
        });
    }

    // ─── DatePicker ────────────────────────────────────────────────────────────

    var MONTHS_SV = ['januari','februari','mars','april','maj','juni','juli','augusti','september','oktober','november','december'];
    var DAYS_SV = ['M','T','O','T','F','L','S'];

    function initDatePicker(root) {
        var name = root.dataset.dpName;
        var selectedDate = root.dataset.dpValue || '';
        var viewDate = selectedDate ? new Date(selectedDate) : new Date();
        viewDate.setDate(1);

        var toggle = root.querySelector('.dp-toggle');
        var span = toggle.querySelector('span');
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        function formatDisplay(iso) {
            if (!iso) return 'Datum';
            var d = new Date(iso);
            var months = ['jan','feb','mar','apr','maj','jun','jul','aug','sep','okt','nov','dec'];
            return d.getDate() + ' ' + months[d.getMonth()] + ' ' + d.getFullYear();
        }

        function updateToggle() {
            span.textContent = formatDisplay(selectedDate);
            if (selectedDate) {
                toggle.classList.add('dp-active');
            } else {
                toggle.classList.remove('dp-active');
            }
        }

        function renderHiddenInput() {
            var existing = root.querySelector('input[type=hidden]');
            if (existing) existing.remove();
            if (selectedDate) {
                var inp = document.createElement('input');
                inp.type = 'hidden';
                inp.name = name;
                inp.value = selectedDate;
                root.appendChild(inp);
            }
            var clearBtn = root.querySelector('.dp-clear');
            if (clearBtn) clearBtn.style.display = selectedDate ? '' : 'none';
        }

        function buildCalendar() {
            var popup = document.createElement('div');
            popup.className = 'dp-popup';

            var year = viewDate.getFullYear();
            var month = viewDate.getMonth();
            var monthLabel = MONTHS_SV[month] + ' ' + year;

            popup.innerHTML =
                '<div class="dd-sheet-head">' +
                    '<span class="dd-sheet-title">Datum</span>' +
                    '<button type="button" class="dd-sheet-done">Klar</button>' +
                '</div>' +
                '<div class="dp-header">' +
                    '<button type="button" class="dp-nav dp-prev" aria-label="Föregående månad"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="m15 18-6-6 6-6"/></svg></button>' +
                    '<span class="dp-month-label">' + monthLabel + '</span>' +
                    '<button type="button" class="dp-nav dp-next" aria-label="Nästa månad"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="m9 18 6-6-6-6"/></svg></button>' +
                '</div>' +
                '<div class="dp-weekdays">' + DAYS_SV.map(function (d) { return '<span>' + d + '</span>'; }).join('') + '</div>' +
                '<div class="dp-days"></div>';

            popup.addEventListener('click', function (e) { e.stopPropagation(); });

            popup.querySelector('.dd-sheet-done').addEventListener('click', closeDatePicker);
            popup.querySelector('.dp-prev').addEventListener('click', function () {
                viewDate.setMonth(viewDate.getMonth() - 1);
                rebuildCalendar(popup);
            });
            popup.querySelector('.dp-next').addEventListener('click', function () {
                viewDate.setMonth(viewDate.getMonth() + 1);
                rebuildCalendar(popup);
            });

            fillDays(popup);
            return popup;
        }

        function fillDays(popup) {
            var year = viewDate.getFullYear();
            var month = viewDate.getMonth();
            var firstDay = new Date(year, month, 1);
            var offset = (firstDay.getDay() + 6) % 7;
            var daysInMonth = new Date(year, month + 1, 0).getDate();
            var grid = popup.querySelector('.dp-days');
            grid.innerHTML = '';

            for (var i = 0; i < offset; i++) {
                var empty = document.createElement('span');
                empty.className = 'dp-day dp-empty';
                grid.appendChild(empty);
            }

            for (var d = 1; d <= daysInMonth; d++) {
                var date = new Date(year, month, d);
                date.setHours(0, 0, 0, 0);
                var isPast = date < today;
                var iso = year + '-' + String(month + 1).padStart(2, '0') + '-' + String(d).padStart(2, '0');
                var isSel = iso === selectedDate;
                var isToday = date.getTime() === today.getTime();

                var cls = 'dp-day';
                if (isPast) cls += ' dp-past';
                if (isSel) cls += ' dp-selected';
                else if (isToday) cls += ' dp-today';

                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = cls;
                btn.textContent = String(d);
                if (isPast) {
                    btn.disabled = true;
                } else {
                    (function (isoVal) {
                        btn.addEventListener('click', function () {
                            selectedDate = isoVal;
                            renderHiddenInput();
                            updateToggle();
                            closeDatePicker();
                            dispatchChange();
                        });
                    }(iso));
                }
                grid.appendChild(btn);
            }
        }

        function rebuildCalendar(popup) {
            popup.querySelector('.dp-month-label').textContent =
                MONTHS_SV[viewDate.getMonth()] + ' ' + viewDate.getFullYear();
            fillDays(popup);
        }

        function openDatePicker() {
            closeAllDropdowns();
            root.classList.add('dp-open');
            document.documentElement.classList.add('scroll-locked');

            var backdrop = document.createElement('div');
            backdrop.className = 'dp-backdrop';
            backdrop.addEventListener('click', closeDatePicker);
            root.appendChild(backdrop);

            root.appendChild(buildCalendar());
        }

        function closeDatePicker() {
            root.classList.remove('dp-open');
            document.documentElement.classList.remove('scroll-locked');
            var backdrop = root.querySelector('.dp-backdrop');
            if (backdrop) backdrop.remove();
            var popup = root.querySelector('.dp-popup');
            if (popup) popup.remove();
        }

        function dispatchChange() {
            var form = document.getElementById('filters');
            if (form) form.dispatchEvent(new Event('change', { bubbles: true }));
        }

        toggle.addEventListener('click', function (e) {
            var clearBtn = root.querySelector('.dp-clear');
            if (clearBtn && clearBtn.contains(e.target)) {
                e.stopPropagation();
                selectedDate = '';
                renderHiddenInput();
                updateToggle();
                closeDatePicker();
                dispatchChange();
                return;
            }
            e.stopPropagation();
            if (root.classList.contains('dp-open')) {
                closeDatePicker();
            } else {
                openDatePicker();
            }
        });

        root._closeDatePicker = closeDatePicker;
    }

    // ─── Clear all filters ─────────────────────────────────────────────────────

    function initClearButton() {
        var btn = document.getElementById('filter-clear');
        if (!btn) return;
        btn.addEventListener('click', function () {
            var form = document.getElementById('filters');
            if (!form) return;

            form.querySelector('[name=q]').value = '';

            document.querySelectorAll('.dd-root').forEach(function (root) {
                root.dataset.ddSelected = '[]';
                root.querySelectorAll('input[type=hidden]').forEach(function (el) { el.remove(); });
                var toggle = root.querySelector('.dd-toggle');
                toggle.classList.remove('dd-active');
                var span = toggle.querySelector('span');
                span.textContent = root.dataset.ddPlaceholder;
            });

            document.querySelectorAll('.dp-root').forEach(function (root) {
                root.dataset.dpValue = '';
                root.querySelectorAll('input[type=hidden]').forEach(function (el) { el.remove(); });
                var toggle = root.querySelector('.dp-toggle');
                toggle.classList.remove('dp-active');
                var span = toggle.querySelector('span');
                if (span) span.textContent = 'Datum';
                var clearSpan = root.querySelector('.dp-clear');
                if (clearSpan) clearSpan.style.display = 'none';
            });

            btn.classList.add('hidden');
            form.dispatchEvent(new Event('change', { bubbles: true }));
        });
    }

    // ─── URL sync ──────────────────────────────────────────────────────────────

    function syncUrl() {
        var form = document.getElementById('filters');
        if (!form) return;

        var params = new URLSearchParams();
        var q = (form.querySelector('[name=q]') || {}).value || '';
        if (q) params.set('q', q);

        form.querySelectorAll('input[type=hidden][name=plats]').forEach(function (el) {
            if (el.value) params.append('plats', el.value);
        });
        form.querySelectorAll('input[type=hidden][name=cat]').forEach(function (el) {
            if (el.value) params.append('cat', el.value);
        });
        var datum = form.querySelector('input[type=hidden][name=datum]');
        if (datum && datum.value) params.set('datum', datum.value);

        // Keep "load more" state in the URL so returning from a detail page
        // re-renders the same number of cards (scroll position depends on it).
        var grid = document.getElementById('events-grid');
        var take = grid ? parseInt(grid.dataset.take || '32', 10) : 32;
        if (take > 32) params.set('ta', String(take));

        var qs = params.toString();
        history.replaceState(null, '', '/evenemang' + (qs ? '?' + qs : ''));
    }

    // Update clear button visibility after any HTMX-driven filter change
    function updateClearButton() {
        var form = document.getElementById('filters');
        var btn = document.getElementById('filter-clear');
        if (!form || !btn) return;

        var q = (form.querySelector('[name=q]') || {}).value || '';
        var hasPlats = form.querySelector('input[type=hidden][name=plats]');
        var hasCat = form.querySelector('input[type=hidden][name=cat]');
        var hasDatum = form.querySelector('input[type=hidden][name=datum]');

        if (q || hasPlats || hasCat || hasDatum) {
            btn.classList.remove('hidden');
        } else {
            btn.classList.add('hidden');
        }
    }

    // ─── List state: remember where the user was, restore on return ───────────

    // Card click (list page): remember scroll + full list URL (filters, ta).
    // Delegated so it survives HTMX re-renders of the grid.
    function initListStateTracking() {
        document.addEventListener('click', function (e) {
            if (e.target.closest && e.target.closest('.event-card')) {
                sessionStorage.setItem('eventsScroll', String(window.scrollY));
                sessionStorage.setItem('eventsUrl', location.href);
            }
        });
    }

    // Detail page: "Alla evenemang" goes back to the exact list view the user
    // left, no matter how many detail pages deep they've navigated.
    function initBackToList() {
        document.querySelectorAll('.detail-back').forEach(function (a) {
            a.addEventListener('click', function (e) {
                var url = sessionStorage.getItem('eventsUrl');
                if (url) {
                    e.preventDefault();
                    location.href = url;
                }
            });
        });
    }

    function restoreScroll() {
        // Only on the list page — consuming the key on a detail page would
        // both scroll the wrong page and lose the position for the real return.
        if (!document.getElementById('filters')) return;

        var y = sessionStorage.getItem('eventsScroll');
        if (y) {
            sessionStorage.removeItem('eventsScroll');
            requestAnimationFrame(function () {
                window.scrollTo({ top: +y, behavior: 'instant' });
            });
        }
    }

    // ─── Init ──────────────────────────────────────────────────────────────────

    function init() {
        document.querySelectorAll('.dd-root[data-dd-name]').forEach(initDropdown);
        document.querySelectorAll('.dp-root[data-dp-name]').forEach(initDatePicker);
        initClearButton();
        initListStateTracking();
        initBackToList();
        restoreScroll();

        document.addEventListener('htmx:afterSettle', function (evt) {
            if (evt.detail.target && evt.detail.target.id === 'events-container') {
                syncUrl();
                updateClearButton();
            }
        });

        // Close popups on outside click
        document.addEventListener('click', function () {
            closeAllDropdowns();
            document.querySelectorAll('.dp-root.dp-open').forEach(function (r) {
                if (r._closeDatePicker) r._closeDatePicker();
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
}());
