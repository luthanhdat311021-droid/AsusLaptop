/* ═════════════════════════════════════════════════════════════════════════════
   ASUS ROG ULTRA-MINIMALIST 3D WEBGL ENGINE & BENTO TILT
   ═════════════════════════════════════════════════════════════════════════════ */

class AsusMinimalist3DEngine {
    constructor() {
        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.laptopGroup = null;
        
        this.isDragging = false;
        this.previousMousePosition = { x: 0, y: 0 };
        this.targetRotation = { x: 0.15, y: -0.35 };
        this.currentRotation = { x: 0.15, y: -0.35 };

        this.init();
    }

    init() {
        document.addEventListener("DOMContentLoaded", () => {
            this.initThreeJS();
            this.initBentoTilt();
            this.animate();
        });
    }

    initThreeJS() {
        const container = document.getElementById("hero-3d-container");
        if (!container) return;

        // 1. Scene & Camera
        this.scene = new THREE.Scene();
        const width = container.clientWidth || window.innerWidth;
        const height = container.clientHeight || window.innerHeight;

        this.camera = new THREE.PerspectiveCamera(42, width / height, 0.1, 100);
        this.camera.position.set(0, 1.0, 4.2);

        // 2. Renderer
        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        this.renderer.setSize(width, height);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1.1;

        container.appendChild(this.renderer.domElement);
        this.renderer.domElement.className = "hero-webgl-canvas";

        // 3. Lighting
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.9);
        this.scene.add(ambientLight);

        const keyLight = new THREE.DirectionalLight(0xffffff, 2.5);
        keyLight.position.set(5, 8, 5);
        this.scene.add(keyLight);

        const rimRed = new THREE.PointLight(0xff0055, 6, 8);
        rimRed.position.set(-3, 2, -1);
        this.scene.add(rimRed);

        const rimCyan = new THREE.PointLight(0x00f0ff, 4, 8);
        rimCyan.position.set(3, -1, 2);
        this.scene.add(rimCyan);

        // 4. Construct Sleek 3D Laptop Mesh
        this.createLaptop();

        // Resize Listener
        window.addEventListener("resize", () => {
            const w = container.clientWidth || window.innerWidth;
            const h = container.clientHeight || window.innerHeight;
            this.camera.aspect = w / h;
            this.camera.updateProjectionMatrix();
            this.renderer.setSize(w, h);
        });

        // Interactive Drag Controls
        const canvas = this.renderer.domElement;
        canvas.addEventListener("mousedown", (e) => {
            this.isDragging = true;
            this.previousMousePosition = { x: e.clientX, y: e.clientY };
        });

        window.addEventListener("mouseup", () => this.isDragging = false);

        canvas.addEventListener("mousemove", (e) => {
            if (!this.isDragging) return;
            const deltaX = e.clientX - this.previousMousePosition.x;
            const deltaY = e.clientY - this.previousMousePosition.y;

            this.targetRotation.y += deltaX * 0.008;
            this.targetRotation.x += deltaY * 0.008;
            this.targetRotation.x = Math.max(-0.4, Math.min(0.6, this.targetRotation.x));

            this.previousMousePosition = { x: e.clientX, y: e.clientY };
        });
    }

    createLaptop() {
        this.laptopGroup = new THREE.Group();

        const darkMetalMat = new THREE.MeshStandardMaterial({
            color: 0x121218,
            roughness: 0.3,
            metalness: 0.85
        });

        // Base Chassis
        const baseGeo = new THREE.BoxGeometry(2.4, 0.12, 1.6);
        const baseMesh = new THREE.Mesh(baseGeo, darkMetalMat);
        this.laptopGroup.add(baseMesh);

        // Keyboard Surface
        const kbGeo = new THREE.BoxGeometry(2.1, 0.02, 0.9);
        const kbMat = new THREE.MeshStandardMaterial({ color: 0x08080c, roughness: 0.5 });
        const kbMesh = new THREE.Mesh(kbGeo, kbMat);
        kbMesh.position.set(0, 0.07, -0.2);
        baseMesh.add(kbMesh);

        // Screen Lid
        const lidGroup = new THREE.Group();
        lidGroup.position.set(0, 0.06, -0.8);

        const lidBackGeo = new THREE.BoxGeometry(2.4, 1.5, 0.06);
        const lidBackMesh = new THREE.Mesh(lidBackGeo, darkMetalMat);
        lidBackMesh.position.set(0, 0.75, 0);
        lidGroup.add(lidBackMesh);

        // Screen Panel Display Texture
        const screenCanvas = document.createElement("canvas");
        screenCanvas.width = 1024;
        screenCanvas.height = 640;
        const ctx = screenCanvas.getContext("2d");
        ctx.fillStyle = "#0a0a0f";
        ctx.fillRect(0, 0, 1024, 640);
        ctx.fillStyle = "#ff0055";
        ctx.font = "bold 52px sans-serif";
        ctx.fillText("REPUBLIC OF GAMERS", 80, 140);
        ctx.fillStyle = "#00f0ff";
        ctx.font = "28px monospace";
        ctx.fillText("ASUS FLAGSHIP 3D // RTX 4090 175W", 80, 210);

        const screenTexture = new THREE.CanvasTexture(screenCanvas);
        const screenMat = new THREE.MeshBasicMaterial({ map: screenTexture });
        const screenGeo = new THREE.PlaneGeometry(2.3, 1.4);
        const screenMesh = new THREE.Mesh(screenGeo, screenMat);
        screenMesh.position.set(0, 0.75, 0.032);
        lidGroup.add(screenMesh);

        // Open Lid Angle
        lidGroup.rotation.x = -Math.PI * 0.6;
        this.laptopGroup.add(lidGroup);

        this.scene.add(this.laptopGroup);
    }

    initBentoTilt() {
        const cards = document.querySelectorAll(".bento-card");
        cards.forEach(card => {
            card.addEventListener("mousemove", (e) => {
                const rect = card.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;

                const rotateX = ((y - rect.height / 2) / rect.height) * -10;
                const rotateY = ((x - rect.width / 2) / rect.width) * 10;

                card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.02, 1.02, 1.02)`;
            });

            card.addEventListener("mouseleave", () => {
                card.style.transform = "perspective(1000px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1)";
            });
        });
    }

    animate() {
        requestAnimationFrame(() => this.animate());

        if (this.laptopGroup) {
            if (!this.isDragging) {
                this.targetRotation.y += 0.003;
            }

            this.currentRotation.x += (this.targetRotation.x - this.currentRotation.x) * 0.05;
            this.currentRotation.y += (this.targetRotation.y - this.currentRotation.y) * 0.05;

            this.laptopGroup.rotation.x = this.currentRotation.x;
            this.laptopGroup.rotation.y = this.currentRotation.y;
        }

        if (this.renderer && this.scene && this.camera) {
            this.renderer.render(this.scene, this.camera);
        }
    }
}

window.asus3DEngine = new AsusMinimalist3DEngine();
