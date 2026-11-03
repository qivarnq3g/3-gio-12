import os
import io
import struct
import zlib
import hashlib
from PIL import Image, ImageDraw

ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUTPUT_IMG_DIR = os.path.join(ROOT_DIR, "arg-site", "assets", "img")
OUT_SECRET_DIR = os.path.join(ROOT_DIR, "tools", "out")

os.makedirs(OUTPUT_IMG_DIR, exist_ok=True)
os.makedirs(OUT_SECRET_DIR, exist_ok=True)

print(f"Generating assets into {OUTPUT_IMG_DIR} and secrets into {OUT_SECRET_DIR}...")

# 1. Avatar: Ngan (CNTT student, gentle sketch)
def make_avatar():
    img = Image.new("RGB", (300, 300), color=(245, 238, 230))
    draw = ImageDraw.Draw(img)
    for r in range(140, 20, -10):
        draw.ellipse([150 - r, 150 - r, 150 + r, 150 + r], fill=(235, 222, 210))
    draw.ellipse([110, 80, 190, 165], fill=(70, 50, 40))
    draw.ellipse([95, 120, 205, 200], fill=(70, 50, 40))
    draw.ellipse([120, 100, 180, 170], fill=(230, 195, 170))
    draw.arc([135, 130, 165, 145], 0, 180, fill=(180, 90, 80), width=2)
    draw.rectangle([115, 170, 185, 240], fill=(210, 170, 145))
    draw.polygon([(110, 210), (190, 210), (220, 300), (80, 300)], fill=(75, 95, 120)) # Blue hoodie
    # Fish embroidery motif
    draw.ellipse([155, 245, 175, 260], fill=(220, 110, 50))
    draw.polygon([(170, 252), (185, 245), (185, 260)], fill=(220, 110, 50))

    path = os.path.join(OUTPUT_IMG_DIR, "avatar.jpg")
    img.save(path, "JPEG", quality=90)
    print("Created avatar.jpg")

# 2. Embroidery fish: "Miếng vải thêu con cá bống"
def make_theu_ca_bong():
    img = Image.new("RGB", (600, 400), color=(228, 220, 205))
    draw = ImageDraw.Draw(img)
    for i in range(0, 600, 6):
        draw.line([(i, 0), (i, 400)], fill=(220, 212, 196), width=1)
    for j in range(0, 400, 6):
        draw.line([(0, j), (600, j)], fill=(220, 212, 196), width=1)
    draw.rectangle([30, 30, 570, 370], outline=(195, 185, 170), width=2)
    draw.ellipse([180, 150, 380, 250], fill=(235, 105, 30), outline=(200, 80, 15), width=3)
    draw.polygon([(360, 200), (450, 140), (430, 200), (455, 260)], fill=(245, 130, 50), outline=(200, 80, 15))
    draw.polygon([(240, 150), (280, 110), (320, 150)], fill=(245, 130, 50), outline=(200, 80, 15))
    draw.ellipse([215, 185, 230, 200], fill=(255, 255, 255))
    draw.ellipse([220, 190, 227, 197], fill=(20, 20, 20))
    draw.text((250, 290), "B O N G  -  2 0 2 6", fill=(130, 95, 75))

    path = os.path.join(OUTPUT_IMG_DIR, "theu-ca-bong.jpg")
    img.save(path, "JPEG", quality=90)
    print("Created theu-ca-bong.jpg")

# 3. Cư xá C4: Góc hành lang căn 44 tầng 4
def make_cu_xa_c4():
    img = Image.new("RGB", (600, 400), color=(50, 50, 55))
    draw = ImageDraw.Draw(img)
    # Old apartment building corridor
    draw.polygon([(0, 0), (200, 120), (200, 400), (0, 400)], fill=(85, 82, 80)) # Left wall
    draw.polygon([(600, 0), (400, 120), (400, 400), (600, 400)], fill=(75, 72, 70)) # Right railing
    draw.polygon([(200, 120), (400, 120), (400, 400), (200, 400)], fill=(40, 40, 45)) # Center end
    draw.rectangle([0, 260, 600, 400], fill=(110, 105, 100)) # Floor
    draw.line([(0, 260), (600, 260)], fill=(30, 30, 35), width=2)
    # Door 44 sign
    draw.rectangle([110, 160, 180, 300], fill=(45, 38, 35), outline=(25, 20, 18), width=2)
    draw.rectangle([130, 180, 160, 200], fill=(210, 180, 100))
    draw.text((135, 184), "44", fill=(20, 20, 20))
    # Soft warm lamp
    draw.ellipse([270, 90, 330, 150], fill=(240, 200, 110))
    draw.text((250, 360), "C U  X A  C 4", fill=(160, 165, 175))

    path = os.path.join(OUTPUT_IMG_DIR, "cu-xa-c4.jpg")
    img.save(path, "JPEG", quality=88)
    print("Created cu-xa-c4.jpg")

