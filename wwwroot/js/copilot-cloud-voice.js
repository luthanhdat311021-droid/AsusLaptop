/* Vietnamese cloud TTS fallback, streamed by the website to avoid browser cross-origin blocking. */
(function () {
    'use strict';
    var activeAudio = null;

    function splitText(text, maxLength) {
        var words = String(text || '').replace(/\s+/g, ' ').trim().split(' ');
        var chunks = [], current = '';
        words.forEach(function (word) {
            var next = current ? current + ' ' + word : word;
            if (next.length > maxLength && current) { chunks.push(current); current = word; }
            else { current = next; }
        });
        if (current) chunks.push(current);
        return chunks;
    }

    function getResultText() {
        var summary = document.querySelector('.copilot-summary');
        var cards = Array.prototype.slice.call(document.querySelectorAll('.copilot-card'));
        return [summary ? summary.textContent : ''].concat(cards.map(function (card) {
            var title = card.querySelector('h3');
            var strengths = Array.prototype.slice.call(card.querySelectorAll('.copilot-strength'));
            var tradeoff = card.querySelector('.copilot-tradeoff');
            return (title ? title.textContent + '. ' : '') + strengths.map(function (el) { return el.textContent; }).join('. ') + '. ' + (tradeoff ? tradeoff.textContent : '');
        })).join(' ');
    }

    function playCloudVietnamese(text, button) {
        if (activeAudio) { activeAudio.pause(); activeAudio = null; }
        var chunks = splitText(text, 180), index = 0, original = button.innerHTML;
        function next() {
            if (index >= chunks.length) { button.innerHTML = original; activeAudio = null; return; }
            button.innerHTML = '<i class="fa fa-stop"></i> Dừng đọc';
            activeAudio = new Audio('/Copilot/Voice?text=' + encodeURIComponent(chunks[index++]));
            activeAudio.onended = next;
            activeAudio.onerror = function () {
                button.innerHTML = original;
                activeAudio = null;
                alert('Dịch vụ giọng nói tạm thời không phản hồi. Vui lòng thử lại sau.');
            };
            activeAudio.play().catch(function () { button.innerHTML = original; activeAudio = null; });
        }
        next();
    }

    document.addEventListener('click', function (event) {
        var button = event.target.closest && event.target.closest('#speakResult');
        if (!button) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        if (activeAudio) {
            activeAudio.pause(); activeAudio = null;
            button.innerHTML = '<i class="fa fa-volume-high"></i> Nghe tư vấn tiếng Việt';
            return;
        }
        playCloudVietnamese(getResultText(), button);
    }, true);
})();
