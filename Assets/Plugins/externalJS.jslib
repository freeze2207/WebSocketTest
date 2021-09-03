mergeInto(LibraryManager.library, {

  SayHello: function () {
    window.alert("Hello, world!");
  },

  GetWindowURL: function () {
    var returnStr = window.location.search;
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