// Sends the page's keystrokes to .NET in batches, each with the browser's event.timeStamp
// (milliseconds since the page's time origin, the same clock as performance.now()), so the
// detector measures the real gaps between keys, however late the batch arrives.

/** how long to wait after a key that is not a terminator before sending the batch */
export const SILENCE_MS = 100;

/**
 * The text a keydown event types: the character, "\r" for Enter, "\t" for Tab, or null for keys
 * that type nothing (modifiers, arrows, shortcuts, auto-repeat, IME composition).
 */
export function keyText(e) {
    if (e.isComposing || e.repeat)
        return null;
    if (e.key === "Enter")
        return "\r";
    if (e.key === "Tab")
        return "\t";
    // a printable key's name is the character itself (one code point); others have names like "Shift"
    if (typeof e.key !== "string" || [...e.key].length !== 1)
        return null;
    // Ctrl/Cmd combinations are shortcuts, except AltGr (reported as Ctrl+Alt on Windows), which types characters
    const altGraph = typeof e.getModifierState === "function" && e.getModifierState("AltGraph");
    if ((e.ctrlKey || e.metaKey) && !altGraph)
        return null;
    return e.key;
}

/**
 * Collects keys and hands them to send(texts, timestamps): at once after Enter or Tab (a possible
 * terminator, so a scan is reported without delay), otherwise after silenceMs without a key.
 */
export function createBatcher(send, silenceMs = SILENCE_MS, setTimer = setTimeout, clearTimer = clearTimeout) {
    let texts = [];
    let timestamps = [];
    let timer = null;

    function cancelTimer() {
        if (timer !== null) {
            clearTimer(timer);
            timer = null;
        }
    }

    function flush() {
        cancelTimer();
        if (texts.length === 0)
            return;
        const batch = [texts, timestamps];
        texts = [];
        timestamps = [];
        send(batch[0], batch[1]);
    }

    function push(text, timestamp) {
        texts.push(text);
        timestamps.push(timestamp);
        if (text === "\r" || text === "\t") {
            flush();
            return;
        }
        cancelTimer();
        timer = setTimer(flush, silenceMs);
    }

    /** forgets the pending keys without sending them */
    function discard() {
        cancelTimer();
        texts = [];
        timestamps = [];
    }

    return { push, flush, discard };
}

const listeners = new Map();
let nextListenerId = 1;

/**
 * Starts listening to keydown on the whole document, in the capture phase so that no element can
 * stop it first, and sends the keys to dotNetRef.ProcessKeys(texts, timestamps).
 * The keys still reach the focused element. Returns an id for stop().
 */
export function start(dotNetRef, target = document) {
    const batcher = createBatcher((texts, timestamps) =>
        // a rejected call (e.g. the .NET side was disposed meanwhile) must not become an unhandled rejection
        dotNetRef.invokeMethodAsync("ProcessKeys", texts, timestamps).catch(() => { }));

    const onKeyDown = e => {
        const text = keyText(e);
        if (text !== null)
            batcher.push(text, e.timeStamp);
    };
    target.addEventListener("keydown", onKeyDown, { capture: true });

    const id = nextListenerId++;
    listeners.set(id, () => {
        target.removeEventListener("keydown", onKeyDown, { capture: true });
        batcher.discard();
    });
    return id;
}

/** stops a listener started with start(); keys not yet sent are dropped */
export function stop(id) {
    const stopListener = listeners.get(id);
    if (stopListener) {
        listeners.delete(id);
        stopListener();
    }
}

/** the current time in the same time base as event.timeStamp */
export function now() {
    return performance.now();
}
