/* ==========================================================================
   ASUS STORYTELLING LAYOUT ENGINE — SCROLLYTELLING INTERACTOR
   ========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
    // 1. WEB AUDIO SYNTHESIZER FOR AMBIENT SCI-FI SOUNDS
    let audioCtx = null;
    let isMuted = true;

    function initAudio() {
        if (!audioCtx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (AudioContext) {
                audioCtx = new AudioContext();
            }
        }
    }

    function playTechSound(freq = 440, type = 'sine', duration = 0.15) {
        if (isMuted || !audioCtx) return;
        try {
            if (audioCtx.state === 'suspended') {
                audioCtx.resume();
            }
            const osc = audioCtx.createOscillator();
            const gain = audioCtx.createGain();
            osc.type = type;
            osc.frequency.setValueAtTime(freq, audioCtx.currentTime);
            gain.gain.setValueAtTime(0.08, audioCtx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + duration);

            osc.connect(gain);
            gain.connect(audioCtx.destination);

            osc.start();
            osc.stop(audioCtx.currentTime + duration);
        } catch (e) {
            console.warn('Audio play error:', e);
        }
    }

    // Audio Toggle Logic
    const audioToggleBtn = document.getElementById('stAudioToggle');
    if (audioToggleBtn) {
        audioToggleBtn.addEventListener('click', () => {
            initAudio();
            isMuted = !isMuted;
            if (isMuted) {
                audioToggleBtn.classList.add('muted');
                audioToggleBtn.querySelector('.st-audio-text').textContent = 'Âm thanh: Tắt';
            } else {
                audioToggleBtn.classList.remove('muted');
                audioToggleBtn.querySelector('.st-audio-text').textContent = 'Âm thanh: Bật';
                playTechSound(600, 'triangle', 0.3);
            }
        });
    }

    // 2. INTERSECTION OBSERVER FOR SCROLLYTELLING STAGE & CARDS
    const storyCards = document.querySelectorAll('.st-story-card');
    const stageContents = document.querySelectorAll('.st-stage-content');
    const navDots = document.querySelectorAll('.st-nav-dot');

    const observerOptions = {
        root: null,
        rootMargin: '-30% 0px -40% 0px',
        threshold: 0.2
    };

    const cardObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                storyCards.forEach(c => c.classList.remove('is-active'));
                entry.target.classList.add('is-active');

                const stageId = entry.target.dataset.stage;
                const chapterId = entry.target.dataset.chapter;

                stageContents.forEach(stage => {
                    if (stage.id === stageId) {
                        stage.classList.add('active');
                    } else {
                        stage.classList.remove('active');
                    }
                });

                navDots.forEach(dot => {
                    if (dot.dataset.chapter === chapterId) {
                        dot.classList.add('active');
                    } else {
                        dot.classList.remove('active');
                    }
                });

                playTechSound(320 + parseInt(chapterId || 1) * 60, 'sine', 0.15);
            }
        });
    }, observerOptions);

    storyCards.forEach(card => cardObserver.observe(card));

    // Observe Chapter 1, 4, 5 for side nav dots
    const chapterSections = [
        { id: 'chapter1', chapter: '1' },
        { id: 'chapter4', chapter: '4' },
        { id: 'chapter5', chapter: '5' }
    ];

    const chapterObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const targetId = entry.target.id;
                const match = chapterSections.find(c => c.id === targetId);
                if (match) {
                    navDots.forEach(dot => {
                        if (dot.dataset.chapter === match.chapter) {
                            dot.classList.add('active');
                        } else {
                            dot.classList.remove('active');
                        }
                    });
                }
            }
        });
    }, { threshold: 0.3 });

    chapterSections.forEach(item => {
        const el = document.getElementById(item.id);
        if (el) chapterObserver.observe(el);
    });

    // 3. NAV DOT SMOOTH SCROLLING
    navDots.forEach(dot => {
        dot.addEventListener('click', () => {
            const targetCh = dot.dataset.chapter;
            let targetEl = document.getElementById(`chapter${targetCh}`);
            if (!targetEl) {
                targetEl = document.querySelector(`.st-story-card[data-chapter="${targetCh}"]`);
            }
            if (targetEl) {
                targetEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
                playTechSound(520, 'triangle', 0.1);
            }
        });
    });

    // 4. HOTSPOT SOUND & HOVER EFFECT
    const hotspots = document.querySelectorAll('.st-hotspot-node');
    hotspots.forEach(hp => {
        hp.addEventListener('mouseenter', () => {
            playTechSound(750, 'sine', 0.08);
        });
    });
});
