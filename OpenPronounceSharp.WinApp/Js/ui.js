/**
 * OpenPronounce web demo.
 *
 * One page, three states: practice (record / upload), loading, result (or error).
 * All server calls live here; audio.js only records, viseme.js only animates the mouth.
 */

'use strict';

const $ = (id) => document.getElementById(id);

// Same tokenization as the server (\b[\w']+\b with Unicode word characters)
const WORD_RE = /[\p{L}\p{N}_](?:[\p{L}\p{N}_']*[\p{L}\p{N}_])?/gu;
const RING_LENGTH = 2 * Math.PI * 62;
const PHONE_WRONG_THRESHOLD = 0.5;
const COLORS = { you: '#ea580c', reference: '#2f6fb3', good: '#10b981', mid: '#f59e0b', bad: '#ef4444' };

const state = {
    lang: 'en',
    result: null,
    words: [],
    errorsByIndex: new Map(),
    selected: null,
    charts: {},
    chartsDirty: false
};
let viseme;

document.addEventListener('DOMContentLoaded', init);

function init() {
    viseme = new Viseme($('viseme-image'));

    $('detail-listen').addEventListener('click', () => {
        if (state.selected !== null) {
            speakWord(state.selected);
        }
    });
    $('details').addEventListener('toggle', () => {
        if ($('details').open && state.chartsDirty) {
            renderCharts();
        }
    });
}

function tokenize(text) {
    return text.match(WORD_RE) || [];
}

function setView(name) {
    $('error').classList.toggle('hidden', name !== 'error');
    $('result').classList.toggle('hidden', name !== 'result');

    $('error').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

// ---------------------------------------------------------------- result

function hideResult() {
    $("result").style.display = 'none';
    $("details").removeAttribute('open');   
}

function renderResult(data, expectedText) {

    data = JSON.parse(decodeURIComponent(data));
    expectedText = decodeURIComponent(expectedText);

    state.result = data;
    state.words = tokenize(expectedText);
    state.selected = null;

    const errors = (data.differences.errors || []).filter(e => e && e.word !== '');
    state.errorsByIndex = new Map();
    errors.forEach(e => {
        if (typeof e.position === 'number' && !state.errorsByIndex.has(e.position)) {
            state.errorsByIndex.set(e.position, e);
        }
    });
    // Fallback for servers that do not send positions: match by name, first unmatched occurrence
    errors.filter(e => typeof e.position !== 'number').forEach(e => {
        const index = state.words.findIndex((w, i) => !state.errorsByIndex.has(i) && w.toLowerCase() === String(e.word).toLowerCase());
        if (index >= 0) {
            state.errorsByIndex.set(index, e);
        }
    });

    renderScore(Math.round(data.score || 0), state.errorsByIndex.size, state.words.length, data.transcribe);
    renderChips();
    renderHeard(data);

    $('word-detail').classList.add('hidden');
    state.chartsDirty = true;
    if ($('details').open) {
        renderCharts();
    }

    setView('result');

    const result = $('result');
    result.classList.remove('reveal');
    void result.offsetWidth;
    result.classList.add('reveal');
    result.scrollIntoView({ behavior: 'smooth', block: 'start' });

    const firstFlagged = state.words.findIndex((_, i) => state.errorsByIndex.has(i));

    if (firstFlagged >= 0) {
        selectWord(firstFlagged, false);
    }

    $("result").style.display ='';
}

function renderScore(score, flagged, total, transcribe) {
    const ring = $('score-ring');
    ring.style.stroke = score >= 80 ? COLORS.good : score >= 50 ? COLORS.mid : COLORS.bad;
    ring.style.strokeDashoffset = RING_LENGTH;
    requestAnimationFrame(() => {
        ring.style.strokeDashoffset = RING_LENGTH * (1 - Math.max(0, Math.min(100, score)) / 100);
    });
    countUp($('score-value'), score, 900);

    let verdict;
    let sub;
    if (total === 0) {
        verdict = 'Nothing to compare';
        sub = 'The sentence is empty.';
    } else if (flagged === 0 && score < 70) {
        verdict = 'No single word stands out';
        sub = transcribe
            ? `But the sentence as a whole was hard to follow: we heard "${String(transcribe).toLowerCase()}". Check that the recording started before you spoke.`
            : 'But the sentence as a whole was hard to follow.';
    } else if (flagged === 0) {
        verdict = 'Every word came through clearly';
        sub = 'Tap a word to see its sounds anyway.';
    } else if (flagged === total) {
        verdict = 'None of the words came through';
        sub = 'Tap a word to see which sounds went wrong. Was it the right sentence?';
    } else {
        verdict = `${flagged} of ${total} word${total > 1 ? 's' : ''} need${flagged === 1 ? 's' : ''} work`;
        sub = 'Tap a red word to see which sounds went wrong.';
    }
    $('verdict').textContent = verdict;
    $('verdict-sub').textContent = sub;
}

function countUp(el, target, duration) {
    const reduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced || duration <= 0) {
        el.textContent = target;
        return;
    }
    const start = performance.now();
    const step = (now) => {
        const t = Math.min(1, (now - start) / duration);
        const eased = 1 - Math.pow(1 - t, 3);
        el.textContent = Math.round(target * eased);
        if (t < 1) {
            requestAnimationFrame(step);
        }
    };
    requestAnimationFrame(step);
}

function renderChips() {
    const container = $('word-chips');
    container.innerHTML = '';
    const expectedPhones = state.result.differences.expected_phones || [];

    state.words.forEach((word, index) => {
        const error = state.errorsByIndex.get(index);
        const chip = document.createElement('button');
        chip.type = 'button';
        chip.dataset.index = index;
        chip.textContent = word;
        chip.setAttribute('aria-pressed', 'false');
        chip.className = 'chip px-3 py-1.5 rounded-lg border text-base font-medium ' + (error
            ? 'bg-red-50 border-red-200 text-red-700 hover:bg-red-100'
            : 'bg-emerald-50 border-emerald-200 text-emerald-800 hover:bg-emerald-100');
        if (error) {
            const conf = typeof error.confidence === 'number' ? `${Math.round(error.confidence * 100)} % sure` : 'flagged';
            chip.title = error.actual === ''
                ? `Not heard (${conf}). Expected /${error.expected}/`
                : `Flagged, ${conf}. Expected /${error.expected}/, heard /${error.actual}/`;
        } else {
            const phones = expectedPhones[index];
            chip.title = phones && phones.length ? `Sounds fine: /${phones.join(' ')}/` : 'Sounds fine';
        }
        chip.addEventListener('click', () => selectWord(index, true));
        container.appendChild(chip);
    });
}

function renderHeard(data) {
    const transcript = (data.transcribe || '').trim();
    $('transcript').textContent = transcript ? transcript.toLowerCase() : 'nothing we could recognize';

    const heard = data.differences.heard_phones || [];
    const confidences = data.differences.heard_phones_confidence || [];
    const container = $('heard-phones');
    container.innerHTML = '';
    if (!heard.length) {
        container.textContent = 'no phones recognized';
        container.className = 'text-sm text-neutral-500';
        return;
    }
    container.className = 'font-mono text-base leading-relaxed';
    heard.forEach((phone, i) => {
        const span = document.createElement('span');
        span.textContent = phone;
        const conf = confidences[i];
        const sure = typeof conf !== 'number' || conf >= 0.5;
        span.className = 'inline-block mr-1.5 ' + (sure ? 'text-neutral-800' : 'text-neutral-400');
        if (typeof conf === 'number') {
            span.title = `${Math.round(conf * 100)} % confidence`;
        }
        container.appendChild(span);
    });
}

function selectWord(index, speak) {
    state.selected = index;
    $('word-chips').querySelectorAll('.chip').forEach(chip => {
        chip.setAttribute('aria-pressed', String(Number(chip.dataset.index) === index));
    });

    const word = state.words[index];
    const error = state.errorsByIndex.get(index);
    const expectedPhones = (state.result.differences.expected_phones || [])[index] || [];
    const detail = $('word-detail');
    const status = $('detail-status');
    const expectedEl = $('detail-expected');
    const heardEl = $('detail-heard');
    const note = $('detail-note');

    $('detail-word').textContent = word;
    expectedEl.innerHTML = '';
    heardEl.innerHTML = '';
    note.textContent = '';

    if (!error) {
        status.textContent = 'Sounds fine';
        status.className = 'text-sm font-medium text-emerald-700';
        expectedEl.textContent = expectedPhones.length ? `/${expectedPhones.join(' ')}/` : '/?/';
        heardEl.textContent = 'the same';
        heardEl.className = 'text-base text-neutral-500';
    } else {
        const conf = typeof error.confidence === 'number' ? Math.round(error.confidence * 100) : null;
        status.textContent = error.actual === ''
            ? (conf === null ? 'Not heard' : `Not heard, ${conf} % sure`)
            : (conf === null ? 'Flagged' : `Flagged, ${conf} % sure`);
        status.className = 'text-sm font-medium text-red-600';

        const phones = Array.isArray(error.phones) && error.phones.length ? error.phones : null;
        expectedEl.appendChild(document.createTextNode('/'));
        if (phones) {
            phones.forEach((p, i) => {
                const span = document.createElement('span');
                span.textContent = p.expected;
                const wrong = typeof p.confidence === 'number' && p.confidence >= PHONE_WRONG_THRESHOLD;
                if (wrong) {
                    span.className = 'text-red-600 font-semibold underline decoration-2 decoration-red-300 underline-offset-4';
                    span.title = `heard /${p.heard || '∅'}/ (${Math.round(p.confidence * 100)} %)`;
                }
                expectedEl.appendChild(span);
                if (i < phones.length - 1) {
                    expectedEl.appendChild(document.createTextNode(' '));
                }
            });
        } else {
            expectedEl.appendChild(document.createTextNode(error.expected || expectedPhones.join(' ')));
        }
        expectedEl.appendChild(document.createTextNode('/'));

        if (error.actual === '') {
            heardEl.textContent = 'nothing';
            heardEl.className = 'text-base text-neutral-500';
        } else {
            const heardPhones = phones ? phones.map(p => p.heard).filter(Boolean).join(' ') : error.actual;
            heardEl.textContent = `/${heardPhones}/`;
            heardEl.className = 'font-mono text-lg tracking-wide text-neutral-800';
        }
        if (error.actual_word) {
            note.textContent = `The speech recognizer understood "${String(error.actual_word).toLowerCase()}".`;
        }
    }
    expectedEl.className = 'font-mono text-lg tracking-wide text-neutral-800';

    detail.classList.remove('hidden');
    if (speak) {
        speakWord(index);
    } else {
        viseme.rest();
    }
}

function speakWord(index) {
    const word = state.words[index];
    const error = state.errorsByIndex.get(index);
    const expectedPhones = (state.result.differences.expected_phones || [])[index] || [];
    const phones = expectedPhones.length ? expectedPhones : (error && error.expected ? [error.expected] : []);

    if (phones.length) {
        const duration = viseme.estimateWordDuration(phones) * 1.5;
        viseme.play(phones, duration);
    }
    if ('speechSynthesis' in window) {
        speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(word);
        utterance.lang = 'en-US';
        utterance.rate = 0.8;
        speechSynthesis.speak(utterance);
    }
}

// ---------------------------------------------------------------- charts

function renderCharts() {
    const data = state.result;
    if (!data || typeof Chart === 'undefined') {
        return;
    }
    state.chartsDirty = false;

    const diff = data.differences;
    const expected = diff.expected_vector || [];
    const heard = diff.transcribed_vector || [];
    const n = Math.max(expected.length, heard.length);
    lineChart('phoneme-chart', Array.from({ length: n }, (_, i) => i + 1), [
        { label: 'Reference', data: expected, borderColor: COLORS.reference },
        { label: 'You', data: heard, borderColor: COLORS.you },
    ], { legend: true, yTicks: false, tooltipTitle: (items) => `Position ${items[0].label}` });

    const prosody = data.prosody || {};
    const f0 = prosody.f0 || [];
    const energy = prosody.energy || [];
    lineChart('f0-chart', f0.map((_, i) => i), [
        { label: 'Pitch (Hz)', data: f0, borderColor: COLORS.you },
    ], { legend: false, yTicks: true });
    lineChart('energy-chart', energy.map((_, i) => i), [
        { label: 'Energy', data: energy, borderColor: COLORS.you },
    ], { legend: false, yTicks: true });
}

function lineChart(canvasId, labels, datasets, { legend, yTicks, tooltipTitle }) {
    if (state.charts[canvasId]) {
        state.charts[canvasId].destroy();
    }
    const ctx = $(canvasId).getContext('2d');
    state.charts[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels,
            datasets: datasets.map(d => ({
                ...d,
                borderWidth: 2,
                pointRadius: 0,
                pointHitRadius: 8,
                tension: 0.3,
                fill: false,
            })),
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 300 },
            interaction: { mode: 'index', intersect: false },
            scales: {
                x: { display: false },
                y: {
                    display: true,
                    grid: { color: '#f0efed' },
                    border: { display: false },
                    ticks: { display: yTicks, color: '#8a8782', font: { size: 11 }, maxTicksLimit: 4 },
                },
            },
            plugins: {
                legend: {
                    display: legend,
                    position: 'bottom',
                    labels: { boxWidth: 10, boxHeight: 10, usePointStyle: true, color: '#57534e', font: { size: 11 } },
                },
                tooltip: {
                    callbacks: tooltipTitle ? { title: tooltipTitle } : {},
                },
            },
        },
    });
}
