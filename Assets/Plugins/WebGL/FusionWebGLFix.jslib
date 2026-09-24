
mergeInto(LibraryManager.library, {
    FusionCopyToClipboard: function(textPtr) {
        var text = UTF8ToString(textPtr);
        if (navigator.clipboard) {
            navigator.clipboard.writeText(text).catch(function(err) {
                console.error('Could not copy text: ', err);
            });
        } else {
            var textArea = document.createElement("textarea");
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.select();
            try {
                document.execCommand('copy');
            } catch (err) {
                console.error('Fallback: Oops, unable to copy', err);
            }
            document.body.removeChild(textArea);
        }
    }
});
