/* ============ ShopSphere - site.js ============ */

// ---------- Toasts ----------
function showToast(message, type) {
    type = type || 'success';
    var icon = type === 'success' ? 'bi-check-circle-fill' : (type === 'danger' ? 'bi-x-circle-fill' : 'bi-info-circle-fill');
    var el = document.createElement('div');
    el.className = 'toast align-items-center text-bg-' + type + ' border-0';
    el.setAttribute('role', 'alert');
    el.innerHTML = '<div class="d-flex"><div class="toast-body"><i class="bi ' + icon + ' me-2"></i></div>' +
        '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
    el.querySelector('.toast-body').appendChild(document.createTextNode(message));
    document.getElementById('toastContainer').appendChild(el);
    new bootstrap.Toast(el, { delay: 3800 }).show();
    el.addEventListener('hidden.bs.toast', function () { el.remove(); });
}

// ---------- AJAX helper (form-encoded POST with anti-forgery header) ----------
function getToken() {
    var input = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
}

async function post(url, data) {
    var res;
    try {
        res = await fetch(url, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getToken(),
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: new URLSearchParams(data || {})
        });
    } catch (e) {
        return { success: false, message: 'Network error. Please try again.' };
    }

    // the server redirected us to the login page
    if (res.redirected && res.url.indexOf('/Account/Login') >= 0) {
        window.location = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
        return { success: false, message: 'Please login first.', redirected: true };
    }
    if (!res.ok) return { success: false, message: 'Something went wrong. Please try again.' };
    return await res.json();
}

function updateCartCount(n) {
    var badge = document.getElementById('cartCount');
    if (!badge || n === undefined) return;
    badge.textContent = n;
    badge.classList.toggle('d-none', n <= 0);
}

// ---------- Theme ----------
function applyThemeIcon() {
    var btn = document.getElementById('themeToggle');
    if (!btn) return;
    var dark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
    btn.innerHTML = dark ? '<i class="bi bi-sun-fill"></i>' : '<i class="bi bi-moon-stars-fill"></i>';
}

function animateCounter(el) {
    var target = parseInt(el.dataset.count) || 0, start = null;
    function step(ts) {
        if (!start) start = ts;
        var p = Math.min((ts - start) / 900, 1);
        el.textContent = Math.floor(p * target);
        if (p < 1) requestAnimationFrame(step); else el.textContent = target;
    }
    requestAnimationFrame(step);
}

document.addEventListener('DOMContentLoaded', function () {
    applyThemeIcon();

    var toggle = document.getElementById('themeToggle');
    if (toggle) {
        toggle.addEventListener('click', function () {
            var next = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            document.documentElement.setAttribute('data-bs-theme', next);
            localStorage.setItem('theme', next);
            applyThemeIcon();
            if (window.__redrawCharts) window.__redrawCharts();
        });
    }

    document.querySelectorAll('[data-count]').forEach(animateCounter);

    document.querySelectorAll('.toggle-password').forEach(function (b) {
        b.addEventListener('click', function () {
            var input = b.parentElement.querySelector('input');
            var show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            b.innerHTML = show ? '<i class="bi bi-eye-slash"></i>' : '<i class="bi bi-eye"></i>';
        });
    });

    document.querySelectorAll('.demo-fill').forEach(function (b) {
        b.addEventListener('click', function () {
            document.querySelector('input[name="Email"]').value = b.dataset.email;
            document.querySelector('input[name="Password"]').value = b.dataset.pass;
        });
    });

    var pw = document.getElementById('regPassword');
    if (pw) {
        pw.addEventListener('input', function () {
            var v = pw.value, score = 0;
            if (v.length >= 6) score++;
            if (v.length >= 10) score++;
            if (/[A-Z]/.test(v) && /[a-z]/.test(v)) score++;
            if (/\d/.test(v) && /[^A-Za-z0-9]/.test(v)) score++;
            var colors = ['#ef4444', '#f59e0b', '#3b82f6', '#10b981'], names = ['Weak', 'Fair', 'Good', 'Strong'];
            var bar = document.getElementById('strengthBar');
            bar.style.width = (v ? Math.max(1, score) * 25 : 0) + '%';
            bar.style.background = colors[Math.max(0, score - 1)];
            document.getElementById('strengthText').textContent = v ? names[Math.max(0, score - 1)] : '';
        });
    }

    // confirm before submitting forms that have data-confirm
    document.querySelectorAll('form[data-confirm]').forEach(function (f) {
        f.addEventListener('submit', function (e) { if (!confirm(f.dataset.confirm)) e.preventDefault(); });
    });

    // admin: live image preview
    var img = document.getElementById('imageInput');
    if (img) {
        img.addEventListener('change', function () {
            var file = img.files[0];
            if (!file) return;
            var preview = document.getElementById('imagePreview');
            var holder = document.getElementById('imagePlaceholder');
            preview.src = URL.createObjectURL(file);
            preview.classList.remove('d-none');
            if (holder) holder.classList.add('d-none');
        });
    }
});

