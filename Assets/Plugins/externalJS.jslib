mergeInto(LibraryManager.library, {

  SayHello: function () {
    window.alert("Hello, world!");
  },

  GetWindowURLToken: function () {
    var urlParams = new URLSearchParams(window.location.search);
    var returnStr = urlParams.get('token');
    var bufferSize = lengthBytesUTF8(returnStr) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(returnStr, buffer, bufferSize);
    return buffer;
  },

  ReportReady: function ()
  {
    window.ReportReady();
  }

});