# 4. Evidence photo: Đôi dép chỉ đỏ trong tủ giày nhà Linh (chụp lúc 23:40 ngày 8/11)
def make_evidence_photo():
    img = Image.new("RGB", (800, 600), color=(20, 18, 16))
    draw = ImageDraw.Draw(img)
    
    # Inside shoe cabinet: wooden panels
    draw.rectangle([80, 60, 720, 540], fill=(36, 30, 26), outline=(20, 15, 12), width=3)
    # Cabinet shelf
    draw.rectangle([80, 340, 720, 365], fill=(55, 45, 38))
    
    # Upper shelf shadows & shoes
    draw.ellipse([160, 180, 260, 250], fill=(25, 22, 20))
    draw.ellipse([480, 180, 580, 250], fill=(30, 28, 25))

    # The suspect sandals on shelf (y: 260-340)
    # Left sandal
    draw.ellipse([240, 260, 370, 340], fill=(48, 44, 40), outline=(25, 22, 20), width=2)
    draw.rectangle([275, 280, 340, 310], fill=(62, 58, 54))
    # Right sandal
    draw.ellipse([410, 260, 540, 340], fill=(48, 44, 40), outline=(25, 22, 20), width=2)
    draw.rectangle([445, 280, 510, 310], fill=(62, 58, 54))
    
    # Distinctive RED THREAD on strap of right sandal
    red_thread = (220, 30, 30)
    draw.line([(455, 280), (465, 310)], fill=red_thread, width=4)
    draw.line([(463, 280), (473, 310)], fill=red_thread, width=4)
    draw.line([(471, 280), (481, 310)], fill=red_thread, width=4)
    draw.arc([475, 305, 500, 335], 30, 180, fill=red_thread, width=3) # dangling loop
    
    # Exact timestamp per kịch bản bản 5 mục 2.7: "2026/11/08 23:40"
    draw.text((580, 500), "2026/11/08 23:40", fill=(210, 160, 40))

    # Grain effect
    noise = Image.effect_noise((800, 600), 10)
    img = Image.blend(img, noise.convert("RGB"), 0.07)

    # Save ONLY into tools/out/ (ignored by git, NOT in arg-site/)
    path = os.path.join(OUT_SECRET_DIR, "red-thread-sandal.jpg")
    img.save(path, "JPEG", quality=92)
    print(f"Created secret evidence photo: {path}")
    
    with open(path, "rb") as f:
        content = f.read()
    sha256 = hashlib.sha256(content).hexdigest()
    print(f"SHA-256 of red-thread-sandal.jpg: {sha256}")
    return content, sha256

# ZipCrypto standard implementation
class ZipCrypto:
    def __init__(self, password: bytes):
        self.k0 = 0x12345678
        self.k1 = 0x23456789
        self.k2 = 0x34567890
        for b in password:
            self._update(b)

    def _update(self, b):
        self.k0 = (~zlib.crc32(bytes([b]), ~self.k0)) & 0xffffffff
        self.k1 = ((self.k1 + (self.k0 & 0xff)) * 134775813 + 1) & 0xffffffff
        self.k2 = (~zlib.crc32(bytes([self.k1 >> 24]), ~self.k2)) & 0xffffffff

    def encrypt_byte(self, c):
        temp = (self.k2 | 2) & 0xffff
        k = ((temp * (temp ^ 1)) >> 8) & 0xff
        self._update(c)
        return c ^ k

def make_encrypted_zip(filename: str, data: bytes, password: bytes) -> bytes:
    crc = zlib.crc32(data) & 0xffffffff
    check_byte = (crc >> 24) & 0xff
    header = b'\x00' * 11 + bytes([check_byte])
    zc = ZipCrypto(password)
    enc_header = bytes([zc.encrypt_byte(b) for b in header])
    enc_data = bytes([zc.encrypt_byte(b) for b in data])
    payload = enc_header + enc_data
    
    fname_bytes = filename.encode('utf-8')
    csize = len(payload)
    usize = len(data)
    
    lfh = struct.pack('<IHHHHHIIIHH', 0x04034b50, 20, 1, 0, 0, 0, crc, csize, usize, len(fname_bytes), 0) + fname_bytes + payload
    cd = struct.pack('<IHHHHHHIIIHHHHHII', 0x02014b50, 20, 20, 1, 0, 0, 0, crc, csize, usize, len(fname_bytes), 0, 0, 0, 0, 0, 0) + fname_bytes
    eocd = struct.pack('<IHHHHIIH', 0x06054b50, 0, 0, 1, 1, len(cd), len(lfh), 0)
    return lfh + cd + eocd

