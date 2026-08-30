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
})();
