let dotNetHelper;
let codeReader;
let selectedDeviceId;

window.barcodeScanner = {
    initialize: async function (helper) {
        dotNetHelper = helper;
        
        if (typeof ZXing === 'undefined') {
            console.error('ZXing library not loaded');
            dotNetHelper.invokeMethodAsync('OnScanError', 'ZXing library not loaded');
            return;
        }

        try {
            // Request camera permission explicitly
            const stream = await navigator.mediaDevices.getUserMedia({ video: true });
            stream.getTracks().forEach(track => track.stop()); // Stop the stream after permission

            codeReader = new ZXing.BrowserMultiFormatReader();
            console.log('ZXing code reader initialized');

            // Check if running on iOS
            const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
            if (isIOS) {
                // Set specific constraints for iOS
                codeReader.constraints = {
                    facingMode: { exact: "environment" }
                };
            }
        } catch (err) {
            console.error('Error initializing camera:', err);
            dotNetHelper.invokeMethodAsync('OnScanError', 'Camera permission denied or not available');
        }
    },

    listVideoDevices: async function () {
        try {
            // First check if we have permission
            await navigator.mediaDevices.getUserMedia({ video: true });

            const devices = await navigator.mediaDevices.enumerateDevices();
            const videoDevices = devices.filter(device => device.kind === 'videoinput');

            // If we're on mobile and have multiple cameras, try to use the back camera by default
            if (videoDevices.length > 1 && /Android|iPhone|iPad|iPod/i.test(navigator.userAgent)) {
                // Look for back camera keywords in label
                const backCamera = videoDevices.find(device =>
                    device.label.toLowerCase().includes('back') ||
                    device.label.toLowerCase().includes('environment') ||
                    device.label.toLowerCase().includes('rear'));

                if (backCamera) {
                    // Move back camera to first position
                    const index = videoDevices.indexOf(backCamera);
                    videoDevices.splice(index, 1);
                    videoDevices.unshift(backCamera);
                }
            }

            return videoDevices.map(device => ({
                deviceId: device.deviceId,
                label: device.label || `Camera ${device.deviceId}`
            }));
        } catch (err) {
            console.error('Error listing video devices:', err);
            dotNetHelper.invokeMethodAsync('OnScanError', 'Error accessing camera list: ' + err.message);
            return [];
        }
    },

    startScanning: async function (deviceId) {
        try {
            selectedDeviceId = deviceId;

            // Define constraints
            const constraints = {
                video: {
                    deviceId: selectedDeviceId ? { exact: selectedDeviceId } : undefined,
                    facingMode: "environment", // Prefer back camera
                    width: { min: 640, ideal: 1280, max: 1920 },
                    height: { min: 480, ideal: 720, max: 1080 },
                    focusMode: "continuous"
                }
            };

            // Try to start scanning with these constraints
            await codeReader.decodeFromVideoDevice(
                selectedDeviceId,
                'video',
                (result, err) => {
                    if (result) {
                        console.log('Scan result:', result);
                        dotNetHelper.invokeMethodAsync('OnScanResult', result.text);
                    }
                    if (err && !(err instanceof ZXing.NotFoundException)) {
                        console.error('Scan error:', err);
                        dotNetHelper.invokeMethodAsync('OnScanError', err.toString());
                    }
                },
                constraints
            );

            console.log(`Started continuous decode from camera with id ${selectedDeviceId}`);
        } catch (err) {
            console.error('Error starting scanner:', err);
            dotNetHelper.invokeMethodAsync('OnScanError', 'Error starting scanner: ' + err.message);
        }
    },

    resetScanning: function () {
        try {
            if (codeReader) {
                codeReader.reset();
                console.log('Scanner reset');
            }
        } catch (err) {
            console.error('Error resetting scanner:', err);
            dotNetHelper.invokeMethodAsync('OnScanError', 'Error resetting scanner: ' + err.message);
        }
    },

    dispose: function () {
        try {
            if (codeReader) {
                codeReader.reset();
                codeReader = null;
            }
            dotNetHelper = null;
        } catch (err) {
            console.error('Error disposing scanner:', err);
        }
    }
};