// ---------- Cart helpers ----------
async function reloadCart() {
    var box = document.getElementById('cartContent');
    if (!box) return;
    var res = await fetch('/Cart/Content');
    box.innerHTML = await res.text();
}

async function cartAction(url, data) {
    var r = await post(url, data);
    if (r.redirected) return r;
    showToast(r.message, r.success ? 'success' : 'danger');
    updateCartCount(r.count);
    await reloadCart();
    return r;
}

function readQty(btn) {
    if (btn.dataset.qtyInput) {
        var el = document.querySelector(btn.dataset.qtyInput);
        var v = parseInt(el && el.value);
        return v > 0 ? v : 1;
    }
    return 1;
}

// ---------- Click handling (delegated so it also works for AJAX-loaded content) ----------
document.addEventListener('click', async function (e) {
    var t = e.target;

    // add to cart
    var add = t.closest('.btn-add-cart');
    if (add) {
        add.disabled = true;
        var r = await post('/Cart/Add', { productId: add.dataset.id, quantity: readQty(add) });
        add.disabled = false;
        if (r.redirected) return;
        showToast(r.message, r.success ? 'success' : 'danger');
        if (r.success) updateCartCount(r.count);
        return;
    }

    // buy now
    var buy = t.closest('.btn-buy-now');
    if (buy) {
        buy.disabled = true;
        var rb = await post('/Cart/Add', { productId: buy.dataset.id, quantity: readQty(buy) });
        if (rb.success) { window.location = '/Checkout'; return; }
        buy.disabled = false;
        if (!rb.redirected) showToast(rb.message, 'danger');
        return;
    }

    // wishlist heart
    var wish = t.closest('.btn-wish');
    if (wish) {
        e.preventDefault();
        var rw = await post('/Wishlist/Toggle', { productId: wish.dataset.id });
        if (rw.redirected) return;
        if (!rw.success) { showToast(rw.message, 'danger'); return; }
        wish.classList.toggle('active', rw.added);
        var icon = wish.querySelector('i');
        icon.className = 'bi ' + (rw.added ? 'bi-heart-fill' : 'bi-heart') + (wish.classList.contains('btn-wish-text') ? ' me-1' : '');
        var label = wish.querySelector('span');
        if (label) label.textContent = rw.added ? 'In wishlist' : 'Add to wishlist';
        showToast(rw.message, 'success');
        // on the wishlist page, removing an item removes its card
        if (!rw.added && document.getElementById('wishlistPage')) {
            var col = wish.closest('.wish-col');
            if (col) col.remove();
            if (!document.querySelector('.wish-col')) location.reload();
        }
        return;
    }

    // quantity +/- buttons (product page and cart)
    var qbtn = t.closest('.qty-btn');
    if (qbtn) {
        var input = qbtn.parentElement.querySelector('.qty-input');
        var min = parseInt(input.min) || 1, max = parseInt(input.max) || 10;
        var val = Math.min(max, Math.max(min, (parseInt(input.value) || 1) + parseInt(qbtn.dataset.delta)));
        if (val === parseInt(input.value)) return;
        input.value = val;
        if (input.dataset.cart) cartAction('/Cart/Update', { productId: input.dataset.id, quantity: val });
        return;
    }

    // cart: remove / clear / coupon
    var rm = t.closest('.btn-cart-remove');
    if (rm) { cartAction('/Cart/Remove', { productId: rm.dataset.id }); return; }

    if (t.closest('#clearCart')) { if (confirm('Remove all items from your cart?')) cartAction('/Cart/Clear'); return; }

    if (t.closest('#applyCoupon')) {
        var code = document.getElementById('couponCode').value.trim();
        if (!code) { showToast('Please enter a coupon code.', 'danger'); return; }
        cartAction('/Cart/ApplyCoupon', { code: code });
        return;
    }
    if (t.closest('#removeCoupon')) { cartAction('/Cart/RemoveCoupon'); return; }

    var chip = t.closest('.coupon-chip');
    if (chip) { e.preventDefault(); document.getElementById('couponCode').value = chip.textContent.trim(); return; }
});

