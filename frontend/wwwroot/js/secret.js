async function getSecret() {
    let username = localStorage.getItem("login_username");
    let token = localStorage.getItem("login_token");
    
    if (token === "" || token === null) {
        alert("You are not logged in!");
    }
    
    await fetchSecret(username, token)
        .then((data) => {
            let secretdiv = document.getElementById("secret-div");
            secretdiv.style.visibility = "visible";
            let secretdivinner = document.getElementById("secret-div-text");
            secretdivinner.innerText = data;
        }).catch((error) => console.log(error));
}

async function fetchSecret(username, token) {
    let response = await fetch('/api/data/secret', {
        method: 'GET',
        headers: {
            'Authorization': 'bearer '+ token, 
            'Accept': 'application/json'
        }
    });
    if (response.status !== 200) {
        throw "incorrect request";
    }
    return await response.json();
}