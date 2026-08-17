/**
 * ASUS LAPTOP STORE - 360° REAL PRODUCT PHOTO SPINNER ENGINE
 * High-performance 60FPS Interactive Photo Rotation & Hotspot Studio
 */

class PhotoSpinner360 {
    constructor(options) {
        this.container = typeof options.container === 'string' 
            ? document.querySelector(options.container) 
            : options.container;

        if (!this.container) return;

        this.images = options.images || [];
        this.totalFrames = this.images.length || 16;
        this.currentFrame = 0;
        this.isDragging = false;
        this.startX = 0;
        this.dragSensitivity = options.sensitivity || 6; // px per frame
        this.isAutoSpinning = false;
        this.autoSpinTimer = null;
        this.preloadedImages = [];
        this.hotspots = options.hotspots || [];

        this.initUI();
        this.preloadImages();
    }

    initUI() {
        this.container.innerHTML = `
            <div class="photo-spinner-wrapper" style="position:relative; width:100%; height:100%; min-height:380px; background:radial-gradient(circle at 50% 45%, #222536 0%, #0d0e15 85%); border-radius:18px; overflow:hidden; user-select:none; touch-action:none; display:flex; flex-direction:column; align-items:center; justify-content:center; border:1px solid rgba(255,77,0,0.25); box-shadow:inset 0 0 40px rgba(0,0,0,0.8);">
                
                <!-- Studio Platform Pedestal Surface Glow -->
                <div style="position:absolute; bottom:60px; left:50%; transform:translateX(-50%); width:75%; height:30px; background:radial-gradient(ellipse at center, rgba(255,77,0,0.2) 0%, rgba(255,77,0,0.05) 45%, transparent 70%); border-radius:50%; pointer-events:none;"></div>

                <!-- Loading Progress Overlay -->
                <div class="spinner-loader" style="position:absolute; inset:0; background:rgba(13,14,21,0.92); backdrop-filter:blur(8px); z-index:10; display:flex; flex-direction:column; align-items:center; justify-content:center; color:#fff; transition:opacity 0.4s ease;">
                    <i class="fa fa-circle-notch fa-spin" style="font-size:36px; color:#ff4d00; margin-bottom:12px;"></i>
                    <div style="font-size:14px; font-weight:700; letter-spacing:1px; margin-bottom:8px;">Đang tải Studio 360° Thực Tế...</div>
                    <div class="spinner-progress-bar" style="width:180px; height:5px; background:rgba(255,255,255,0.1); border-radius:99px; overflow:hidden;">
                        <div class="spinner-progress-fill" style="width:0%; height:100%; background:linear-gradient(90deg,#ff4d00,#ffd700); transition:width 0.2s;"></div>
                    </div>
                </div>

                <!-- Canvas Rendering Area -->
                <canvas class="spinner-canvas" style="width:100%; height:100%; max-height:420px; object-fit:contain; cursor:grab; position:relative; z-index:2;"></canvas>
                
                <!-- Drag Hint Badge -->
                <div class="spinner-hint-badge" style="position:absolute; top:16px; left:16px; background:rgba(0,0,0,0.7); backdrop-filter:blur(10px); border:1px solid rgba(255,255,255,0.15); border-radius:99px; padding:6px 14px; font-size:12px; font-weight:700; color:#ffffff; display:flex; align-items:center; gap:8px; pointer-events:none; z-index:5;">
                    <i class="fa-solid fa-arrows-left-right" style="color:#ff4d00;"></i>
                    <span>Kéo / Vuốt tay để xoay 360°</span>
                </div>

                <!-- Frame Angle Indicator Pill -->
                <div class="spinner-info-badge" style="position:absolute; top:16px; right:16px; background:rgba(0,0,0,0.7); backdrop-filter:blur(10px); border:1px solid rgba(255,77,0,0.3); border-radius:8px; padding:5px 12px; font-size:11px; font-weight:800; color:#ffd700; font-family:monospace; z-index:5;">
                    GÓC 360°: <span class="spinner-frame-text">0°</span>
                </div>

                <!-- Bottom Control Toolbar -->
                <div class="spinner-controls" style="position:absolute; bottom:16px; display:flex; align-items:center; gap:10px; background:rgba(18,19,27,0.88); backdrop-filter:blur(12px); border:1px solid rgba(255,255,255,0.15); border-radius:99px; padding:6px 18px; z-index:5; box-shadow:0 10px 25px rgba(0,0,0,0.6);">
                    <button type="button" class="btn-spinner-play" title="Tự động xoay 360°" style="background:none; border:none; color:#ff4d00; font-size:15px; cursor:pointer; padding:4px 8px; display:flex; align-items:center; gap:6px; font-weight:700;">
                        <i class="fa-solid fa-play"></i> <span style="font-size:12px; color:#fff;">Auto Spin</span>
                    </button>
                    <div style="width:1px; height:16px; background:rgba(255,255,255,0.2);"></div>
                    <button type="button" class="btn-spinner-reset" title="Về góc chính diện" style="background:none; border:none; color:rgba(255,255,255,0.8); font-size:13px; cursor:pointer; padding:4px 8px; display:flex; align-items:center; gap:6px; font-weight:700;">
                        <i class="fa-solid fa-rotate-left"></i> <span style="font-size:12px; color:#fff;">Chính diện</span>
                    </button>
                </div>

                <!-- Hotspot Overlay Container -->
                <div class="spinner-hotspots-layer" style="position:absolute; inset:0; pointer-events:none; z-index:6;"></div>
            </div>
        `;

        this.canvas = this.container.querySelector('.spinner-canvas');
        this.ctx = this.canvas.getContext('2d');
        this.loader = this.container.querySelector('.spinner-loader');
        this.progressFill = this.container.querySelector('.spinner-progress-fill');
        this.frameText = this.container.querySelector('.spinner-frame-text');
        this.playBtn = this.container.querySelector('.btn-spinner-play');
        this.resetBtn = this.container.querySelector('.btn-spinner-reset');
        this.hotspotsLayer = this.container.querySelector('.spinner-hotspots-layer');

        this.attachEvents();
    }

