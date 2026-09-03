/**
 * ADHUNIK BARI - CCTV Surveillance Engine
 * Clean, lightweight, professional.
 * - Live security OSD timecode
 * - Matrix layout switcher (1x1, 2x2, Grid)
 * - Single-click Maximize / Fullscreen
 * - Status toggle (Online / Offline)
 * - Add/Edit Stream URL preview tester
 */

(function () {
    'use strict';

    // 1. Live Running OSD Timecode
    function startOsdClock() {
        const timecodeEls = document.querySelectorAll('[data-osd-clock]');
        if (!timecodeEls.length) return;

        function updateClock() {
            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            const day = String(now.getDate()).padStart(2, '0');
            const hours = String(now.getHours()).padStart(2, '0');
            const minutes = String(now.getMinutes()).padStart(2, '0');
            const seconds = String(now.getSeconds()).padStart(2, '0');
            const millis = String(now.getMilliseconds()).padStart(3, '0');

            const formatted = `${year}-${month}-${day} ${hours}:${minutes}:${seconds}.${millis}`;
            timecodeEls.forEach(el => {
                el.textContent = formatted;
            });

            requestAnimationFrame(updateClock);
        }

        requestAnimationFrame(updateClock);
    }

    // 2. Matrix View Switcher (1x1 Focus, 2x2 Quad, Responsive Grid)
    function initMatrixSwitcher() {
        const matrixContainer = document.querySelector('[data-cctv-matrix]');
        const switcherBtns = document.querySelectorAll('[data-matrix-mode]');
        if (!matrixContainer || !switcherBtns.length) return;

        switcherBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const mode = this.getAttribute('data-matrix-mode');

                switcherBtns.forEach(b => b.classList.remove('active'));
                this.classList.add('active');

                matrixContainer.classList.remove('matrix-1x1', 'matrix-2x2', 'matrix-grid');
                matrixContainer.classList.add(`matrix-${mode}`);

                const cards = matrixContainer.querySelectorAll('.cctv-card');
                if (mode === '1x1') {
                    cards.forEach((c, idx) => {
                        if (idx === 0) c.classList.add('focus-active');
                        else c.classList.remove('focus-active');
                    });
                } else {
                    cards.forEach(c => c.classList.remove('focus-active'));
                }
            });
        });
    }

    // 3. Quick Actions (Bottom-Right Maximize, Status Toggle)
    function initQuickActions() {
        // Maximize / Fullscreen Toggle
        document.querySelectorAll('[data-cctv-fullscreen]').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                const card = this.closest('.cctv-card');
                const target = card ? (card.querySelector('.cctv-screen') || card) : (this.closest('.cctv-screen') || this.closest('.cctv-card'));
                if (!target) return;

                if (!document.fullscreenElement) {
                    if (target.requestFullscreen) target.requestFullscreen();
                    else if (target.webkitRequestFullscreen) target.webkitRequestFullscreen();
                } else {
                    if (document.exitFullscreen) document.exitFullscreen();
                }
            });
        });

        // Refresh All Feeds for the whole page
        document.querySelectorAll('[data-cctv-refresh-all]').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const icon = this.querySelector('i');
                if (icon) icon.classList.add('fa-spin');

                const feeds = document.querySelectorAll('.cctv-video-feed');
                const now = Date.now();

                feeds.forEach(media => {
                    const baseSrc = media.getAttribute('data-stream-src') || media.src.split('?')[0];
                    if (!baseSrc) return;
                    const separator = baseSrc.includes('?') ? '&' : '?';
                    media.src = `${baseSrc}${separator}t=${now}`;
                });

                setTimeout(() => {
                    if (icon) icon.classList.remove('fa-spin');
                }, 800);
            });
        });

        function updateWallGridDimensions() {
            const wall = document.querySelector('.cctv-matrix-container');
            if (!wall) return;
            const visibleCards = Array.from(wall.querySelectorAll('.cctv-card')).filter(c => c.offsetParent !== null || c.style.display !== 'none');
            const count = visibleCards.length;

            let cols = 1;
            let rows = 1;

            if (count <= 1) {
                cols = 1;
                rows = 1;
            } else if (count === 2) {
                cols = 2;
                rows = 1;
            } else if (count <= 4) {
                cols = 2;
                rows = 2;
            } else if (count <= 6) {
                cols = 3;
                rows = 2;
            } else if (count <= 9) {
                cols = 3;
                rows = 3;
            } else if (count <= 12) {
                cols = 4;
                rows = 3;
            } else if (count <= 16) {
                cols = 4;
                rows = 4;
            } else {
                cols = Math.ceil(Math.sqrt(count));
                rows = Math.ceil(count / cols);
            }

            wall.style.setProperty('--cctv-wall-cols', cols);
            wall.style.setProperty('--cctv-wall-rows', rows);
        }

        // Fullscreen All / Video Wall View
        document.querySelectorAll('[data-cctv-wall-fullscreen]').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const wall = document.querySelector('.cctv-matrix-container');
                if (!wall) return;

                updateWallGridDimensions();

                if (!document.fullscreenElement) {
                    if (wall.requestFullscreen) {
                        wall.requestFullscreen();
                    } else if (wall.webkitRequestFullscreen) {
                        wall.webkitRequestFullscreen();
                    }
                } else {
                    if (document.exitFullscreen) {
                        document.exitFullscreen();
                    }
                }
            });
        });

        // Fullscreen Change Listener for Video Wall
        document.addEventListener('fullscreenchange', handleFullscreenChange);
        document.addEventListener('webkitfullscreenchange', handleFullscreenChange);

        function handleFullscreenChange() {
            const wall = document.querySelector('.cctv-matrix-container');
            if (!wall) return;

            const isWallFs = document.fullscreenElement === wall;
            if (isWallFs) {
                wall.classList.add('is-wall-fullscreen');
                updateWallGridDimensions();

                if (!wall.querySelector('.cctv-wall-exit-btn')) {
                    const exitBtn = document.createElement('button');
                    exitBtn.className = 'cctv-wall-exit-btn';
                    exitBtn.innerHTML = '<i class="fa-solid fa-compress text-success"></i> Exit Fullscreen (Esc)';
                    exitBtn.addEventListener('click', (e) => {
                        e.stopPropagation();
                        if (document.exitFullscreen) document.exitFullscreen();
                    });
                    wall.appendChild(exitBtn);
                }
            } else {
                wall.classList.remove('is-wall-fullscreen');
                const exitBtn = wall.querySelector('.cctv-wall-exit-btn');
                if (exitBtn) exitBtn.remove();
            }
        }

        // Quick Online / Offline Toggle
        document.querySelectorAll('[data-toggle-status]').forEach(btn => {
            btn.addEventListener('click', async function (e) {
                e.preventDefault();
                const id = this.getAttribute('data-toggle-status');
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

                const formData = new FormData();
                formData.append('id', id);
                formData.append('__RequestVerificationToken', token);

                try {
                    const res = await fetch('/Manager/ToggleCctvStatus', {
                        method: 'POST',
                        body: formData
                    });
                    const data = await res.json();
                    if (data.success) {
                        location.reload();
                    }
                } catch (err) {
                    console.error(err);
                }
            });
        });

        // Async Flicker-Free Camera Deletion
        document.querySelectorAll('form[data-async-delete]').forEach(form => {
            form.addEventListener('submit', async function (e) {
                e.preventDefault();
                if (!confirm('Remove this camera?')) return;

                const card = this.closest('.cctv-card');
                const action = this.getAttribute('action') || form.action;
                const formData = new FormData(this);

                try {
                    const res = await fetch(action, {
                        method: 'POST',
                        body: formData,
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest',
                            'Accept': 'application/json'
                        }
                    });

                    const data = await res.json();
                    if (data.success) {
                        // Smoothly collapse and remove the deleted card with zero screen flicker
                        if (card) {
                            card.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
                            card.style.opacity = '0';
                            card.style.transform = 'scale(0.85)';
                            setTimeout(() => {
                                card.remove();

                                // If no cameras left, reload to show empty state
                                const grid = document.querySelector('.cctv-matrix-container');
                                if (grid && grid.querySelectorAll('.cctv-card').length === 0) {
                                    location.reload();
                                }
                            }, 400);
                        }

                        // Show butter-smooth toast
                        showCctvToast(data.message, 'success');

                        // Update telemetry counters in header
                        if (data.totalCameras !== undefined) {
                            const totalEl = document.querySelector('.cctv-telemetry-item:nth-child(1) .val');
                            if (totalEl) totalEl.textContent = data.totalCameras;
                        }
                        if (data.onlineCount !== undefined) {
                            const onlineEl = document.querySelector('.cctv-telemetry-item:nth-child(2) .val');
                            if (onlineEl) onlineEl.textContent = data.onlineCount;
                        }
                        if (data.offlineCount !== undefined) {
                            const offlineEl = document.querySelector('.cctv-telemetry-item:nth-child(3) .val');
                            if (offlineEl) offlineEl.textContent = data.offlineCount;
                        }
                    } else {
                        showCctvToast(data.message || 'Error removing camera', 'danger');
                    }
                } catch (err) {
                    console.error('Delete error:', err);
                    showCctvToast('Failed to connect to server.', 'danger');
                }
            });
        });
    }

    function showCctvToast(message, type) {
        const container = document.getElementById('cctvToastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `cctv-toast cctv-toast-${type}`;
        toast.setAttribute('data-auto-dismiss', '');
        toast.innerHTML = `
            <div class="d-flex align-items-center gap-2">
                <i class="fa-solid ${type === 'success' ? 'fa-circle-check text-success' : 'fa-triangle-exclamation text-danger'}"></i>
                <span>${message}</span>
            </div>
            <button type="button" class="cctv-toast-close" aria-label="Close">
                <i class="fa-solid fa-xmark"></i>
            </button>
        `;

        toast.querySelector('.cctv-toast-close').addEventListener('click', () => {
            toast.classList.add('fading');
            setTimeout(() => toast.remove(), 500);
        });

        container.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('fading');
            setTimeout(() => toast.remove(), 500);
        }, 3000);
    }

    // 4. Live Test Preview in Add/Edit Camera Modal & Page
    function initUrlTester() {
        document.querySelectorAll('[data-test-url-btn]').forEach(btn => {
            btn.addEventListener('click', function () {
                const container = this.closest('form') || document;
                const urlInput = container.querySelector('[name="StreamUrl"]');
                const previewBox = container.querySelector('[data-url-preview-box]');
                const previewMedia = container.querySelector('[data-url-preview-media]');
                const previewStatus = container.querySelector('[data-url-preview-status]');

                if (!urlInput || !previewBox || !previewMedia) return;

                const url = urlInput.value.trim();
                if (!url) {
                    if (previewStatus) previewStatus.innerHTML = '<span class="text-warning">Please enter a URL first.</span>';
                    return;
                }

                if (previewStatus) previewStatus.innerHTML = '<span class="text-info"><i class="fa-solid fa-spinner fa-spin"></i> Connecting to stream URL...</span>';
                previewBox.style.display = 'block';

                previewMedia.onerror = function () {
                    if (previewStatus) previewStatus.innerHTML = '<span class="text-danger"><i class="fa-solid fa-circle-exclamation"></i> Could not reach stream. Check that the URL is correct and active.</span>';
                };

                previewMedia.onload = function () {
                    if (previewStatus) previewStatus.innerHTML = '<span class="text-success"><i class="fa-solid fa-circle-check"></i> Stream connected successfully!</span>';
                };

                previewMedia.src = url;
            });
        });
    }

    // 5. Automatically remove any browser extension toolbars injected on the video screen
    function suppressExtensionToolbars() {
        function clean() {
            // Remove any unknown element injected inside .cctv-screen
            document.querySelectorAll('.cctv-screen').forEach(screen => {
                Array.from(screen.children).forEach(child => {
                    if (!child.classList.contains('cctv-video-feed') &&
                        !child.classList.contains('cctv-scanlines') &&
                        !child.classList.contains('cctv-vignette') &&
                        !child.classList.contains('cctv-screen-shield') &&
                        !child.classList.contains('cctv-osd-top') &&
                        !child.classList.contains('cctv-osd-bottom')) {
                        child.remove();
                    }
                });
            });

            // Target known extension toolbar selectors
            const badSelectors = [
                '[class*="rtvp"]', '[id*="rtvp"]',
                '[class*="rotate"]', '[id*="rotate"]',
                '[class*="flip"]', '[id*="flip"]',
                '[class*="grugru"]', '[id*="grugru"]',
                '[class*="video-rotate"]', '[class*="image-control"]'
            ];
            document.querySelectorAll(badSelectors.join(',')).forEach(el => {
                if (!el.closest('.cctv-matrix-switcher') && !el.classList.contains('fa-rotate')) {
                    el.remove();
                }
            });
        }

        clean();
        setInterval(clean, 150);

        const observer = new MutationObserver(() => clean());
        document.querySelectorAll('.cctv-screen').forEach(screen => {
            observer.observe(screen, { childList: true, subtree: true });
        });
    }

    // 6. Butter-smooth auto-fadeout for alert messages
    function initAlertFadeout() {
        const alerts = document.querySelectorAll('[data-auto-dismiss], .cctv-toast');
        alerts.forEach(toast => {
            setTimeout(() => {
                toast.classList.add('fading');
                setTimeout(() => {
                    toast.remove();
                }, 500);
            }, 3000);
        });
    }

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', () => {
        startOsdClock();
        initMatrixSwitcher();
        initQuickActions();
        initUrlTester();
        suppressExtensionToolbars();
        initAlertFadeout();
    });
})();
