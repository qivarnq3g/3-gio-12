import os
import io
import struct
import zlib
import hashlib
import base64
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "..", "arg-site", "assets", "img")
EVIDENCE_DIR = os.path.join(OUTPUT_DIR, "evidence")

os.makedirs(OUTPUT_DIR, exist_ok=True)
os.makedirs(EVIDENCE_DIR, exist_ok=True)

print(f"Generating assets into {OUTPUT_DIR}...")

# 1. Avatar: Warm, gentle portrait/sketch representing Ngan
def make_avatar():
    img = Image.new("RGB", (300, 300), color=(245, 238, 230))
    draw = ImageDraw.Draw(img)
    # Background soft gradient circles
    for r in range(140, 20, -10):
        alpha = int(255 - r * 0.8)
        draw.ellipse([150 - r, 150 - r, 150 + r, 150 + r], fill=(235, 222, 210))
    # Silhouette of girl with hair tied
    draw.ellipse([110, 80, 190, 165], fill=(70, 50, 40))  # Head
    draw.ellipse([95, 120, 205, 200], fill=(70, 50, 40))  # Hair volume
    draw.ellipse([120, 100, 180, 170], fill=(230, 195, 170)) # Face
    draw.arc([135, 130, 165, 145], 0, 180, fill=(180, 90, 80), width=2) # Gentle smile
    draw.rectangle([115, 170, 185, 240], fill=(210, 170, 145)) # Neck
    # Collar / simple linen shirt
    draw.polygon([(110, 210), (190, 210), (220, 300), (80, 300)], fill=(90, 115, 100))
    # Embroidery fish motif on shirt
    draw.ellipse([155, 245, 175, 260], fill=(220, 110, 50))
    draw.polygon([(170, 252), (185, 245), (185, 260)], fill=(220, 110, 50))

    path = os.path.join(OUTPUT_DIR, "avatar.jpg")
    img.save(path, "JPEG", quality=90)
    print("Created avatar.jpg")

# 2. Cat Bé Na: Calico cat sitting in corridor
def make_meo_na():
    img = Image.new("RGB", (600, 400), color=(60, 55, 50))
    draw = ImageDraw.Draw(img)
    # Concrete stairs / floor
    draw.rectangle([0, 220, 600, 400], fill=(110, 105, 100))
    draw.line([(0, 220), (600, 220)], fill=(40, 35, 30), width=3)
    draw.rectangle([0, 120, 600, 220], fill=(80, 75, 70))
    draw.line([(0, 120), (600, 120)], fill=(40, 35, 30), width=3)
    # Cat body (calico: white, orange, dark patches)
    draw.ellipse([220, 180, 380, 290], fill=(240, 240, 235))
    draw.ellipse([240, 190, 320, 260], fill=(215, 125, 45)) # Orange patch
    draw.ellipse([310, 200, 360, 250], fill=(50, 45, 45))   # Dark patch
    # Cat head & ears
    draw.ellipse([320, 140, 395, 215], fill=(240, 240, 235))
    draw.ellipse([345, 145, 385, 185], fill=(215, 125, 45))
    # Ears
    draw.polygon([(330, 150), (345, 115), (360, 150)], fill=(215, 125, 45))
    draw.polygon([(365, 150), (385, 120), (395, 155)], fill=(50, 45, 45))
    # Tail
    draw.arc([160, 200, 250, 270], 90, 270, fill=(215, 125, 45), width=14)
    # Soft lighting & shadow
    draw.ellipse([200, 275, 400, 305], fill=(70, 65, 60)) # Shadow under cat

    path = os.path.join(OUTPUT_DIR, "meo-na.jpg")
    img.save(path, "JPEG", quality=88)
    print("Created meo-na.jpg")

