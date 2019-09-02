$(document).ready(() => ko.applyBindings(new Index()));

function Index() {

    searchResults = ko.observableArray();

    searchPhrase = "";

    GetClass = function(d){

        if(d == "comment"){
            return "code-comment";
        }

        return "code-other";
    }

    let lastFilter = "";
    let filterTimeout;
    CheckFilter = function(d, e){        
        if(lastFilter == searchPhrase) return;
        lastFilter = searchPhrase;

        clearTimeout(filterTimeout);
        let filter = searchPhrase;
        setTimeout(() => UpdateItems(filter), 500);
    }

    UpdateItems = function(filter){
        if(filter == ""){
            searchResults([]);
            return;
        }
        let lowerFilter = filter.toLowerCase();
        let filteredItems = allItems.filter(x => x.searchBy.toLowerCase().includes(lowerFilter));

        if(filteredItems.length > 50){
            filteredItems = filteredItems.slice(0, 50);
        }

        filteredItems.sort((a,b)=> a.searchBy.length - b.searchBy.length);
        // filteredItems = filteredItems.sort((a,b)=> a.searchBy > b.searchBy);

        searchResults(filteredItems);
        Prism.highlightAll();
    }

    allItems = [];
    FixItems = function(){
        for (var file of cgincData.files) {
            // console.log(file);
            for(var prop in file){
                if (Object.prototype.hasOwnProperty.call(file, prop)) {
                    if(typeof(file[prop]) != "object"){
                        continue;
                    }

                    // do stuff
                    for(var item of file[prop]){
                        let newItem = {};
                        newItem.properties = {};
                        newItem.propNames = [];

                        for(var itemProp in item){
                            if (itemProp != "code" && Object.prototype.hasOwnProperty.call(item, itemProp)) {
                                if(itemProp == "comment" || itemProp == "modifiers"){                                    
                                    if(item[itemProp].trim() == "") continue;
                                }

                                newItem.properties[itemProp] = item[itemProp];

                                if(itemProp == "parameters"){
                                    newItem.properties[itemProp] = "(" + newItem.properties[itemProp] + ")";
                                }


                                newItem.propNames.push(itemProp);
                            }
                        }

                        newItem.properties["file"] = file.file;
                        newItem.propNames.push("file");
                        newItem.code = item.code;
                        newItem.searchBy = item.name ? item.name : item.code;
                        newItem.codeShown = ko.observable(false);
                        allItems.push(newItem);
                    }
                }
                
            }
        }
    }

    FixItems();
}