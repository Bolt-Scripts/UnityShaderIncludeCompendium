(() => {
  // Initialize Firebase
  var config = {
    apiKey: "AIzaSyAQ_YYbq0533lX9DCzxPV8mKg2sQfemovc",
    authDomain: "personalwebsite-2229a.firebaseapp.com",
    databaseURL: "https://personalwebsite-2229a.firebaseio.com",
    projectId: "personalwebsite-2229a",
    storageBucket: "personalwebsite-2229a.appspot.com",
    messagingSenderId: "361656897436"
  };

  firebase.initializeApp(config);

  database = firebase.database();
})();