# 3. Embroidery fish: "Miếng vải thêu con cá bống"
def make_theu_ca_bong():
    img = Image.new("RGB", (600, 400), color=(228, 220, 205))
    draw = ImageDraw.Draw(img)
    # Linen fabric texture lines
    for i in range(0, 600, 6):
        draw.line([(i, 0), (i, 400)], fill=(220, 212, 196), width=1)
    for j in range(0, 400, 6):
        draw.line([(0, j), (600, j)], fill=(220, 212, 196), width=1)
    # Cloth border frayed edge
    draw.rectangle([30, 30, 570, 370], outline=(195, 185, 170), width=2)
    # Orange fish body (Goby fish / cá bống)
    draw.ellipse([180, 150, 380, 250], fill=(235, 105, 30), outline=(200, 80, 15), width=3)
    # Tail fin
    draw.polygon([(360, 200), (450, 140), (430, 200), (455, 260)], fill=(245, 130, 50), outline=(200, 80, 15))
    # Dorsal fin
    draw.polygon([(240, 150), (280, 110), (320, 150)], fill=(245, 130, 50), outline=(200, 80, 15))
    # Eye
    draw.ellipse([215, 185, 230, 200], fill=(255, 255, 255))
    draw.ellipse([220, 190, 227, 197], fill=(20, 20, 20))
    # Hand-stitch text effect: "Bống · 2026"
    draw.text((250, 290), "B O N G  -  2 0 2 6", fill=(130, 95, 75))

    path = os.path.join(OUTPUT_DIR, "theu-ca-bong.jpg")
    img.save(path, "JPEG", quality=90)
    print("Created theu-ca-bong.jpg")

# 4. Alleyway: Hẻm 12 Sài Gòn
def make_hem_12():
    img = Image.new("RGB", (600, 400), color=(45, 42, 40))
    draw = ImageDraw.Draw(img)
    # Dim rainy alleyway perspective
    # Left house wall
    draw.polygon([(0, 0), (220, 150), (220, 400), (0, 400)], fill=(65, 60, 55))
    # Right house wall
    draw.polygon([(600, 0), (380, 150), (380, 400), (600, 400)], fill=(55, 52, 48))
    # Wet ground reflection
    draw.polygon([(220, 240), (380, 240), (600, 400), (0, 400)], fill=(35, 38, 42))
    # Dim street lamp yellow light
    draw.ellipse([260, 120, 340, 190], fill=(210, 170, 70))
    draw.line([(0, 80), (600, 60)], fill=(20, 20, 20), width=2) # Overhead cables
    draw.line([(0, 100), (600, 85)], fill=(20, 20, 20), width=1)
    draw.text((250, 360), "H E M  1 2", fill=(90, 95, 105))

    path = os.path.join(OUTPUT_DIR, "hem-12.jpg")
    img.save(path, "JPEG", quality=88)
    print("Created hem-12.jpg")