    preloadImages() {
        if (!this.images || this.images.length === 0) {
            this.loader.style.display = 'none';
            this.renderFrame();
            return;
        }

        let loaded = 0;
        const total = this.images.length;

        this.images.forEach((src, idx) => {
            const img = new Image();
            // IMPORTANT: DO NOT set crossOrigin = 'anonymous' to prevent CORS block on external CDNs!
            img.onload = () => {
                loaded++;
                if (this.progressFill) {
                    this.progressFill.style.width = Math.round((loaded / total) * 100) + '%';
                }
                if (loaded === total) this.onLoaded();
            };
            img.onerror = () => {
                loaded++;
                if (this.progressFill) {
                    this.progressFill.style.width = Math.round((loaded / total) * 100) + '%';
                }
                if (loaded === total) this.onLoaded();
            };
            img.src = src;
            this.preloadedImages[idx] = img;
        });
    }

    onLoaded() {
        setTimeout(() => {
            if (this.loader) {
                this.loader.style.opacity = '0';
                setTimeout(() => this.loader.style.display = 'none', 300);
            }
            this.renderFrame();
        }, 150);
    }

    attachEvents() {
        const wrapper = this.container.querySelector('.photo-spinner-wrapper');
        if (!wrapper) return;

        // Mouse Drag
        wrapper.addEventListener('mousedown', (e) => {
            if (e.target.closest('.spinner-controls') || e.target.closest('.spinner-hotspot-card')) return;
            this.isDragging = true;
            this.startX = e.clientX;
            this.canvas.style.cursor = 'grabbing';
            this.stopAutoSpin();
        });

        window.addEventListener('mousemove', (e) => {
            if (!this.isDragging) return;
            const deltaX = e.clientX - this.startX;
            if (Math.abs(deltaX) > this.dragSensitivity) {
                const frameStep = Math.floor(deltaX / this.dragSensitivity);
                this.currentFrame = (this.currentFrame - frameStep + this.totalFrames * 100) % this.totalFrames;
                this.startX = e.clientX;
                this.renderFrame();
            }
        });

        window.addEventListener('mouseup', () => {
            if (this.isDragging) {
                this.isDragging = false;
                this.canvas.style.cursor = 'grab';
            }
        });

        // Touch Drag for Mobile
        wrapper.addEventListener('touchstart', (e) => {
            if (e.target.closest('.spinner-controls')) return;
            if (e.touches.length === 1) {
                this.isDragging = true;
                this.startX = e.touches[0].clientX;
                this.stopAutoSpin();
            }
        });

        wrapper.addEventListener('touchmove', (e) => {
            if (!this.isDragging || e.touches.length !== 1) return;
            const deltaX = e.touches[0].clientX - this.startX;
            if (Math.abs(deltaX) > this.dragSensitivity) {
                const frameStep = Math.floor(deltaX / this.dragSensitivity);
                this.currentFrame = (this.currentFrame - frameStep + this.totalFrames * 100) % this.totalFrames;
                this.startX = e.touches[0].clientX;
                this.renderFrame();
            }
        });

        wrapper.addEventListener('touchend', () => {
            this.isDragging = false;
        });

        // Controls
        if (this.playBtn) {
            this.playBtn.addEventListener('click', () => {
                if (this.isAutoSpinning) {
                    this.stopAutoSpin();
                } else {
                    this.startAutoSpin();
                }
            });
        }

        if (this.resetBtn) {
            this.resetBtn.addEventListener('click', () => {
                this.stopAutoSpin();
                this.currentFrame = 0;
                this.renderFrame();
            });
        }

        window.addEventListener('resize', () => {
            requestAnimationFrame(() => this.renderFrame());
        });
    }

