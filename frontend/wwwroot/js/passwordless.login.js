async function handleSignInSubmit(username) {
    if (username === "") {
        alert("Username cannot be empty!");
        return;
    }

    // prepare form post data
    var formData = new FormData();
    formData.append('username', username);

    // send to server for registering
    let makeAssertionOptions;
    try {
        var res = await fetch('/api/fido2/makeAssertionOptions', {
            method: 'POST',
            body: new URLSearchParams(formData), // data can be `string` or {object}!
            headers: {
                'Accept': 'application/x-www-form-urlencoded'
            }
        });

        makeAssertionOptions = await res.json();
    } catch (e) {
        showErrorAlert("Request to server failed", e);
    }

    console.log("Assertion Options Object", makeAssertionOptions);

    // show options error to user
    if (makeAssertionOptions.status === "error") {
        console.log("Error creating assertion options");
        console.log(makeAssertionOptions.errorMessage);
        showErrorAlert(makeAssertionOptions.errorMessage);
        return;
    }

    makeAssertionOptions.challenge = coerceToArrayBuffer(makeAssertionOptions.challenge);
    makeAssertionOptions.allowCredentials.forEach(function (listItem) {
        listItem.id = coerceToArrayBuffer(listItem.id);
    });

    console.log("Assertion options", makeAssertionOptions);
    
    alert("Logging in... tap your security key to login")
    
    // ask browser for credentials (browser will ask connected authenticators)
    let credential;
    try {
        credential = await navigator.credentials.get({ publicKey: makeAssertionOptions })
    } catch (err) {
        showErrorAlert(err.message ? err.message : err);
    }

    try {
        await verifyAssertionWithServer(credential);
    } catch (e) {
        showErrorAlert("Could not verify assertion", e);
    }
}

/**
 * Sends the credential to the the FIDO2 server for assertion
 * @param {any} assertedCredential
 */
async function verifyAssertionWithServer(assertedCredential) {

    // Move data into Arrays incase it is super long
    let authData = new Uint8Array(assertedCredential.response.authenticatorData);
    let clientDataJSON = new Uint8Array(assertedCredential.response.clientDataJSON);
    let rawId = new Uint8Array(assertedCredential.rawId);
    let sig = new Uint8Array(assertedCredential.response.signature);
    const data = {
        id: assertedCredential.id,
        rawId: coerceToBase64Url(rawId),
        type: assertedCredential.type,
        extensions: assertedCredential.getClientExtensionResults(),
        response: {
            authenticatorData: coerceToBase64Url(authData),
            clientDataJSON: coerceToBase64Url(clientDataJSON),
            signature: coerceToBase64Url(sig)
        }
    };

    let response;
    try {
        let res = await fetch("/api/fido2/makeAssertion", {
            method: 'POST',
            body: JSON.stringify(data), // data can be `string` or {object}!
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        response = await res.json();
    } catch (e) {
        showErrorAlert("Request to server failed", e);
        throw e;
    }

    console.log("Assertion Object", response);

    // show error
    if (response.status === "error") {
        console.log("Error doing assertion");
        console.log(response.errorMessage);
        showErrorAlert(response.errorMessage);
        return;
    }

    localStorage.setItem("login_token", response.token);
    // show success message
    alert("Logged in!");

    window.location.href = '/secret';
}