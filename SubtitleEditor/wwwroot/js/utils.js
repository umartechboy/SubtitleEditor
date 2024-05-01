window.downloadFile = function (fileName, fileData) {
    const blob = new Blob([fileData], { type: 'application/octet-stream' });
    const blobUrl = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = blobUrl;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(blobUrl);
};
window.loadWaveFileFromBuffer = async (waveBuffer) => {
    const wavesurfer = WaveSurfer.create({
        container: '#waveform',
        backend: 'MediaElement'
    });

    const blob = new Blob([waveBuffer], { type: 'audio/wav' });
    await wavesurfer.loadBlob(blob);
};