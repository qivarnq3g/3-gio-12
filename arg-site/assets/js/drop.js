// Client-side Decryptor for Emergency Drop
document.addEventListener("DOMContentLoaded", () => {
  const keyInput = document.getElementById("keyInput");
  const btnDecrypt = document.getElementById("btnDecrypt");
  const decryptMsg = document.getElementById("decryptMsg");
  const evidenceReveal = document.getElementById("evidenceReveal");
  const imgPreview = document.getElementById("imgPreview");
  const shaDisplay = document.getElementById("shaDisplay");
  const btnDownloadDecrypted = document.getElementById("btnDownloadDecrypted");

  if (!btnDecrypt || !keyInput) return;

  function doDecrypt() {
    const key = keyInput.value.trim();
    if (!key) {
      decryptMsg.textContent = "Vui lòng nhập mật mã bảo mật.";
      decryptMsg.className = "decrypt-msg error";
      return;
    }

    if (!window.__EVIDENCE_DATA__) {
      decryptMsg.textContent = "Lỗi: Không tìm thấy tệp dữ liệu lưu trữ.";
      decryptMsg.className = "decrypt-msg error";
      return;
    }

    const data = window.__EVIDENCE_DATA__;

    if (key === data.expectedKey) {
      decryptMsg.textContent = "Xác thực thành công. Đang giải mã dữ liệu bằng chứng...";
      decryptMsg.className = "decrypt-msg success";

      setTimeout(() => {
        // Decode base64 to Blob
        const byteCharacters = atob(data.base64);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: "image/jpeg" });
        const blobUrl = URL.createObjectURL(blob);

        imgPreview.src = blobUrl;
        shaDisplay.textContent = "Mã băm SHA-256: " + data.sha256;

        btnDownloadDecrypted.href = blobUrl;
        btnDownloadDecrypted.download = data.fileName;

        evidenceReveal.classList.add("active");
        decryptMsg.textContent = "Giải mã hoàn tất! Tệp bằng chứng gốc đã sẵn sàng để tải về.";
      }, 300);
    } else {
      evidenceReveal.classList.remove("active");
      decryptMsg.textContent = "Mật mã không đúng. Vui lòng kiểm tra lại manh mối tìm được.";
      decryptMsg.className = "decrypt-msg error";
    }
  }

  btnDecrypt.addEventListener("click", doDecrypt);
  keyInput.addEventListener("keypress", (e) => {
    if (e.key === "Enter") {
      doDecrypt();
    }
  });
});
