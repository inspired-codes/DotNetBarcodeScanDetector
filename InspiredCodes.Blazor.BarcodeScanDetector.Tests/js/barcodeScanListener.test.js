import { keyText, createBatcher, start, stop, now, SILENCE_MS } from "../../InspiredCodes.Blazor.BarcodeScanDetector/wwwroot/barcodeScanListener.js";

const tests = [];
const test = (name, body) => tests.push([name, body]);

function eq(actual, expected, what = "") {
    const a = JSON.stringify(actual), e = JSON.stringify(expected);
    if (a !== e)
        throw new Error(`${what ? what + ": " : ""}expected ${e}, got ${a}`);
}

const key = (name, init = {}) => new KeyboardEvent("keydown", { key: name, bubbles: true, cancelable: true, ...init });
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));

/** setTimeout/clearTimeout stand-ins that only run the timer when told to */
function fakeTimers() {
    let pending = null, lastId = 0;
    return {
        set: (fn, ms) => { pending = { fn, ms, id: ++lastId }; return lastId; },
        clear: id => { if (pending && pending.id === id) pending = null; },
        fire: () => { const p = pending; pending = null; if (p) p.fn(); },
        get pending() { return pending; },
    };
}

function batcher(timers) {
    const sent = [];
    const b = createBatcher((texts, timestamps) => sent.push([texts, timestamps]), SILENCE_MS, timers.set, timers.clear);
    return { b, sent };
}

/** a stand-in for the DotNetObjectReference */
function dotNetRef(result = () => Promise.resolve()) {
    const calls = [];
    return { calls, invokeMethodAsync: (name, texts, timestamps) => { calls.push([name, texts, timestamps]); return result(); } };
}

// --- keyText -----------------------------------------------------------------------------------

test("printable keys type their character", () => {
    for (const k of ["a", "Z", "7", " ", "-", "/", "ä"])
        eq(keyText(key(k)), k, k);
});

test("Enter types \\r, Tab types \\t", () => {
    eq(keyText(key("Enter")), "\r");
    eq(keyText(key("Tab")), "\t");
});

test("keys that type nothing give null", () => {
    for (const k of ["Shift", "Control", "Alt", "AltGraph", "ArrowLeft", "Backspace", "Escape", "F5", "Dead", "Unidentified"])
        eq(keyText(key(k)), null, k);
});

test("Ctrl and Cmd shortcuts type nothing", () => {
    eq(keyText(key("c", { ctrlKey: true })), null, "Ctrl+C");
    eq(keyText(key("v", { metaKey: true })), null, "Cmd+V");
});

test("AltGr characters are typed (reported as Ctrl+Alt on Windows)", () => {
    eq(keyText(key("@", { ctrlKey: true, altKey: true, modifierAltGraph: true })), "@");
});

test("auto-repeat and IME composition type nothing", () => {
    eq(keyText(key("a", { repeat: true })), null, "repeat");
    eq(keyText(key("a", { isComposing: true })), null, "composing");
});

test("a character outside the BMP is one key", () => {
    eq(keyText(key("😀")), "😀");
});

// --- createBatcher -----------------------------------------------------------------------------

test("Enter sends the batch at once, with the timestamps", () => {
    const timers = fakeTimers(), { b, sent } = batcher(timers);
    b.push("A", 1.5);
    b.push("B", 2.5);
    eq(sent, [], "before Enter");
    b.push("\r", 3.5);
    eq(sent, [[["A", "B", "\r"], [1.5, 2.5, 3.5]]]);
    eq(timers.pending, null, "no timer left");
});

test("Tab sends the batch at once", () => {
    const timers = fakeTimers(), { b, sent } = batcher(timers);
    b.push("A", 1);
    b.push("\t", 2);
    eq(sent, [[["A", "\t"], [1, 2]]]);
});

test("other keys are sent after SILENCE_MS without a key", () => {
    const timers = fakeTimers(), { b, sent } = batcher(timers);
    b.push("A", 1);
    b.push("B", 2);
    eq(sent, []);
    eq(timers.pending.ms, 100, "silence");
    timers.fire();
    eq(sent, [[["A", "B"], [1, 2]]]);
});

test("every key restarts the silence", () => {
    const timers = fakeTimers(), { b, sent } = batcher(timers);
    b.push("A", 1);
    const first = timers.pending;
    b.push("B", 2);
    if (timers.pending === first)
        throw new Error("the timer was not restarted");
    timers.fire();
    eq(sent.length, 1, "one batch");
});

test("discard drops the pending keys", () => {
    const timers = fakeTimers(), { b, sent } = batcher(timers);
    b.push("A", 1);
    b.discard();
    eq(timers.pending, null, "timer cancelled");
    b.flush();
    eq(sent, []);
});