// typing a quantity directly in the cart
document.addEventListener('change', function (e) {
    var input = e.target.closest ? e.target.closest('.qty-input[data-cart]') : null;
    if (!input) return;
    var min = parseInt(input.min) || 1, max = parseInt(input.max) || 10;
    var val = Math.min(max, Math.max(min, parseInt(input.value) || 1));
    input.value = val;
    cartAction('/Cart/Update', { productId: input.dataset.id, quantity: val });
});

// ---------- Shop page: live search / filters / paging ----------
function initShop() {
    var box = document.getElementById('searchBox');
    var sort = document.getElementById('sortBy');
    var minP = document.getElementById('minPrice');
    var maxP = document.getElementById('maxPrice');
    var results = document.getElementById('productResults');
    var bar = document.getElementById('loadingBar');
    var timer = null, seq = 0, controller = null;

    function params(page) {
        var p = new URLSearchParams();
        if (box.value.trim()) p.set('q', box.value.trim());
        var cat = document.querySelector('.filter-cat:checked');
        if (cat && cat.value) p.set('category', cat.value);
        if (sort.value && sort.value !== 'relevance') p.set('sort', sort.value);
        if (minP.value) p.set('min', minP.value);
        if (maxP.value) p.set('max', maxP.value);
        if (page > 1) p.set('page', page);
        return p.toString();
    }

    async function load(page) {
        var qs = params(page || 1);

        // cancel the previous request so a slow old answer can never replace a newer one
        var mine = ++seq;
        if (controller) controller.abort();
        controller = new AbortController();

        results.classList.add('is-loading');
        bar.classList.remove('d-none');
        try {
            var res = await fetch(results.dataset.url + (qs ? '?' + qs : ''), { signal: controller.signal });
            if (!res.ok) throw new Error('bad response');
            var html = await res.text();
            if (mine !== seq) return;
            results.innerHTML = html;
            history.replaceState(null, '', '/Shop' + (qs ? '?' + qs : ''));
        } catch (err) {
            if (err.name === 'AbortError') return;
            showToast('Could not load products. Please try again.', 'danger');
        }
        if (mine === seq) {
            results.classList.remove('is-loading');
            bar.classList.add('d-none');
        }
    }

    function debounced() { clearTimeout(timer); timer = setTimeout(function () { load(1); }, 300); }

    box.addEventListener('input', debounced);
    box.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); clearTimeout(timer); load(1); }
    });
    minP.addEventListener('input', debounced);
    maxP.addEventListener('input', debounced);
    sort.addEventListener('change', function () { load(1); });
    document.querySelectorAll('.filter-cat').forEach(function (r) { r.addEventListener('change', function () { load(1); }); });

    document.getElementById('resetFilters').addEventListener('click', function () {
        box.value = ''; minP.value = ''; maxP.value = ''; sort.value = 'relevance';
        document.getElementById('cat0').checked = true;
        load(1);
    });

    results.addEventListener('click', function (e) {
        var link = e.target.closest('a[data-page]');
        if (!link) return;
        e.preventDefault();
        var li = link.closest('.page-item');
        if (li.classList.contains('disabled') || li.classList.contains('active')) return;
        load(parseInt(link.dataset.page));
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });
}

