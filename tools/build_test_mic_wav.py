"""Build test_mic_12-11.wav, the recording in the inner part of Ngân's vault.

It sounds like a phone microphone test: room hiss, a mains hum and a few taps on the mic.
The path "/drop" is written into the 7-14 kHz band, so it only shows up in a spectrogram
(Audacity: track menu > Spectrogram). See docs/thiet-ke/cau-do-arg.md, step 2.

    python -m pip install numpy==2.3.5 pillow==12.0.0
    python tools/build_test_mic_wav.py --spectrogram tools/out/test_mic_spectrogram.png

Output is deterministic: the same inputs always give the same bytes.
"""

import argparse
import hashlib
import wave
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "game" / "Assets" / "StreamingAssets" / "Vault" / "test_mic_12-11.wav"

RATE = 44100
SECONDS = 14.0
TEXT = "/drop"
TEXT_START, TEXT_SECONDS = 4.0, 6.0
LOW_HZ, HIGH_HZ = 7000.0, 14000.0
ROWS = 72
SEED = 1211


def text_mask() -> np.ndarray:
    """Rows x columns in [0, 1]; row 0 is the top of the letters (highest frequency)."""
    font = ImageFont.load_default(size=ROWS)
    left, top, right, bottom = font.getbbox(TEXT)
    image = Image.new("L", (right - left + 8, bottom - top + 8), 0)
    ImageDraw.Draw(image).text((4 - left, 4 - top), TEXT, fill=255, font=font)
    image = image.resize((image.width * 3, ROWS), Image.Resampling.LANCZOS)
    return np.asarray(image, dtype=np.float64) / 255.0


def synthesize() -> np.ndarray:
    rng = np.random.default_rng(SEED)
    n = int(RATE * SECONDS)
    t = np.arange(n) / RATE

    # Room hiss, tilted towards low frequencies like a cheap phone mic.
    white = rng.standard_normal(n)
    hiss = np.convolve(white, np.ones(6) / 6, mode="same") * 0.02
    hum = 0.012 * np.sin(2 * np.pi * 50 * t) + 0.004 * np.sin(2 * np.pi * 150 * t)

    taps = np.zeros(n)
    for at in (1.1, 1.5, 11.8, 12.3):
        start = int(at * RATE)
        length = int(0.09 * RATE)
        envelope = np.exp(-np.arange(length) / (0.012 * RATE))
        taps[start:start + length] += 0.35 * envelope * np.sin(2 * np.pi * 90 * np.arange(length) / RATE)

    mask = text_mask()
    text = np.zeros(n)
    start = int(TEXT_START * RATE)
    span = int(TEXT_SECONDS * RATE)
    local_t = np.arange(span) / RATE
    columns = np.linspace(0, mask.shape[1] - 1, span)
    frequencies = np.linspace(HIGH_HZ, LOW_HZ, mask.shape[0])
    for row, frequency in enumerate(frequencies):
        envelope = np.interp(columns, np.arange(mask.shape[1]), mask[row])
        if envelope.max() < 0.05:
            continue
        phase = rng.uniform(0, 2 * np.pi)
        text[start:start + span] += envelope * np.sin(2 * np.pi * frequency * local_t + phase)
    text *= 0.012

    signal = hiss + hum + taps + text
    fade = int(0.05 * RATE)
    signal[:fade] *= np.linspace(0, 1, fade)
    signal[-fade:] *= np.linspace(1, 0, fade)
    return np.clip(signal, -1, 1)


def write_wav(path: Path, signal: np.ndarray) -> str:
    pcm = (signal * 32767).astype("<i2").tobytes()
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as stream:
        stream.setnchannels(1)
        stream.setsampwidth(2)
        stream.setframerate(RATE)
        stream.writeframes(pcm)
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_spectrogram(path: Path, signal: np.ndarray) -> None:
    """Save a spectrogram like Audacity's, to check the text is readable."""
    size, hop = 2048, 512
    window = np.hanning(size)
    frames = np.lib.stride_tricks.sliding_window_view(signal, size)[::hop] * window
    power = 20 * np.log10(np.abs(np.fft.rfft(frames, axis=1)) + 1e-9)
    image = np.clip((power - power.max() + 80) / 80, 0, 1)[:, ::-1].T
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray((image * 255).astype(np.uint8)).resize((1400, 600)).save(path)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", type=Path, default=OUTPUT)
    parser.add_argument("--spectrogram", type=Path, help="also save a spectrogram PNG for checking")
    args = parser.parse_args()

    signal = synthesize()
    digest = write_wav(args.output, signal)
    print(f"{args.output} sha256 {digest}")
    if args.spectrogram:
        write_spectrogram(args.spectrogram, signal)
        print(f"spectrogram {args.spectrogram}")


if __name__ == "__main__":
    main()
