(() => {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const siteNav = document.querySelector('[data-site-nav]');

    const updateNavigation = () => siteNav?.classList.toggle('is-scrolled', window.scrollY > 24);
    updateNavigation();
    window.addEventListener('scroll', updateNavigation, { passive: true });

    const revealItems = document.querySelectorAll('[data-reveal]');
    if (prefersReducedMotion || !('IntersectionObserver' in window)) {
        revealItems.forEach(item => item.classList.add('is-visible'));
    } else {
        const revealObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -40px' });
        revealItems.forEach((item, index) => {
            item.style.transitionDelay = `${Math.min(index % 4, 3) * 70}ms`;
            revealObserver.observe(item);
        });
    }

    document.querySelectorAll('[data-audience]').forEach(button => {
        button.addEventListener('click', () => {
            const audience = button.dataset.audience;
            document.querySelectorAll('[data-audience]').forEach(item => {
                const selected = item === button;
                item.classList.toggle('active', selected);
                item.setAttribute('aria-selected', selected.toString());
            });
            document.querySelectorAll('[data-audience-panel]').forEach(panel => {
                const selected = panel.dataset.audiencePanel === audience;
                panel.hidden = !selected;
                panel.classList.toggle('active', selected);
            });
        });
    });

    if (!prefersReducedMotion && window.matchMedia('(pointer: fine)').matches) {
        document.querySelectorAll('[data-tilt]').forEach(card => {
            card.addEventListener('pointermove', event => {
                const bounds = card.getBoundingClientRect();
                const rotateX = ((event.clientY - bounds.top) / bounds.height - .5) * -5;
                const rotateY = ((event.clientX - bounds.left) / bounds.width - .5) * 6;
                card.style.transform = `perspective(1100px) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
            });
            card.addEventListener('pointerleave', () => { card.style.transform = ''; });
        });
    }

    document.querySelectorAll('[data-password-toggle]').forEach(toggle => {
        toggle.addEventListener('click', () => {
            const input = toggle.parentElement?.querySelector('input');
            if (!input) return;
            const shouldShow = input.type === 'password';
            input.type = shouldShow ? 'text' : 'password';
            toggle.setAttribute('aria-label', shouldShow ? 'Hide password' : 'Show password');
            toggle.querySelector('i')?.classList.toggle('fa-eye', !shouldShow);
            toggle.querySelector('i')?.classList.toggle('fa-eye-slash', shouldShow);
        });
    });

    document.getElementById('loginForm')?.addEventListener('submit', () => {
        const label = document.getElementById('loginText');
        const spinner = document.getElementById('spinner');
        if (label) label.textContent = 'Signing in…';
        if (spinner) spinner.style.display = 'inline-block';
    });

    document.querySelectorAll('#navbarMenu a[href*="#"]').forEach(link => {
        link.addEventListener('click', () => {
            const menu = document.getElementById('navbarMenu');
            if (menu?.classList.contains('show') && window.bootstrap?.Collapse) {
                window.bootstrap.Collapse.getOrCreateInstance(menu).hide();
            }
        });
    });

    const appShell = document.querySelector('[data-app-shell]');
    const sidebarToggle = document.querySelector('[data-sidebar-toggle]');
    const setSidebar = open => {
        if (!appShell) return;
        appShell.classList.toggle('sidebar-open', open);
        sidebarToggle?.setAttribute('aria-expanded', open.toString());
        document.body.style.overflow = open ? 'hidden' : '';
    };
    sidebarToggle?.addEventListener('click', () => setSidebar(!appShell?.classList.contains('sidebar-open')));
    document.querySelectorAll('[data-sidebar-close]').forEach(item => item.addEventListener('click', () => setSidebar(false)));
    document.querySelectorAll('[data-app-sidebar] a').forEach(link => link.addEventListener('click', () => {
        if (window.innerWidth < 1200) setSidebar(false);
    }));
    window.addEventListener('resize', () => { if (window.innerWidth >= 1200) setSidebar(false); });

    const animateCounter = element => {
        const target = Number.parseInt(element.dataset.counter ?? element.textContent ?? '0', 10);
        if (!Number.isFinite(target) || prefersReducedMotion) return;
        const duration = 900;
        const startTime = performance.now();
        const update = now => {
            const progress = Math.min((now - startTime) / duration, 1);
            element.textContent = Math.round(target * (1 - Math.pow(1 - progress, 3))).toString();
            if (progress < 1) requestAnimationFrame(update);
        };
        requestAnimationFrame(update);
    };
    document.querySelectorAll('[data-counter]').forEach(animateCounter);

    document.querySelectorAll('[data-ring-value]').forEach(ring => {
        ring.style.setProperty('--value', ring.dataset.ringValue ?? '0');
    });
    document.querySelectorAll('[data-progress-value]').forEach(progress => {
        progress.style.width = `${progress.dataset.progressValue ?? 0}%`;
    });

    document.querySelectorAll('[data-confirm]').forEach(trigger => {
        trigger.addEventListener('click', event => {
            if (!window.confirm(trigger.dataset.confirm)) event.preventDefault();
        });
    });

    document.querySelectorAll('[data-print-page]').forEach(trigger => {
        trigger.addEventListener('click', () => window.print());
    });

    const noticeType = document.querySelector('[data-notice-type]');
    const noticeTargets = document.querySelector('[data-notice-targets]');
    const updateNoticeTargets = () => {
        if (!noticeType || !noticeTargets) return;
        const visible = noticeType.value !== 'General';
        noticeTargets.hidden = !visible;
        noticeTargets.setAttribute('aria-hidden', (!visible).toString());
    };
    noticeType?.addEventListener('change', updateNoticeTargets);
    updateNoticeTargets();

    const cinematicHero = document.querySelector('[data-cinematic-hero]');
    if (cinematicHero) {
        const slides = [...cinematicHero.querySelectorAll('[data-story-slide]')];
        const dots = [...cinematicHero.querySelectorAll('[data-story-dot]')];
        const back = cinematicHero.querySelector('[data-story-back]');
        const next = cinematicHero.querySelector('[data-story-next]');
        const nextLabel = cinematicHero.querySelector('[data-story-next-label]');
        const currentLabel = cinematicHero.querySelector('[data-story-current]');
        const progress = cinematicHero.querySelector('[data-story-progress]');
        const depthScene = cinematicHero.querySelector('[data-depth-scene]');
        const motionToggle = cinematicHero.querySelector('[data-motion-toggle]');
        const motionToggleLabel = cinematicHero.querySelector('[data-motion-toggle-label]');
        let storyIndex = 0;
        let depthMotionPaused = prefersReducedMotion;

        const showStory = index => {
            storyIndex = (index + slides.length) % slides.length;
            slides.forEach((slide, slideIndex) => {
                const selected = slideIndex === storyIndex;
                slide.hidden = !selected;
                slide.classList.toggle('is-active', selected);
            });
            dots.forEach((dot, dotIndex) => {
                const selected = dotIndex === storyIndex;
                dot.classList.toggle('is-active', selected);
                dot.setAttribute('aria-selected', selected.toString());
            });
            cinematicHero.dataset.stage = storyIndex.toString();
            if (currentLabel) currentLabel.textContent = String(storyIndex + 1).padStart(2, '0');
            if (progress) progress.style.width = `${((storyIndex + 1) / slides.length) * 100}%`;
            if (back) back.disabled = storyIndex === 0;
            if (nextLabel) nextLabel.textContent = storyIndex === slides.length - 1 ? 'Start again' : 'Next answer';
        };

        back?.addEventListener('click', () => showStory(Math.max(0, storyIndex - 1)));
        next?.addEventListener('click', () => showStory(storyIndex + 1));
        dots.forEach((dot, index) => dot.addEventListener('click', () => showStory(index)));
        cinematicHero.addEventListener('keydown', event => {
            if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) return;
            if (event.key === 'ArrowLeft') { event.preventDefault(); showStory(Math.max(0, storyIndex - 1)); }
            if (event.key === 'ArrowRight') { event.preventDefault(); showStory(storyIndex + 1); }
        });
        const resetDepth = () => {
            if (!depthScene) return;
            depthScene.style.setProperty('--depth-x', '0px');
            depthScene.style.setProperty('--depth-y', '0px');
            depthScene.style.setProperty('--depth-rx', '0deg');
            depthScene.style.setProperty('--depth-ry', '0deg');
            depthScene.style.setProperty('--depth-fg-x', '0px');
            depthScene.style.setProperty('--depth-fg-y', '0px');
        };
        const setDepthMotionState = paused => {
            depthMotionPaused = paused;
            cinematicHero.classList.toggle('is-motion-paused', paused);
            if (paused) resetDepth();
            if (!motionToggle) return;
            motionToggle.setAttribute('aria-pressed', paused.toString());
            motionToggle.setAttribute('aria-label', paused ? 'Play depth motion' : 'Pause depth motion');
            motionToggle.querySelector('i')?.classList.toggle('fa-play', paused);
            motionToggle.querySelector('i')?.classList.toggle('fa-pause', !paused);
            if (motionToggleLabel) motionToggleLabel.textContent = paused ? 'Play depth' : 'Pause depth';
        };
        if (depthScene && window.matchMedia('(pointer: fine)').matches) {
            cinematicHero.addEventListener('pointermove', event => {
                if (depthMotionPaused) return;
                const bounds = cinematicHero.getBoundingClientRect();
                const x = Math.max(-1, Math.min(1, ((event.clientX - bounds.left) / bounds.width - .5) * 2));
                const y = Math.max(-1, Math.min(1, ((event.clientY - bounds.top) / bounds.height - .5) * 2));
                depthScene.style.setProperty('--depth-x', `${x * -9}px`);
                depthScene.style.setProperty('--depth-y', `${y * -6}px`);
                depthScene.style.setProperty('--depth-rx', `${y * -1.2}deg`);
                depthScene.style.setProperty('--depth-ry', `${x * 1.8}deg`);
                depthScene.style.setProperty('--depth-fg-x', `${x * -16}px`);
                depthScene.style.setProperty('--depth-fg-y', `${y * -10}px`);
            });
            cinematicHero.addEventListener('pointerleave', resetDepth);
        }
        motionToggle?.addEventListener('click', () => setDepthMotionState(!depthMotionPaused));
        setDepthMotionState(depthMotionPaused);
        showStory(0);
    }

    if (!prefersReducedMotion && window.matchMedia('(pointer: fine)').matches) {
        document.querySelectorAll('[data-spotlight]').forEach(card => {
            card.addEventListener('pointermove', event => {
                const bounds = card.getBoundingClientRect();
                card.style.setProperty('--spot-x', `${event.clientX - bounds.left}px`);
                card.style.setProperty('--spot-y', `${event.clientY - bounds.top}px`);
            });
        });
    }

    document.querySelectorAll('[data-coverflow]').forEach(coverflow => {
        const cards = [...coverflow.querySelectorAll('[data-coverflow-card]')];
        const dots = [...coverflow.querySelectorAll('[data-coverflow-dot]')];
        const previous = coverflow.querySelector('[data-coverflow-prev]');
        const next = coverflow.querySelector('[data-coverflow-next]');
        if (!cards.length) return;

        let currentIndex = 0;
        let touchStartX = 0;
        const renderCoverflow = index => {
            currentIndex = (index + cards.length) % cards.length;
            cards.forEach((card, cardIndex) => {
                let offset = cardIndex - currentIndex;
                if (offset > cards.length / 2) offset -= cards.length;
                if (offset < -cards.length / 2) offset += cards.length;
                const position = Math.max(-2, Math.min(2, offset));
                card.dataset.position = position.toString();
                card.classList.toggle('is-active', position === 0);
                card.setAttribute('aria-hidden', (position !== 0).toString());
                card.querySelectorAll('a, button').forEach(item => { item.tabIndex = position === 0 ? 0 : -1; });
            });
            dots.forEach((dot, dotIndex) => {
                const selected = dotIndex === currentIndex;
                dot.classList.toggle('is-active', selected);
                dot.setAttribute('aria-selected', selected.toString());
            });
        };
        previous?.addEventListener('click', () => renderCoverflow(currentIndex - 1));
        next?.addEventListener('click', () => renderCoverflow(currentIndex + 1));
        dots.forEach((dot, index) => dot.addEventListener('click', () => renderCoverflow(index)));
        cards.forEach((card, index) => card.addEventListener('click', event => {
            if (index === currentIndex || event.target.closest('a')) return;
            renderCoverflow(index);
        }));
        coverflow.addEventListener('touchstart', event => { touchStartX = event.touches[0].clientX; }, { passive: true });
        coverflow.addEventListener('touchend', event => {
            const distance = event.changedTouches[0].clientX - touchStartX;
            if (Math.abs(distance) > 45) renderCoverflow(currentIndex + (distance < 0 ? 1 : -1));
        }, { passive: true });
        renderCoverflow(0);
    });
})();