# 5. Sunset photo of Saigon river with attached encrypted zip
def make_hoang_hon_polyglot(evidence_bytes, password_str):
    width, height = 960, 640
    img = Image.new("RGB", (width, height), color=(255, 120, 60))
    draw = ImageDraw.Draw(img)
    
    # Sunset sky gradient
    for y in range(0, 380):
        r = int(255 - (y / 380) * 40)
        g = int(120 + (y / 380) * 60)
        b = int(50 + (y / 380) * 110)
        draw.line([(0, y), (width, y)], fill=(r, g, b))
    
    # Sun setting
    draw.ellipse([420, 240, 540, 360], fill=(255, 230, 160))
    
    # Saigon river water with golden reflection
    for y in range(380, height):
        factor = (y - 380) / (height - 380)
        r = int(210 - factor * 140)
        g = int(120 - factor * 80)
        b = int(70 - factor * 30)
        draw.line([(0, y), (width, y)], fill=(r, g, b))
    
    # City skyline silhouette across river
    draw.polygon([(80, 380), (80, 280), (120, 280), (120, 380)], fill=(30, 20, 35))
    draw.polygon([(140, 380), (140, 240), (180, 240), (180, 380)], fill=(35, 25, 40)) # Landmark 81 vibe
    draw.polygon([(170, 240), (175, 170), (180, 240)], fill=(35, 25, 40)) # Spire
    draw.polygon([(210, 380), (210, 310), (260, 310), (260, 380)], fill=(30, 20, 35))
    draw.polygon([(300, 380), (300, 290), (350, 290), (350, 380)], fill=(28, 18, 32))
    draw.polygon([(620, 380), (620, 320), (670, 320), (670, 380)], fill=(30, 20, 35))
    draw.polygon([(700, 380), (700, 270), (760, 270), (760, 380)], fill=(25, 18, 30))
    draw.polygon([(800, 380), (800, 300), (850, 300), (850, 380)], fill=(30, 20, 35))

    # Water reflections
    draw.ellipse([400, 420, 560, 480], fill=(235, 160, 80))
    draw.ellipse([430, 500, 530, 550], fill=(220, 130, 60))

    jpeg_buf = io.BytesIO()
    img.save(jpeg_buf, "JPEG", quality=90)
    jpeg_bytes = jpeg_buf.getvalue()

    # Create encrypted zip with evidence file inside
    zip_bytes = make_encrypted_zip("red-thread-sandal.jpg", evidence_bytes, password_str.encode("utf-8"))

    # Polyglot concatenation: JPEG + ZIP
    polyglot_bytes = jpeg_bytes + zip_bytes

    out_path = os.path.join(OUTPUT_IMG_DIR, "hoang-hon-song-sai-gon.jpg")
    with open(out_path, "wb") as f:
        f.write(polyglot_bytes)
    print(f"Created polyglot image: {out_path} ({len(polyglot_bytes)} bytes)")
    return out_path

# Execute build
make_avatar()
make_theu_ca_bong()
make_cu_xa_c4()
evidence_bytes, sha256 = make_evidence_photo()

# Secret key: 16 characters (mục 8.4 kịch bản bản 5)
KEY_16 = "7bF#9qK$2vX@mP5!"
key_path = os.path.join(OUT_SECRET_DIR, "khoa.txt")
with open(key_path, "w", encoding="utf-8") as f:
    f.write(KEY_16)
print(f"Created secret key: {key_path} -> {KEY_16}")

make_hoang_hon_polyglot(evidence_bytes, KEY_16)

# Remove old files if any
old_files = [
    os.path.join(OUTPUT_IMG_DIR, "meo-na.jpg"),
    os.path.join(OUTPUT_IMG_DIR, "hem-12.jpg"),
    os.path.join(OUTPUT_IMG_DIR, "evidence", "red-thread-sandal.jpg"),
    os.path.join(OUTPUT_IMG_DIR, "evidence", "evidence-package.zip"),
    os.path.join(ROOT_DIR, "arg-site", "assets", "js", "drop.js"),
    os.path.join(ROOT_DIR, "arg-site", "assets", "js", "evidence_data.js"),
    os.path.join(ROOT_DIR, "arg-site", "README.md"),
    os.path.join(ROOT_DIR, "arg-site", "posts", "2026-10-23-nhung-dieu-ky-la.html"),
]
for f in old_files:
    if os.path.exists(f):
        os.remove(f)
        print(f"Removed outdated file: {f}")

evidence_dir = os.path.join(OUTPUT_IMG_DIR, "evidence")
if os.path.exists(evidence_dir) and not os.listdir(evidence_dir):
    os.rmdir(evidence_dir)

js_dir = os.path.join(ROOT_DIR, "arg-site", "assets", "js")
if os.path.exists(js_dir) and not os.listdir(js_dir):
    os.rmdir(js_dir)

print("\n--- SUMMARY OF ASSETS & SECRETS ---")
print(f"SHA-256 of red-thread-sandal.jpg: {sha256}")
print(f"Key in khoa.txt: {KEY_16}")
print(f"Polyglot image: arg-site/assets/img/hoang-hon-song-sai-gon.jpg")