    renderFrame() {
        if (!this.canvas) return;

        const rect = this.canvas.getBoundingClientRect();
        const width = rect.width > 0 ? rect.width : 400;
        const height = rect.height > 0 ? rect.height : 360;

        const dpr = window.devicePixelRatio || 1;
        this.canvas.width = width * dpr;
        this.canvas.height = height * dpr;

        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

        const degrees = Math.round((this.currentFrame / this.totalFrames) * 360);
        if (this.frameText) {
            this.frameText.textContent = `${degrees}°`;
        }

        let img = this.preloadedImages[this.currentFrame % this.preloadedImages.length];

        // If specific frame isn't loaded or invalid, fallback to first valid loaded image
        if (!img || !img.complete || img.naturalWidth === 0) {
            img = this.preloadedImages.find(i => i && i.complete && i.naturalWidth > 0);
        }

        if (img && img.complete && img.naturalWidth > 0) {
            this.drawRealistic3DImage(img, degrees);
        } else {
            this.drawFallbackStudioText(degrees);
        }

        this.renderHotspots();
    }

    drawRealistic3DImage(img, degrees) {
        const cW = this.canvas.width;
        const cH = this.canvas.height;

        const imgRatio = img.naturalWidth / img.naturalHeight;
        const canvasRatio = cW / cH;
        let drawW, drawH;

        if (imgRatio > canvasRatio) {
            drawW = cW * 0.78;
            drawH = drawW / imgRatio;
        } else {
            drawH = cH * 0.78;
            drawW = drawH * imgRatio;
        }

        // Calculate 3D Perspective Rotation Factors based on current angle degrees
        const rad = (degrees * Math.PI) / 180;
        const scaleX = Math.cos(rad); // Horizontal perspective contraction
        const absScaleX = Math.max(0.18, Math.abs(scaleX));
        const skewY = Math.sin(rad) * 0.08; // Subtle 3D tilt

        this.ctx.save();
        this.ctx.translate(cW / 2, cH / 2 + 10);

        // Dynamic 3D Drop Shadow on Pedestal Surface
        const shadowScaleX = 1 + (1 - absScaleX) * 0.3;
        this.ctx.save();
        this.ctx.scale(shadowScaleX, 0.25);
        this.ctx.beginPath();
        this.ctx.arc(0, drawH * 1.5, drawW * 0.5, 0, Math.PI * 2);
        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.6)';
        this.ctx.fill();
        this.ctx.restore();

        // 3D Perspective Transformation Matrix
        this.ctx.transform(scaleX, skewY, 0, 1, 0, 0);

        this.ctx.imageSmoothingEnabled = true;
        this.ctx.imageSmoothingQuality = 'high';
        this.ctx.drawImage(img, -drawW / 2, -drawH / 2, drawW, drawH);

        // Ambient Studio Reflection Shading based on Rotation Angle
        const shadowAlpha = Math.abs(Math.sin(rad)) * 0.35;
        if (shadowAlpha > 0.05) {
            this.ctx.fillStyle = `rgba(0, 0, 0, ${shadowAlpha})`;
            this.ctx.fillRect(-drawW / 2, -drawH / 2, drawW, drawH);
        }

