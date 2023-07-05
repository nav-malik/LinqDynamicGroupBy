window.userActivity = {
  idleTimeout: null,

  startIdleTimer: function (callback, timeout) {
    // Clear the previous timeout if it exists
    if (this.idleTimeout) {
      clearTimeout(this.idleTimeout);
    }

    // Set a new timeout for the specified duration
    this.idleTimeout = setTimeout(callback, timeout);
  },

  resetIdleTimer: function () {
    // Clear the timeout when there is user activity
    if (this.idleTimeout) {
      clearTimeout(this.idleTimeout);
    }
  }
};

document.addEventListener('mousemove', function () {
  window.userActivity.resetIdleTimer();
});

document.addEventListener('keydown', function () {
  window.userActivity.resetIdleTimer();
});
