window.startBarcodeScanner = function (dotnetHelper) {
  // Khoi tao Quagga
  Quagga.init(
    {
      inputStream: {
        name: "Live",
        type: "LiveStream",
        target: document.querySelector("#barcode-reader"),
        constraints: {
          facingMode: "environment",  // Sử dụng camera sau
          width: { ideal: window.innerWidth },  // Độ rộng lý tưởng là chiều rộng của cửa sổ trình duyệt
          height: { ideal: window.innerHeight } // Độ cao lý tưởng là chiều cao của cửa sổ trình duyệt
        },
      },
      decoder: {
        readers: ["code_128_reader"], // Chỉ đọc mã vạch code 128
        multiple: false,
        decodingResolution: 2  // Giá trị cao hơn để tăng độ chính xác
      },
      locate: true,
      halfSample: false,  // Tắt half-sample để giữ nguyên độ phân giải
    },
    function (err) {
      if (err) {
        console.error("Initialization error: ", err);
        dotnetHelper.invokeMethodAsync(
          "HandleError",
          err.message || "Không nhận diện được máy ảnh"
        );
        return;
      }
      console.log("Initialization finished. Ready to start");
      Quagga.start();
    }
  );

  Quagga.onDetected(function (result) {
    if (result && result.codeResult && result.codeResult.code) {
      console.log("Barcode detected: ", result.codeResult.code);
      dotnetHelper.invokeMethodAsync("ReceiveBarcode", result.codeResult.code);
      Quagga.stop(); // Stop after first detection
      // Giải phóng camera
      if (Quagga.CameraAccess) {
        Quagga.CameraAccess.release();
      }
    }
  });
};

window.stopBarcodeScanner = function () {
  Quagga.stop();
  if (Quagga.CameraAccess) {
    Quagga.CameraAccess.release();
  }
};

window.destroyBarcodeScanner = function () {
  if (Quagga) {
    Quagga.stop();
    if (Quagga.CameraAccess && Quagga.CameraAccess.getActiveTrack()) {
      Quagga.CameraAccess.getActiveTrack().stop();
    }
  }
};