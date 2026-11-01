// Giải mã bản sao lưu ở trang /drop ngay trên trình duyệt.
// Ảnh vỏ là JPEG nối thêm một gói ZIP mã hóa ZipCrypto (xem tools/build_arg_site_assets.py).
// Trang không chứa mã hay ảnh gốc: chỉ khi giải đúng mã thì CRC32 của dữ liệu mới khớp.
const CARRIER_URL = "IMG_20261022_234117.jpg";

const CRC_TABLE = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    table[n] = c >>> 0;
  }
  return table;
})();

function crc32Byte(crc, b) {
  return (CRC_TABLE[(crc ^ b) & 0xff] ^ (crc >>> 8)) >>> 0;
}

function crc32(bytes) {
  let crc = 0xffffffff;
  for (let i = 0; i < bytes.length; i++) crc = crc32Byte(crc, bytes[i]);
  return (crc ^ 0xffffffff) >>> 0;
}

function zipCryptoDecrypt(password, encrypted) {
  let k0 = 0x12345678, k1 = 0x23456789, k2 = 0x34567890;
  const update = (b) => {
    k0 = crc32Byte(k0, b);
    k1 = (Math.imul((k1 + (k0 & 0xff)) >>> 0, 134775813) + 1) >>> 0;
    k2 = crc32Byte(k2, k1 >>> 24);
  };
  for (const b of password) update(b);

  const out = new Uint8Array(encrypted.length);
  for (let i = 0; i < encrypted.length; i++) {
    const temp = (k2 | 2) & 0xffff;
    const c = encrypted[i] ^ ((Math.imul(temp, temp ^ 1) >>> 8) & 0xff);
    update(c);
    out[i] = c;
  }
  return out;
}

// Đọc mục đầu tiên của gói ZIP nằm ở cuối tệp.
function readZipEntry(buf) {
  const view = new DataView(buf.buffer, buf.byteOffset, buf.byteLength);
  let eocd = -1;
  for (let i = buf.length - 22; i >= 0; i--) {
    if (view.getUint32(i, true) === 0x06054b50) { eocd = i; break; }
  }
  if (eocd < 0) throw new Error("no-zip");

  const cd = view.getUint32(eocd + 16, true);
  const crc = view.getUint32(cd + 16, true);
  const size = view.getUint32(cd + 20, true);
  const nameLen = view.getUint16(cd + 28, true);
  const name = new TextDecoder().decode(buf.subarray(cd + 46, cd + 46 + nameLen));
  const local = view.getUint32(cd + 42, true);
  const dataStart = local + 30 + view.getUint16(local + 26, true) + view.getUint16(local + 28, true);
  return { name, crc, data: buf.subarray(dataStart, dataStart + size) };
}

document.addEventListener("DOMContentLoaded", () => {
  const keyInput = document.getElementById("keyInput");
  const btnDecrypt = document.getElementById("btnDecrypt");
  const decryptMsg = document.getElementById("decryptMsg");
  const evidenceReveal = document.getElementById("evidenceReveal");
  const imgPreview = document.getElementById("imgPreview");
  const btnDownloadDecrypted = document.getElementById("btnDownloadDecrypted");

  if (!btnDecrypt || !keyInput) return;

  let entry = null;
  let blobUrl = null;

  function showMessage(text, kind) {
    decryptMsg.textContent = text;
    decryptMsg.className = "decrypt-msg " + kind;
  }

  async function doDecrypt() {
    const key = keyInput.value.trim();
    if (!key) {
      showMessage("Nhập mã trước đã.", "error");
      return;
    }

    try {
      if (!entry) {
        const res = await fetch(CARRIER_URL);
        if (!res.ok) throw new Error("fetch");
        entry = readZipEntry(new Uint8Array(await res.arrayBuffer()));
      }
    } catch {
      showMessage("Không đọc được bản sao lưu. Thử tải lại trang.", "error");
      return;
    }

    const plain = zipCryptoDecrypt(new TextEncoder().encode(key), entry.data);
    // 12 byte đầu là phần đầu mã hóa của ZipCrypto; byte cuối của nó là byte cao của CRC.
    const content = plain.subarray(12);
    if (plain[11] !== entry.crc >>> 24 || crc32(content) !== entry.crc) {
      evidenceReveal.classList.remove("active");
      showMessage("Sai mã.", "error");
      return;
    }

    if (blobUrl) URL.revokeObjectURL(blobUrl);
    blobUrl = URL.createObjectURL(new Blob([content], { type: "image/jpeg" }));
    imgPreview.src = blobUrl;
    btnDownloadDecrypted.href = blobUrl;
    btnDownloadDecrypted.download = entry.name;
    btnDownloadDecrypted.textContent = "Tải ảnh gốc (" + entry.name + ")";
    evidenceReveal.classList.add("active");
    showMessage("Đã mở.", "success");
  }

  btnDecrypt.addEventListener("click", doDecrypt);
  keyInput.addEventListener("keypress", (e) => {
    if (e.key === "Enter") doDecrypt();
  });
});
