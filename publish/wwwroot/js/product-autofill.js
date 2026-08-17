/* Automatically fills product specifications after an administrator types a model name. */
(function () {
    'use strict';
    var nameInput = document.getElementById('Name');
    if (!nameInput) return;

    var status = document.getElementById('aiDescriptionStatus');
    var debounceTimer, requestVersion = 0;
    var fields = ['Brand', 'Series', 'CPU', 'RAM', 'Storage', 'GPU', 'ScreenSize', 'ScreenResolution', 'Battery', 'Weight', 'OS', 'Description'];

    function setStatus(message, isError) {
        if (!status) return;
        status.textContent = message;
        status.style.color = isError ? '#c62828' : '#777';
    }

    async function autoFill() {
        var name = nameInput.value.trim();
        if (name.length < 5) return;
        var currentVersion = ++requestVersion;
        setStatus('AI đang nhận diện tên máy và tự điền cấu hình...', false);
        try {
            var response = await fetch('/Product/AutoFillFromName', {
                method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name: name })
            });
            var data = await response.json();
            if (currentVersion !== requestVersion) return;
            if (!response.ok) throw new Error(data.message || 'Không tìm được cấu hình.');
            fields.forEach(function (field) {
                var input = document.getElementById(field);
                var property = field.charAt(0).toLowerCase() + field.slice(1);
                if (input && data[property]) input.value = data[property];
            });
            setStatus(data.notice || 'Đã tự điền cấu hình. Hãy kiểm tra trước khi lưu.', false);
        } catch (error) {
            if (currentVersion === requestVersion) setStatus(error.message, true);
        }
    }

    nameInput.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(autoFill, 900);
    });
})();
