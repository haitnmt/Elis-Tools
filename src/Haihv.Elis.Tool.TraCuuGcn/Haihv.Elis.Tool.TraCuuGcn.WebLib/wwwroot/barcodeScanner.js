window.initializeCodeReader = async function () {
    const codeReader = new ZXing.BrowserMultiFormatReader();
    console.log('ZXing code reader initialized');

    const videoInputDevices = await codeReader.listVideoInputDevices();
    let selectedDeviceId = videoInputDevices[0].deviceId;

    const cameraDevices = videoInputDevices.map(device => ({
        label: device.label,
        deviceId: device.deviceId
    }));

    return {codeReader, selectedDeviceId, cameraDevices};
};

window.startScanning = function (codeReader, selectedDeviceId) {
    codeReader.decodeFromVideoDevice(selectedDeviceId, 'video', (result, err) => {
        if (result) {
            console.log(result);
            document.getElementById('result').textContent = result.text;
        }
        if (err && !(err instanceof ZXing.NotFoundException)) {
            console.error(err);
            document.getElementById('result').textContent = err;
        }
    });
    console.log(`Started continuous decode from camera with id ${selectedDeviceId}`);
};

window.resetScanner = function (codeReader) {
    codeReader.reset();
    document.getElementById('result').textContent = '';
    console.log('Reset.');
};

window.initializeImageDecoder = function (img) {
    const codeReader = new ZXing.BrowserMultiFormatReader();
    console.log('ZXing code reader initialized');

    return new Promise((resolve, reject) => {
        document.getElementById('decodeButton').addEventListener('click', () => {
            codeReader.decodeFromImage(img).then((result) => {
                console.log(result);
                resolve(result.text);
            }).catch((err) => {
                console.error(err);
                reject(err);
            });
            console.log(`Started decode for image from ${img.src}`);
        });
    });
};

window.pasteImageFromClipboard = function () {
    return new Promise((resolve, reject) => {
        navigator.clipboard.read().then(items => {
            for (let item of items) {
                if (item.types.includes('image/png')) {
                    item.getType('image/png').then(blob => {
                        const img = new Image();
                        img.src = URL.createObjectURL(blob);
                        img.onload = () => {
                            window.initializeImageDecoder(img).then(result => {
                                resolve(result);
                            }).catch(err => {
                                reject(err);
                            });
                        };
                    }).catch(err => {
                        reject(err);
                    });
                } else {
                    reject('No image found in clipboard');
                }
            }
        }).catch(err => {
            reject(err);
        });
    });
};