# 5. Evidence photo: Đôi dép chỉ đỏ (Red Thread Sandal)
def make_evidence_photo():
    # Dim corridor night shot, taken by phone camera (Night shot, 22/10)
    img = Image.new("RGB", (800, 600), color=(28, 25, 23))
    draw = ImageDraw.Draw(img)
    
    # Wooden/metal shoe rack against wall
    draw.rectangle([120, 80, 680, 520], fill=(42, 38, 35))
    # Rack shelves
    draw.rectangle([140, 180, 660, 200], fill=(65, 58, 52))
    draw.rectangle([140, 320, 660, 340], fill=(65, 58, 52))
    draw.rectangle([140, 460, 660, 480], fill=(65, 58, 52))
    
    # Other shoes on lower/upper shelf (subtle/dark)
    draw.ellipse([180, 400, 270, 460], fill=(30, 28, 26))
    draw.ellipse([280, 400, 370, 460], fill=(48, 45, 42))
    draw.ellipse([450, 120, 550, 180], fill=(35, 32, 30))

    # The suspect sandals on the middle shelf (shelf y: 320-340)
    # Left sandal
    draw.ellipse([260, 240, 380, 320], fill=(55, 50, 45), outline=(30, 28, 25), width=2)
    draw.rectangle([290, 260, 350, 290], fill=(70, 65, 60)) # Strap
    # Right sandal
    draw.ellipse([400, 240, 520, 320], fill=(55, 50, 45), outline=(30, 28, 25), width=2)
    draw.rectangle([430, 260, 490, 290], fill=(70, 65, 60)) # Strap
    
    # THE DISTINCTIVE RED THREAD bound tightly around the strap of the right sandal!
    # Red thread winding around the strap
    red_thread_color = (220, 35, 35)
    draw.line([(440, 260), (450, 290)], fill=red_thread_color, width=4)
    draw.line([(448, 260), (458, 290)], fill=red_thread_color, width=4)
    draw.line([(456, 260), (466, 290)], fill=red_thread_color, width=4)
    # Hanging thread tail
    draw.arc([460, 285, 485, 315], 30, 180, fill=red_thread_color, width=3)
    
    # Timestamp overlay on bottom right: "2026/10/22 23:41"
    timestamp_text = "2026/10/22 23:41"
    draw.text((600, 540), timestamp_text, fill=(210, 160, 40))

    # Add subtle camera noise/grain
    noise = Image.effect_noise((800, 600), 12)
    img = Image.blend(img, noise.convert("RGB"), 0.08)

    path = os.path.join(EVIDENCE_DIR, "red-thread-sandal.jpg")
    img.save(path, "JPEG", quality=92)
    print(f"Created evidence photo: {path}")
    
    with open(path, "rb") as f:
        content = f.read()
    sha256 = hashlib.sha256(content).hexdigest()
    print(f"SHA-256 of red-thread-sandal.jpg: {sha256}")
    return path, content, sha256

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
    
    # Local file header
    lfh = struct.pack('<IHHHHHIIIHH', 0x04034b50, 20, 1, 0, 0, 0, crc, csize, usize, len(fname_bytes), 0) + fname_bytes + payload
    
    # Central directory
    cd = struct.pack('<IHHHHHHIIIHHHHHII', 0x02014b50, 20, 20, 1, 0, 0, 0, crc, csize, usize, len(fname_bytes), 0, 0, 0, 0, 0, 0) + fname_bytes
    
    # End of central directory
    eocd = struct.pack('<IHHHHIIH', 0x06054b50, 0, 0, 1, 1, len(cd), len(lfh), 0)
    return lfh + cd + eocd

make_avatar()
make_meo_na()
make_theu_ca_bong()
make_hem_12()
img_path, img_bytes, img_sha256 = make_evidence_photo()

# Create encrypted zip package with password '472619'
zip_bytes = make_encrypted_zip("red-thread-sandal.jpg", img_bytes, b"472619")
zip_path = os.path.join(EVIDENCE_DIR, "evidence-package.zip")
with open(zip_path, "wb") as f:
    f.write(zip_bytes)
print(f"Created encrypted zip: {zip_path} (Password: 472619)")

# Also create base64 string for direct client-side decryptor in drop.js
b64_data = base64.b64encode(img_bytes).decode('ascii')
payload_js_path = os.path.join(os.path.dirname(__file__), "..", "arg-site", "assets", "js", "evidence_data.js")
os.makedirs(os.path.dirname(payload_js_path), exist_ok=True)
with open(payload_js_path, "w", encoding="utf-8") as f:
    f.write(f"// Encrypted data container for browser-based decryptor\n")
    f.write(f"window.__EVIDENCE_DATA__ = {{\n")
    f.write(f"  fileName: 'red-thread-sandal.jpg',\n")
    f.write(f"  sha256: '{img_sha256}',\n")
    f.write(f"  expectedKey: '472619',\n")
    f.write(f"  base64: '{b64_data}'\n")
    f.write(f"}};\n")
print(f"Created {payload_js_path}")
print("Assets generation complete!")
