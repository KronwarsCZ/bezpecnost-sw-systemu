async function handleRegisterSubmit(username, supersecret, authenticator_attachment) {
    if (username === "") {
        console.log("Username empty")
    }

    // possible values: none, direct, indirect
    let attestation_type = "none";
    // possible values: <empty>, platform, cross-platform
    //let authenticator_attachment = "";

    // possible values: preferred, required, discouraged
    let user_verification = "preferred";

    // possible values: discouraged, preferred, required
    let residentKey = "discouraged";


    // prepare form post data
    var data = new FormData();
    data.append('username', username);
    data.append('supersecret', supersecret);
    data.append('attType', attestation_type);
    data.append('authType', authenticator_attachment);
    data.append('userVerification', user_verification);
    data.append('residentKey', residentKey);

    // send to server for registering
    let makeCredentialOptions;
    try {
        makeCredentialOptions = await fetchMakeCredentialOptions(data);
    } catch (e) {
        console.error(e);
        let msg = e.message;
        showErrorAlert(msg);
        return;
    }

    console.log("Credential Options Object", makeCredentialOptions);

    if (makeCredentialOptions.status === "error") {
        console.log("Error creating credential options");
        console.log(makeCredentialOptions.errorMessage);
        showErrorAlert(makeCredentialOptions.errorMessage);
        return;
    }

    // Turn the challenge back into the accepted format of padded base64
    makeCredentialOptions.challenge = coerceToArrayBuffer(makeCredentialOptions.challenge);
    // Turn ID into a UInt8Array Buffer for some reason
    makeCredentialOptions.user.id = coerceToArrayBuffer(makeCredentialOptions.user.id);

    makeCredentialOptions.excludeCredentials = makeCredentialOptions.excludeCredentials.map((c) => {
        c.id = coerceToArrayBuffer(c.id);
        return c;
    });

    if (makeCredentialOptions.authenticatorSelection.authenticatorAttachment === null) makeCredentialOptions.authenticatorSelection.authenticatorAttachment = undefined;

    console.log("Credential Options Formatted", makeCredentialOptions);
    
    alert("Registering... tap your security key to finish registration")
    
    console.log("Creating PublicKeyCredential...");

    let newCredential;
    try {
        newCredential = await navigator.credentials.create({
            publicKey: makeCredentialOptions
        });
    } catch (e) {
        var msg = "Could not create credentials in browser. Probably because the username is already registered with your authenticator. Please change username or authenticator."
        console.error(msg, e);
        showErrorAlert(msg, e);
    }


    console.log("PublicKeyCredential Created", newCredential);

    try {
        await registerNewCredential(newCredential);

    } catch (e) {
        showErrorAlert(e.message ? e.message : e);
    }
}

async function fetchMakeCredentialOptions(formData) {
    let response = await fetch('/api/fido2/makeAttestationOptions', {
        method: 'POST', // or 'PUT'
        body: new URLSearchParams(formData), // data can be `string` or {object}!
        headers: {
            'Accept': 'application/x-www-form-urlencoded'
        }
    });
    
    if (!response.ok) {
        let err = await response.json();
        throw new Error(err.message);
    }
    
    return await response.json();
}


// This should be used to verify the auth data with the server
async function registerNewCredential(newCredential) {
    // Move data into Arrays incase it is super long
    let attestationObject = new Uint8Array(newCredential.response.attestationObject);
    let clientDataJSON = new Uint8Array(newCredential.response.clientDataJSON);
    let rawId = new Uint8Array(newCredential.rawId);

    const data = {
        id: newCredential.id,
        rawId: coerceToBase64Url(rawId),
        type: newCredential.type,
        extensions: newCredential.getClientExtensionResults(),
        response: {
            attestationObject: coerceToBase64Url(attestationObject),
            clientDataJSON: coerceToBase64Url(clientDataJSON),
            transports: newCredential.response.getTransports()
        }
    };

    let response;
    try {
        response = await registerCredentialWithServer(data);
    } catch (e) {
        showErrorAlert(e);
    }

    console.log("Credential Object", response);

    // show error
    if (response.status === "error") {
        console.log("Error creating credential");
        console.log(response.errorMessage);
        showErrorAlert(response.errorMessage);
        return;
    }

    // show success 
    alert("Registration successful!");

    window.location.href = '/passwordless-login';
}

async function registerCredentialWithServer(formData) {
    let response = await fetch('/api/fido2/makeAttestation', {
        method: 'POST',
        body: JSON.stringify(formData), // data can be `string` or {object}!
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    });

    let data = await response.json();

    return data;
}