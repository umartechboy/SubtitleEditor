console.log("Font Utils loaded.");
window.fontUtils = {
    getFontFamilies: function () {
        console.log("getFontFamilies called.");
        return [...document.fonts].map(font => font.family);
    }
};