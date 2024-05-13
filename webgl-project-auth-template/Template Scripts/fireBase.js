var fireBase = fireBase || firebase;
var hasInit = false;
var config = {
    apiKey: "*",
    authDomain: "*",
    projectId: "*",
    storageBucket: "*",
    messagingSenderId: "*",
    appId: "*",
    measurementId: "*"
  };
if(!hasInit){
    firebase.initializeApp(config);
    hasInit = true;
}