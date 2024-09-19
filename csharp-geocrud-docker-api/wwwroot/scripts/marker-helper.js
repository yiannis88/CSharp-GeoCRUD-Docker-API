// Function to process the marker data
function markerPostProcess(data) {
    if (data.length === 0) return; // Handle empty data

    if (data[0].colour !== undefined) {
        markerColour(data);
    }
}

// Function to assign colors
function markerColour(data) {
    data.forEach(item => {
        console.log(item);
        if (item.colour === undefined || !item.colour.startsWith('#')){
            item.colour = '#808080';
            item.value = 'changed';
            console.log("Colour changed...");
        }
    });
}