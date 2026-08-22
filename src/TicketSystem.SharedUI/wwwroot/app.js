window.scrollElementToBottom = (element) => {
    if (!element) {
        return;
    }

    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            element.scrollTop = element.scrollHeight;
        });
    });
};
