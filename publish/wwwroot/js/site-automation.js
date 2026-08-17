/**
 * Tự động hóa frontend — poll API live status, cập nhật Flash Sale & thông báo.
 */
(function () {
    'use strict';

    var POLL_MS = 45000;
    var lastFlashActive = null;

    function pad(n) { return String(n).padStart(2, '0'); }

    function updateCountdown(endIso) {
        if (!endIso) return;
        var end = new Date(endIso.replace(' ', 'T'));
        var diff = Math.max(0, end - Date.now());
        var h = Math.floor(diff / 3600000);
        var m = Math.floor((diff % 3600000) / 60000);
        var s = Math.floor((diff % 60000) / 1000);

        ['flashHours', 'flashMins', 'flashSecs', 'flashHoursTicker', 'flashMinutesTicker', 'flashSecondsTicker'].forEach(function (id, i) {
            var el = document.getElementById(id);
            if (!el) return;
            if (id.indexOf('Hours') !== -1 || id.indexOf('flash-h') !== -1) el.textContent = pad(h);
            else if (id.indexOf('Mins') !== -1 || id.indexOf('Minutes') !== -1 || id.indexOf('flash-m') !== -1) el.textContent = pad(m);
            else el.textContent = pad(s);
        });
    }

    function updateFlashSaleStatus(isActive, slotName) {
        if (typeof isActive !== 'boolean') return;

        document.querySelectorAll('.flash-sub-badge').forEach(function (el) {
            el.textContent = 'KHUNG GIỜ ' + (slotName || '20:00 - 23:00') + (isActive ? ' • ĐANG DIỄN RA' : ' • SẮP DIỄN RA');
        });

        document.querySelectorAll('.flash-timer-label').forEach(function (el) {
            el.textContent = isActive ? 'KẾT THÚC SAU:' : 'BẮT ĐẦU SAU:';
        });

        document.querySelectorAll('.flash-card').forEach(function (card) {
            var btn = card.querySelector('.flash-buy-btn');
            if (!btn) return;
            if (isActive) {
                if (btn.classList.contains('disabled')) {
                    btn.classList.remove('disabled');
                    btn.removeAttribute('disabled');
                    btn.removeAttribute('title');
                    btn.innerHTML = '<i class="fa fa-bolt"></i> Mua Ngay Flash Sale';
                    var cardLink = card.querySelector('a[href*="/Product/Details/"]');
                    if (cardLink) {
                        var pId = cardLink.getAttribute('href').split('/').pop();
                        btn.setAttribute('onclick', 'addToCartAjax(' + pId + ', this)');
                    }
                }
            } else {
                btn.classList.add('disabled');
                btn.setAttribute('disabled', 'disabled');
                btn.setAttribute('title', 'Chưa đến khung giờ Flash Sale');
                btn.innerHTML = '<i class="fa fa-clock"></i> Sắp Đến Giờ Sale';
                btn.removeAttribute('onclick');
            }
        });
    }

    function updateSoldBars(items) {
        if (!items || !items.length) return;
        items.forEach(function (item) {
            var cards = document.querySelectorAll('.flash-card');
            cards.forEach(function (card) {
                var link = card.querySelector('a[href*="/Product/Details/' + item.productId + '"]');
                if (!link) return;
                var fill = card.querySelector('.flash-progress-fill');
                var text = card.querySelector('.flash-progress-text');
                if (fill) fill.style.width = item.soldPercent + '%';
                if (text) text.textContent = '🔥 Đã bán ' + item.soldPercent + '%';
            });
        });
    }

    function showFlashToast() {
        var toast = document.createElement('div');
        toast.style.cssText = 'position:fixed;top:90px;right:24px;z-index:99999;background:#111;color:#fff;padding:14px 20px;border-radius:12px;box-shadow:0 8px 30px rgba(0,0,0,.35);font-weight:600;font-size:14px;animation:slideIn .3s ease';
        toast.innerHTML = '<i class="fa fa-bolt" style="color:#ff4d00;margin-right:8px"></i> Flash Sale đã bắt đầu — Săn deal ngay!';
        document.body.appendChild(toast);
        setTimeout(function () { toast.remove(); }, 5000);
    }

    function poll() {
        fetch('/api/automation/live')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                updateCountdown(data.flashSaleEndIso);
                updateFlashSaleStatus(data.flashSaleActive, data.flashSlotName);

                if (lastFlashActive === false && data.flashSaleActive === true)
                    showFlashToast();
                lastFlashActive = data.flashSaleActive;

                updateSoldBars(data.flashItems);
            })
            .catch(function () { /* silent */ });
    }

    if (document.getElementById('flashSaleSection') || document.getElementById('flashTickerBox')) {
        poll();
        setInterval(poll, POLL_MS);
    }
})();