        this.ctx.restore();
    }

    drawFallbackStudioText(degrees) {
        const cW = this.canvas.width;
        const cH = this.canvas.height;
        this.ctx.save();
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';

        this.ctx.fillStyle = '#ff4d00';
        this.ctx.font = 'bold 18px Barlow Condensed, sans-serif';
        this.ctx.fillText('ASUS REAL 360° STUDIO', cW / 2, cH / 2 - 20);

        this.ctx.fillStyle = 'rgba(255, 255, 255, 0.7)';
        this.ctx.font = '13px sans-serif';
        this.ctx.fillText(`Góc quan sát: ${degrees}° (Kéo chuột/vuốt để xoay)`, cW / 2, cH / 2 + 15);
        this.ctx.restore();
    }

    renderHotspots() {
        if (!this.hotspotsLayer) return;
        this.hotspotsLayer.innerHTML = '';

        const currentHotspots = this.hotspots.filter(h => Math.abs(h.frameIndex - this.currentFrame) <= 1);

        currentHotspots.forEach(h => {
            const el = document.createElement('div');
            el.className = 'spinner-hotspot-pin';
            el.style.cssText = `position:absolute; left:${h.x}%; top:${h.y}%; transform:translate(-50%,-50%); pointer-events:auto; cursor:pointer; z-index:8;`;
            el.innerHTML = `
                <div class="hotspot-pulse" style="width:28px; height:28px; background:rgba(255,77,0,0.3); border:2px solid #ff4d00; border-radius:50%; display:flex; align-items:center; justify-content:center; box-shadow:0 0 15px rgba(255,77,0,0.8); animation:hotspotPulse 1.8s infinite;">
                    <i class="fa fa-plus" style="color:#fff; font-size:11px;"></i>
                </div>
            `;
            el.addEventListener('click', (e) => {
                e.stopPropagation();
                this.showHotspotCard(h);
            });
            this.hotspotsLayer.appendChild(el);
        });
    }

    showHotspotCard(hotspot) {
        const existing = this.container.querySelector('.spinner-hotspot-card');
        if (existing) existing.remove();

        const card = document.createElement('div');
        card.className = 'spinner-hotspot-card';
        card.style.cssText = `position:absolute; left:50%; top:50%; transform:translate(-50%,-50%); background:rgba(18,19,27,0.95); backdrop-filter:blur(12px); border:1px solid rgba(255,77,0,0.4); box-shadow:0 15px 40px rgba(0,0,0,0.8), 0 0 25px rgba(255,77,0,0.2); border-radius:16px; padding:20px; max-width:320px; width:90%; color:#fff; z-index:20; pointer-events:auto; animation:vipFadeIn 0.3s ease;`;
        card.innerHTML = `
            <button type="button" class="btn-close-card" style="position:absolute; top:12px; right:14px; background:none; border:none; color:rgba(255,255,255,0.6); font-size:20px; cursor:pointer;">&times;</button>
            <div style="font-size:11px; font-weight:800; color:#ff4d00; text-transform:uppercase; letter-spacing:1px; margin-bottom:6px;"><i class="fa-solid fa-microchip"></i> CHI TIẾT ĐẶC TÍNH</div>
            <h4 style="font-size:1.1rem; font-weight:800; margin-bottom:8px; color:#ffffff;">${hotspot.title}</h4>
            <p style="font-size:12.5px; color:rgba(255,255,255,0.8); line-height:1.5; margin-bottom:12px;">${hotspot.description}</p>
            ${hotspot.image ? `<img src="${hotspot.image}" style="width:100%; height:140px; object-fit:cover; border-radius:8px; border:1px solid rgba(255,255,255,0.1);" />` : ''}
        `;

        card.querySelector('.btn-close-card').addEventListener('click', () => card.remove());
        this.container.querySelector('.photo-spinner-wrapper').appendChild(card);
    }

    startAutoSpin() {
        if (this.isAutoSpinning) return;
        this.isAutoSpinning = true;
        if (this.playBtn) {
            this.playBtn.innerHTML = '<i class="fa-solid fa-pause"></i> <span style="font-size:12px; color:#fff;">Tạm dừng</span>';
        }
        this.autoSpinTimer = setInterval(() => {
            this.currentFrame = (this.currentFrame + 1) % this.totalFrames;
            this.renderFrame();
        }, 80);
    }

    stopAutoSpin() {
        if (!this.isAutoSpinning) return;
        this.isAutoSpinning = false;
        clearInterval(this.autoSpinTimer);
        if (this.playBtn) {
            this.playBtn.innerHTML = '<i class="fa-solid fa-play"></i> <span style="font-size:12px; color:#fff;">Auto Spin</span>';
        }
    }
}

// Global Factory Helper
function initAsusPhotoSpinner(containerId, imageUrls, hotspots) {
    return new PhotoSpinner360({
        container: containerId,
        images: imageUrls,
        hotspots: hotspots || []
    });
}
