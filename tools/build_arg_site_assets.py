"""Build the /drop attachment from fixed local source files and a private key."""

import argparse
import hashlib
from pathlib import Path
from tempfile import TemporaryDirectory

import pyzipper


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "arg-site" / "drop" / "hoang-hon-song-sai-gon.jpg"
MEMBERS = ("red-thread-sandal.jpg", "missing-child-notice.jpg", "ghi-chu-so-sanh.txt")


def build(inputs: Path) -> str:
    cover = (inputs / "cover-2026-11-08.jpg").read_bytes()
    if not cover.startswith(b"\xff\xd8") or not cover.endswith(b"\xff\xd9"):
        raise ValueError("Use a clean JPEG for cover-2026-11-08.jpg.")

    password = (inputs / "khoa.txt").read_bytes().strip()
    if len(password) != 16 or not all(33 <= value <= 126 for value in password):
        raise ValueError("khoa.txt must contain exactly 16 printable ASCII characters.")

    with TemporaryDirectory(dir=OUTPUT.parent) as temp:
        attachment = Path(temp) / OUTPUT.name
        with attachment.open("wb") as stream:
            stream.write(cover)
            with pyzipper.AESZipFile(stream, "w", compression=pyzipper.ZIP_DEFLATED) as archive:
                archive.setpassword(password)
                archive.setencryption(pyzipper.WZ_AES, nbits=256)
                for name in MEMBERS:
                    archive.write(inputs / name, arcname=name)
        attachment.replace(OUTPUT)

    return hashlib.sha256((inputs / MEMBERS[0]).read_bytes()).hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inputs", type=Path, default=ROOT / "tools" / "out" / "arg-evidence")
    args = parser.parse_args()
    print(f"SHA-256 (red-thread-sandal.jpg): {build(args.inputs)}")


if __name__ == "__main__":
    main()
