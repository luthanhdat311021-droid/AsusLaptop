// ===== ASUS LAPTOP STORE - MAIN JS =====

// Format currency VND
function formatVND(amount) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

// Add to cart AJAX
function addToCartAjax(productId, btn) {
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<i class="fa fa-spinner fa-spin"></i>';
    }
    fetch('/Cart/AddToCartAjax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'productId=' + productId
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            // Update badge
            const badge = document.getElementById('cartBadge');
            if (badge) badge.textContent = data.cartCount;

            // Show mini toast
            showMiniToast('Đã thêm vào giỏ hàng!', 'success');
        }
    })
    .catch(() => showMiniToast('Có lỗi xảy ra!', 'error'))
    .finally(() => {
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<i class="fa fa-cart-plus"></i> Thêm giỏ';
        }
    });
}

function showMiniToast(msg, type) {
    const existing = document.querySelector('.mini-toast');
    if (existing) existing.remove();
    const t = document.createElement('div');
    t.className = 'toast mini-toast toast-' + (type === 'success' ? 'success' : 'error');
    t.style.cssText = 'position:fixed;bottom:24px;right:24px;z-index:9999;';
    t.innerHTML = '<i class="fa fa-' + (type === 'success' ? 'check-circle' : 'exclamation-circle') + '"></i> ' + msg;
    document.body.appendChild(t);
    setTimeout(() => t.classList.add('toast-hide'), 2000);
    setTimeout(() => t.remove(), 2500);
}

// ===== So sánh sản phẩm (Compare) =====
function addToCompareAjax(productId, btn) {
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<i class="fa fa-spinner fa-spin"></i>';
    }
    fetch('/Compare/Add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'id=' + productId
    })
    .then(r => r.json())
    .then(data => {
        showMiniToast(data.message || (data.success ? 'Đã thêm vào so sánh!' : 'Có lỗi xảy ra!'), data.success ? 'success' : 'error');
        if (data.success) refreshCompareBar();
    })
    .catch(() => showMiniToast('Không thể kết nối server.', 'error'))
    .finally(() => {
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<i class="fa fa-balance-scale"></i>';
        }
    });
}

function removeFromCompareBar(productId) {
    fetch('/Compare/Remove', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'id=' + productId
    })
    .then(r => r.json())
    .then(() => refreshCompareBar())
    .catch(() => showMiniToast('Không thể kết nối server.', 'error'));
}

function refreshCompareBar() {
    const bar = document.getElementById('compareBar');
    if (!bar) return;
    fetch('/Compare/List')
        .then(r => r.json())
        .then(data => {
            const badge = document.getElementById('compareBadge');
            const itemsWrap = document.getElementById('compareBarItems');
            const goBtn = document.getElementById('compareGoBtn');

            if (!data.items || data.items.length === 0) {
                bar.classList.remove('show');
                if (badge) badge.style.display = 'none';
                return;
            }

            if (badge) {
                badge.style.display = 'flex';
                badge.textContent = data.count;
            }
            if (goBtn) goBtn.textContent = 'So sánh ngay (' + data.count + ')';

            if (itemsWrap) {
                itemsWrap.innerHTML = data.items.map(function (p) {
                    var img = p.imageUrl
                        ? '<img src="' + p.imageUrl + '" alt="" />'
                        : '<i class="fa fa-laptop"></i>';
                    return '<div class="compare-bar-item">' +
                        '<button type="button" class="compare-bar-remove" onclick="removeFromCompareBar(' + p.id + ')" title="Bỏ khỏi so sánh"><i class="fa fa-times"></i></button>' +
                        '<div class="compare-bar-thumb">' + img + '</div>' +
                        '<span class="compare-bar-name">' + p.name + '</span>' +
                        '</div>';
                }).join('');
            }

            bar.classList.add('show');
        })
        .catch(() => {});
}

document.addEventListener('DOMContentLoaded', refreshCompareBar);

// Cart qty update
function updateCartQty(productId, qty) {
    fetch('/Cart/UpdateQuantityAjax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'productId=' + productId + '&quantity=' + qty
    })
    .then(r => r.json())
    .then(data => { if (data.success) location.reload(); });
}
