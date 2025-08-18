// Fonction de téléchargement de fichiers
window.downloadFile = function(dataUrl, fileName) {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Fonction d'affichage de toast (si utilisée ailleurs)
window.showToast = function(message, type) {
    // Simple alert pour l'instant
    alert(message);
};