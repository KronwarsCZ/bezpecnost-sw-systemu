using backend.entities;
using backend.helpers;
using backend.Repositories;
using backend.service;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/fido2")]
public class Fido2Controller(
    IUserRepository userRepository, 
    IFido2 fido2, 
    ICredentialRepository credentialRepository, 
    TokenService tokenService) : ControllerBase
{
    IUserRepository _userRepository = userRepository;
    ICredentialRepository _credentialRepository = credentialRepository;
    IFido2 _fido2 = fido2;
    TokenService _tokenService = tokenService;
    
    [HttpPost]
    [Route("makeAttestationOptions")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<ActionResult<CredentialCreateOptions>> MakeAttestationOptionsAsync(
        [FromForm] string username,
        [FromForm] string supersecret,
        [FromForm] string attType,
        [FromForm] string? authType,
        [FromForm] string residentKey,
        [FromForm] string userVerification
        )
    {
        if (string.IsNullOrEmpty(username)) {
            return BadRequest(new { message = "Username is required" });
        }
        if (string.IsNullOrEmpty(supersecret)) {
            supersecret = "default_supersecret";
        }
        if (string.IsNullOrEmpty(attType)) {
            attType = "none";
        }
        if (string.IsNullOrEmpty(authType)) {
            authType = "platform";
        }
        if (string.IsNullOrEmpty(residentKey)) {
            residentKey = "discouraged";
        }
        if (string.IsNullOrEmpty(userVerification)) {
            userVerification = "preferred";
        }
        
        // check user is in db
        if (await _userRepository.GetUserAsync(username) is not null)
        {
            return BadRequest(new { message = "Username already exists" });
        }

        var user = await _userRepository.AddUserAsync(new User
        {
            Id = Guid.NewGuid(),
            Name = username,
            SuperSecret = supersecret,
        });
        if (user is null)
        {
            return BadRequest(new { message = "User creation failed" });
        }

        var excludedCreds = user.Credentials;

        var authenticatorSelection = new AuthenticatorSelection();

        switch (residentKey)
        {
            case "preferred":
            case "required":
                authenticatorSelection.RequireResidentKey = true;
                break;
            case "discouraged":
                authenticatorSelection.RequireResidentKey = false;
                break;
            default:
               BadRequest("Unknown resident key option");
                break;
        }

        switch (userVerification)
        {
            case "preferred":
                authenticatorSelection.UserVerification = UserVerificationRequirement.Preferred;
                break;
            case "required":
                authenticatorSelection.UserVerification = UserVerificationRequirement.Required;
                break;
            case "discouraged":
                authenticatorSelection.UserVerification = UserVerificationRequirement.Discouraged;
                break;
            default:
                BadRequest("Bad user verification option");
                break;
        }
        
        authenticatorSelection.AuthenticatorAttachment = authType.ToEnum<AuthenticatorAttachment>();

        var extensions = new AuthenticationExtensionsClientInputs()
        {
            Extensions = true,
            UserVerificationMethod = true
        };

        var attestationConveyancePreference = AttestationConveyancePreference.None;
        switch (attType)
        {
            case "none":
                attestationConveyancePreference = AttestationConveyancePreference.None;
                break;
            case "indirect":
                attestationConveyancePreference = AttestationConveyancePreference.Indirect;
                break;
            case "direct":
                attestationConveyancePreference = AttestationConveyancePreference.Direct;
                break;
            default:
                BadRequest("Bad attestationConveyancePreference option");
                break;
        }   
        
        var credentialOptions = _fido2.RequestNewCredential(
            user.CreateFido2User(),
            excludedCreds.Select(credential => new PublicKeyCredentialDescriptor(credential.Id.ToByteArray())).ToList(),
            authenticatorSelection,
            attestationConveyancePreference,
            extensions
            );
        
        HttpContext.Session.SetString("fido2.attestationOptions", credentialOptions.ToJson());
        
        
        return Ok(credentialOptions);
    }
    
    [HttpPost]
    [Route("makeAttestation")]
    public async Task<ActionResult<Fido2.CredentialMakeResult>> MakeAttestationAsync([FromBody] AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken)
    {
        var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
        HttpContext.Session.Remove("fido2.attestationOptions");
        var options = CredentialCreateOptions.FromJson(jsonOptions);

        async Task<bool> Callback(IsCredentialIdUniqueToUserParams args, CancellationToken cT)
        {
            return await _credentialRepository.GetCountUsersByCredentialIdAsync(args.CredentialId, cT) <= 0;
        }

        var credentialResult = await _fido2.MakeNewCredentialAsync(
            attestationResponse,
            options,
            Callback,
            null,
            cancellationToken
            );

        if (!string.IsNullOrEmpty(credentialResult.ErrorMessage))
        {
            return BadRequest("Error while generating creds");
        }

        var creds = new Credential()
        {
            CreatedAtUtc = DateTime.Now,
            UpdatedAtUtc = DateTime.Now,
            Id = new Guid(TrimByteArray.TrimTo16Bytes(credentialResult.Result!.CredentialId)),
            CredentialId = credentialResult.Result!.CredentialId,
            PublicKey = credentialResult.Result.PublicKey,
            SignCounter = 0
        };
        
        var savedCreds = await _userRepository.AddCredentialsToUserAsync(options.User.Name, creds, cancellationToken);
        if (savedCreds is null)
        {
            return BadRequest("Error while saving credentials");
        }
        
        return Ok(credentialResult);
    }
    
    [HttpPost]
    [Route("makeAssertionOptions")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<ActionResult<AssertionOptions>> MakeAssertionOptionsAsync(
        [FromForm] string username,
        [FromQuery] UserVerificationRequirement userVerificationRequirement = UserVerificationRequirement.Preferred,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(username))
        {
            return BadRequest("Username is required to create an authentication options");
        }

        var user = await _userRepository.GetUserAsync(username, cancellationToken);
        if (user is null)
        {
            return BadRequest("User not found");
        }

        var existingCreds = await _credentialRepository.GetCredentialsForUser(user.Id, cancellationToken);
        List<PublicKeyCredentialDescriptor> allowedCreds;
        if (existingCreds is null)
        {
            allowedCreds = [];
            goto continuation;
        }

        allowedCreds = existingCreds.Select(existingCred => new PublicKeyCredentialDescriptor(existingCred.CredentialId)).ToList();

        continuation:
        var options = _fido2.GetAssertionOptions(allowedCreds, userVerificationRequirement);
        
        HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());
        HttpContext.Session.SetString("user", username);
        
        return Ok(options);
    }
    
    //model
    public class TokenizedAssertionVerificationResult
    {
        public string token { get; set; } = "";
        public AssertionVerificationResult assertionVerificationResult { get; set; } = new();
    }
    
    
    [HttpPost]
    [Route("makeAssertion")]
    public async Task<ActionResult<TokenizedAssertionVerificationResult>> MakeAssertionAsync(
        [FromBody] AuthenticatorAssertionRawResponse clientResponse, 
        CancellationToken cancellationToken = default)
    {
        var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
        var options = AssertionOptions.FromJson(jsonOptions);
        
        var username = HttpContext.Session.GetString("user");
        if (string.IsNullOrEmpty(username))
        {
            return BadRequest("Username in session is empty");
        }
        
        var credential = await _credentialRepository.GetCredentialByCredentialIdAsync(clientResponse.Id, cancellationToken);
        if (credential is null)
        {
            return BadRequest("Credential specified not found");
        }

        var signCount = credential.SignCounter;

        async Task<bool> Callback(IsUserHandleOwnerOfCredentialIdParams args, CancellationToken cT)
        {
            var name = args.UserHandle.ToString();
            
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Username empty in callback from library");
            }

            if (name != username)
            {
                throw new ArgumentException("Username in session and callback not same");
            }

            var storedCreds = await _userRepository.GetCredentialsByUserAsync(name, cT);
            if (storedCreds is null)
            {
                throw new ArgumentException("No credentials for current user");
            }

            return storedCreds.Any(c => c.PublicKey.SequenceEqual(args.CredentialId));
        }

        var res = await _fido2.MakeAssertionAsync(
            clientResponse,
            options,
            credential.PublicKey,
            signCount,
            Callback,
            cancellationToken: cancellationToken);
        
        var token = await tokenService.GenerateTokenAsync(username, cancellationToken);
        
        return Ok(
            new TokenizedAssertionVerificationResult {
                token = token,
                assertionVerificationResult = res
            }
        );
    }
}