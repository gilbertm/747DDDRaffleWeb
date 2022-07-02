// loadScript: returns a promise that completes when the script loads
window.loadCSS = function (cssPath) {
    // check list - if already loaded we can ignore
    if (loaded[cssPath]) {
        console.log(cssPath + " already loaded");
        // return 'empty' promise
        return new this.Promise(function (resolve, reject) {
            resolve();
        });
    }

    return new Promise(function (resolve, reject) {
        // create CSS library link element
        var css = document.createElement("link");
        css.rel = "stylesheet";
        css.type = "text/css";
        css.href = cssPath;
        console.log(cssPath + " created");

        // flag as loading/loaded
        loaded[cssPath] = true;

        // if the css returns okay, return resolve
        css.onload = function () {
            console.log(cssPath + " loaded ok");
            resolve(cssPath);
        };

        // if it fails, return reject
        css.onerror = function () {
            console.log(cssPath + " load failed");
            reject(cssPath);
        }

        // scripts will load at end of body
        document["head"].appendChild(css);
    });
}
// store list of what css we've loaded
loaded = [];