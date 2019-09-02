(() => {
    let pagePath = "/hitCounts/" + (location.host + location.pathname).replace(/\./g, ",").replace(/\//g, "-");
    let ref = database.ref(pagePath);
    let pageViews;
    //ref.on('value', data => {console.log(data)}, err => {});
    ref.transaction(current => { return (pageViews = (current || 0) + 1); });

    //function SetViews(views){
    //    document.getElementById("viewCounter").innerText = "Page Views: " + views;
    //}
})();