// ---------- Navbar search with live suggestions ----------
function initNavSearch() {
    var input = document.getElementById('navSearch');
    var box = document.getElementById('navSuggest');
    if (!input || !box) return;

    var timer = null, controller = null, seq = 0, active = -1;

    function esc(text) { var d = document.createElement('div'); d.textContent = text; return d.innerHTML; }

    // wrap the typed words in <mark> (the text is split first, so it is always HTML-escaped safely)
    function mark(text, term) {
        var words = term.toLowerCase().split(/[\s,;\/+.-]+/).filter(function (w) { return w.length > 1; });
        if (!words.length) return esc(text);
        var re = new RegExp('(' + words.map(function (w) { return w.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }).join('|') + ')', 'gi');
        return text.split(re).map(function (part, i) { return i % 2 ? '<mark>' + esc(part) + '</mark>' : esc(part); }).join('');
    }

    function hide() { box.classList.add('d-none'); active = -1; }

    function render(list, term) {
        if (!list.length) {
            box.innerHTML = '<div class="suggest-empty">No products match &ldquo;' + esc(term) + '&rdquo;</div>';
        } else {
            box.innerHTML = list.map(function (p) {
                var thumb = p.imageUrl ? '<img src="' + esc(p.imageUrl) + '" alt="" />' : '<i class="bi bi-box-seam"></i>';
                return '<a class="suggest-item" href="/Shop/Details/' + p.id + '">' +
                    '<span class="s-thumb">' + thumb + '</span>' +
                    '<span class="s-text"><span class="s-name">' + mark(p.name, term) + '</span><small>' + esc(p.category) + '</small></span>' +
                    '<span class="s-price">&#8377;' + Number(p.price).toLocaleString('en-IN') + '</span></a>';
            }).join('') +
                '<a class="suggest-all" href="/Shop?q=' + encodeURIComponent(term) + '">See all results for &ldquo;' + esc(term) + '&rdquo; <i class="bi bi-arrow-right"></i></a>';
        }
        active = -1;
        box.classList.remove('d-none');
    }

    async function fetchSuggestions() {
        var term = input.value.trim();
        if (term.length < 2) { hide(); return; }

        var mine = ++seq;
        if (controller) controller.abort();
        controller = new AbortController();
        try {
            var res = await fetch('/Shop/Suggest?term=' + encodeURIComponent(term), { signal: controller.signal });
            if (!res.ok) return;
            var list = await res.json();
            if (mine === seq) render(list, term);
        } catch (err) { /* aborted or offline - ignore */ }
    }

    input.addEventListener('input', function () {
        clearTimeout(timer);
        timer = setTimeout(fetchSuggestions, 200);
    });
    input.addEventListener('focus', function () { if (input.value.trim().length >= 2 && box.innerHTML) box.classList.remove('d-none'); });

    input.addEventListener('keydown', function (e) {
        var items = box.querySelectorAll('.suggest-item');
        if (e.key === 'Escape') { hide(); return; }
        if (!items.length || box.classList.contains('d-none')) return;

        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            e.preventDefault();
            active = e.key === 'ArrowDown' ? (active + 1) % items.length : (active - 1 + items.length) % items.length;
            items.forEach(function (it, i) { it.classList.toggle('active', i === active); });
        } else if (e.key === 'Enter' && active >= 0) {
            e.preventDefault();
            window.location = items[active].getAttribute('href');
        }
    });

    document.addEventListener('click', function (e) { if (!e.target.closest('.nav-search')) hide(); });
}

document.addEventListener('DOMContentLoaded', initNavSearch);

// ---------- Admin dashboard charts ----------
function initAdminCharts(data) {
    if (typeof Chart === 'undefined') return;
    var charts = [];

    function draw() {
        charts.forEach(function (c) { c.destroy(); });
        charts = [];
        var dark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
        Chart.defaults.color = dark ? '#cbd5e1' : '#475569';
        Chart.defaults.font.family = 'Inter, sans-serif';
        var grid = dark ? 'rgba(255,255,255,.08)' : 'rgba(0,0,0,.06)';

        var rev = document.getElementById('revChart');
        if (rev) charts.push(new Chart(rev, {
            type: 'bar',
            data: { labels: data.dayLabels, datasets: [{ label: 'Revenue', data: data.dayRevenue, backgroundColor: '#6d28d9', borderRadius: 8, maxBarThickness: 44 }] },
            options: { plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, grid: { color: grid } }, x: { grid: { display: false } } } }
        }));

        var st = document.getElementById('statusChart');
        if (st) charts.push(new Chart(st, {
            type: 'doughnut',
            data: { labels: data.statusLabels, datasets: [{ data: data.statusCounts, backgroundColor: ['#f59e0b', '#06b6d4', '#6d28d9', '#10b981', '#ef4444'], borderWidth: 0 }] },
            options: { cutout: '62%', plugins: { legend: { position: 'bottom' } } }
        }));
    }

    window.__redrawCharts = draw;
    draw();
}