// --- start / stop ------------------------------------------------------------------------------

test("start sends the keys typed on the target to ProcessKeys", () => {
    const target = document.createElement("div"), ref = dotNetRef();
    const id = start(ref, target);
    const events = ["A", "B", "Enter"].map(k => key(k));
    events.forEach(e => target.dispatchEvent(e));
    stop(id);
    eq(ref.calls.length, 1, "calls");
    eq(ref.calls[0][0], "ProcessKeys");
    eq(ref.calls[0][1], ["A", "B", "\r"]);
    eq(ref.calls[0][2], events.map(e => e.timeStamp), "event.timeStamp");
});

test("the keys still reach the page: nothing is prevented or stopped", () => {
    const parent = document.createElement("div"), child = document.createElement("input");
    parent.appendChild(child);
    let reachedParent = false;
    parent.addEventListener("keydown", () => reachedParent = true);
    const id = start(dotNetRef(), parent);
    const e = key("A");
    child.dispatchEvent(e);
    stop(id);
    eq(e.defaultPrevented, false, "defaultPrevented");
    eq(reachedParent, true, "bubbled on");
});

test("the listener runs in the capture phase, before the element's own handlers", () => {
    const parent = document.createElement("div"), child = document.createElement("input");
    parent.appendChild(child);
    child.addEventListener("keydown", e => e.stopPropagation());   // a control that swallows the key
    const ref = dotNetRef();
    const id = start(ref, parent);
    child.dispatchEvent(key("A"));
    child.dispatchEvent(key("Enter"));
    stop(id);
    eq(ref.calls.map(c => c[1]), [["A", "\r"]]);
});

test("stop removes the listener", () => {
    const target = document.createElement("div"), ref = dotNetRef();
    const id = start(ref, target);
    stop(id);
    target.dispatchEvent(key("A"));
    target.dispatchEvent(key("Enter"));
    eq(ref.calls, []);
    stop(id);   // a second stop does nothing
});

test("two listeners are independent", () => {
    const target = document.createElement("div"), first = dotNetRef(), second = dotNetRef();
    const firstId = start(first, target), secondId = start(second, target);
    stop(firstId);
    target.dispatchEvent(key("A"));
    target.dispatchEvent(key("Enter"));
    stop(secondId);
    eq(first.calls.length, 0, "first");
    eq(second.calls.length, 1, "second");
});

test("keys without a terminator arrive after the silence (real timers)", async () => {
    const target = document.createElement("div"), ref = dotNetRef();
    const id = start(ref, target);
    target.dispatchEvent(key("A"));
    eq(ref.calls.length, 0, "at once");
    await delay(SILENCE_MS + 50);
    stop(id);
    eq(ref.calls.map(c => c[1]), [["A"]]);
});

test("stop drops keys not yet sent (real timers)", async () => {
    const target = document.createElement("div"), ref = dotNetRef();
    const id = start(ref, target);
    target.dispatchEvent(key("A"));
    stop(id);
    await delay(SILENCE_MS + 50);
    eq(ref.calls, []);
});

test("a failing .NET call does not become an unhandled rejection", async () => {
    let unhandled = 0;
    const onUnhandled = () => unhandled++;
    window.addEventListener("unhandledrejection", onUnhandled);
    const target = document.createElement("div");
    const id = start(dotNetRef(() => Promise.reject(new Error("disposed"))), target);
    target.dispatchEvent(key("A"));
    target.dispatchEvent(key("Enter"));
    stop(id);
    await delay(50);
    window.removeEventListener("unhandledrejection", onUnhandled);
    eq(unhandled, 0);
});

test("now() uses the same clock as event.timeStamp", () => {
    const before = now();
    const e = key("A");
    const after = now();
    if (!(before <= e.timeStamp && e.timeStamp <= after))
        throw new Error(`event.timeStamp ${e.timeStamp} is not between ${before} and ${after}`);
});

// --- run ---------------------------------------------------------------------------------------

const lines = [];
let failed = 0;
for (const [name, body] of tests) {
    try {
        await body();
        lines.push(`PASS  ${name}`);
    } catch (error) {
        failed++;
        lines.push(`FAIL  ${name}: ${error.message}`);
    }
}
lines.push(failed === 0 ? `RESULT: ALL ${tests.length} PASSED` : `RESULT: ${failed} OF ${tests.length} FAILED`);

const results = document.getElementById("results");
results.textContent = "";
for (const line of lines) {
    const div = document.createElement("div");
    div.textContent = line;
    results.appendChild(div);
}
