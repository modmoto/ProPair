// Long-press (touch) and right-click (mouse) on matrix cells → opens the cell sheet in Blazor.
// Cells carry data-own / data-opp. A long press must not also open the cell's native <select>.
window.proPair = window.proPair || {};

window.proPair.longPress = (root, dotnet) => {
    const holdMillis = 500;
    let timer = null;
    let start = null;
    let firedAt = 0;

    const cellOf = e => e.target.closest('td[data-own]');
    const fire = cell => {
        if (Date.now() - firedAt < 800) return; // Android sends contextmenu after our own timer
        firedAt = Date.now();
        navigator.vibrate?.(15);
        dotnet.invokeMethodAsync('OnCellLongPress', cell.dataset.own, cell.dataset.opp);
    };
    const cancel = () => { clearTimeout(timer); timer = null; };

    root.addEventListener('contextmenu', e => {
        const cell = cellOf(e);
        if (!cell) return;
        e.preventDefault();
        fire(cell);
    });

    root.addEventListener('touchstart', e => {
        const cell = cellOf(e);
        if (!cell) return;
        const t = e.touches[0];
        start = { x: t.clientX, y: t.clientY };
        cancel();
        timer = setTimeout(() => fire(cell), holdMillis);
    }, { passive: true });

    root.addEventListener('touchmove', e => {
        if (!timer || !start) return;
        const t = e.touches[0];
        if (Math.hypot(t.clientX - start.x, t.clientY - start.y) > 10) cancel(); // scrolling, not holding
    }, { passive: true });

    root.addEventListener('touchend', e => {
        cancel();
        if (Date.now() - firedAt < 800) e.preventDefault(); // swallow the click → select stays closed
    }, { passive: false });

    root.addEventListener('touchcancel', cancel);
};

// Brings a freshly opened panel into view (it opens below the matrix, often off-screen on phones).
window.proPair.reveal = el => el?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
