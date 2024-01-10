// <copyright file="Authenticator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Android.Widget;
using Microsoft.Identity.Client;
using Microsoft.Intune.Mam.Client.App;
using Microsoft.Intune.Mam.Policy;
using Application = MauiMAM.MainApplication;
using Resource = MauiMAM.Resource;

namespace MauiMAM.Platforms.Android.Authentication;

/// <summary>
/// Manages authentication for the app.
/// 
/// Deals with both MSAL and MAM, significantly.
/// </summary>
public class Authenticator
{
    private const string _placeholderClientID = "<placeholder_aad_client_id>";
    private const string _placeholderRedirectURI = "<placeholder_redirect_uri>";

    /// <summary>
    /// The authority for the MSAL AuthenticationContext. Sign in will use this URL.
    /// </summary>
    private const string _authority = "https://login.microsoftonline.com/common";

    /// <summary>
    /// Identifier of the client requesting the token. 
    /// The client ID must be registered at https://apps.dev.microsoft.com.
    /// </summary>
    /// <remarks>
    /// This ID is unique to this application and should be replaced wth the MSAL Application ID.
    /// </remarks>
    private const string _clientID = _placeholderClientID; //TODO - Replace with your value.

    /// <summary>
    /// Address to return to upon receiving a response from the authority.
    /// </summary>
    /// <remarks>
    /// This URI is configurable while registering this application with MSAL and should be replaced with the MSAL Redirect URI.
    /// </remarks>
    private const string _redirectURI = _placeholderRedirectURI; //TODO - Replace with your value.

    /// <summary>
    /// Identifier of the target resource that is the recipient of the requested token.
    /// </summary>
    private const string _resourceID = "https://graph.microsoft.com/";

    private static IPublicClientApplication pca;

    private string _cachedMamResourceId;
    private string _cachedUPN;
    private string _cachedAADID;

    private IEnumerable<string> _scopes = new string[] { _resourceID + ".default" };

    /// <summary>
    /// The current MAM user.
    /// </summary>
    /// <returns>The current user's username, null if it hasn't been found yet.</returns>
    public string User
    {
        get
        {
            IMAMUserInfo info = MAMComponents.Get<IMAMUserInfo>();
            return info?.PrimaryUser;
        }
    }

    /// <summary>
    /// The current Authentication Context.
    /// </summary>
    private static IPublicClientApplication AuthContext
    {
        get
        {
            pca ??= PublicClientApplicationBuilder
                            .Create(_clientID)
                            .WithAuthority(_authority)
                            .WithRedirectUri(_redirectURI)
                            .WithParentActivityOrWindow(() => MainActivity.Activity)
                            .Build();
            return pca;
        }
    }

    /// <summary>
    /// Authenticate by signing in the user and enrolling the user's account with MAM.
    /// </summary>
    /// <returns>The authentication result.</returns>
    public async Task<AuthenticationResult> Authenticate()
    {
        // Check initial authentication values.
        if (_clientID.Equals(_placeholderClientID) || _redirectURI.Equals(_placeholderRedirectURI))
        {
            Toast.MakeText(Application.Context, "Please update the authentication values for your application.", ToastLength.Long).Show();
            return null;
        }

        if (!Uri.IsWellFormedUriString(_redirectURI, UriKind.RelativeOrAbsolute))
        {
            Toast.MakeText(Application.Context, "Please correct the redirect URI for your application.", ToastLength.Long).Show();
            return null;
        }

        IEnumerable<IAccount> accounts = await AuthContext.GetAccountsAsync();
        IAccount knownAccount = null;

        // Found a single known account to use for authentication.
        if (accounts != null && accounts.Count() == 1)
        {
            knownAccount = accounts.First();
        }

        // Attempt to sign the user in silently.
        AuthenticationResult result = await SignInSilent(_scopes, knownAccount);

        // If the user cannot be signed in silently, prompt the user to manually sign in.
        result ??= await SignInWithPrompt(knownAccount);

        // If auth was successful, cache the values.
        if (result != null && result.AccessToken != null)
        {
            _cachedUPN = result.Account.Username;
            _cachedAADID = result.Account.HomeAccountId.ObjectId;

            // Register the account for MAM
            // See: https://learn.microsoft.com/mem/intune/developer/app-sdk-android-phase4#registering-for-app-protection-policy
            // This app requires MSAL authentication prior to MAM enrollment so we delay the registration
            // until after the sign in flow.
            IMAMEnrollmentManager mgr = MAMComponents.Get<IMAMEnrollmentManager>();
            mgr.RegisterAccountForMAM(_cachedUPN, _cachedAADID, result.TenantId);
        }

        return result;
    }

    /// <summary>
    /// Attempt to get a token from the cache without prompting the user for authentication.
    /// </summary>
    /// <param name="aadId"> The AAD ID for the user </param>
    /// <param name="resourceId"> The resource we're authenticating against to obtain a token </param>
    /// <returns> A token on success, null otherwise </returns>
    public async Task<string> GetAccessTokenForMAM(string aadId, string resourceId)
    {
        _cachedMamResourceId = resourceId;

        AuthenticationResult result = null;

        IEnumerable<IAccount> accounts = await AuthContext.GetAccountsAsync();
        IAccount account = accounts.Where((account) => account.HomeAccountId.ObjectId == aadId).FirstOrDefault();
        if (account == null)
            return null;

        // Attempt to acquire a token silently.
        var scopes = new string[] { resourceId + "/.default" };
        try
        {
            result = await AuthContext.AcquireTokenSilent(scopes, account).ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            // We failed to acquire the token silently, because interactive auth is required.
            // Try to start interactive auth.
            result = await AuthContext.AcquireTokenInteractive(scopes)
                .WithParentActivityOrWindow(MainActivity.Activity)
                .WithUseEmbeddedWebView(true)
                .WithAccount(account)
                .ExecuteAsync();
        }

        return result?.AccessToken;
    }

    /// <summary>
    /// Sign the user out and unenroll the user's account from MAM.
    /// </summary>
    public async Task SignOutAsync()
    {
        IEnumerable<IAccount> accts = await AuthContext.GetAccountsAsync();

        foreach (IAccount acct in accts)
        {
            // Clear the app's token cache so the user will be prompted to sign in again.
            await AuthContext.RemoveAsync(acct);
        }

        string user = User;
        if (user != null)
        {
            // Remove the user's MAM policy from the app
            IMAMEnrollmentManager mgr = MAMComponents.Get<IMAMEnrollmentManager>();
            mgr.UnregisterAccountForMAM(user, _cachedAADID);
        }

        Toast.MakeText(Application.Context, _Microsoft.Android.Resource.Designer.ResourceConstant.String.auth_out_success, ToastLength.Short).Show();
    }

    /// <summary>
    /// Attempt to get a token from the cache without prompting the user for authentication.
    /// </summary>
    /// <returns> A token on success, null otherwise </returns>
    public async void UpdateAccessTokenForMAM()
    {
        if (string.IsNullOrWhiteSpace(_cachedMamResourceId))
        {
            return;
        }

        string token = await GetAccessTokenForMAM(_cachedAADID, _cachedMamResourceId);

        if (!string.IsNullOrWhiteSpace(token))
        {
            IMAMEnrollmentManager mgr = MAMComponents.Get<IMAMEnrollmentManager>();
            mgr.UpdateToken(_cachedUPN, _cachedAADID, _cachedMamResourceId, token);
        }
    }

    /// <summary>
    /// Attempt silent authentication through the broker.
    /// </summary>
    /// <param name="scopes">The scopes for which we're trying to obtain a token</param>
    /// <param name="account">The account, null if not known</param>
    /// <returns> The AuthenticationResult on success, null otherwise</returns>
    private async Task<AuthenticationResult> SignInSilent(IEnumerable<string> scopes, IAccount account)
    {
        if (account == null)
        {
            return null;
        }

        try
        {
            return await AuthContext.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);
        }
        catch (MsalException)
        {
            // Expected if there is no token in the cache.
            return null;
        }
    }

    /// <summary>
    /// Attempt interactive authentication through the broker.
    /// </summary>
    /// <param name="account">Account to use, null if unknown.</param>
    /// <returns>The AuthenticationResult on success, null otherwise.</returns>
    private async Task<AuthenticationResult> SignInWithPrompt(IAccount account)
    {
        try
        {
            IEnumerable<IAccount> accounts = await AuthContext.GetAccountsAsync();

            return await AuthContext.AcquireTokenInteractive(_scopes)
                .WithParentActivityOrWindow(MainActivity.Activity)
                .WithUseEmbeddedWebView(true)
                .WithAccount(account)
                .ExecuteAsync();
        }
        catch (MsalException e)
        {
            string msg = Resource.String.err_auth + e.Message;
            Toast.MakeText(Application.Context, msg, ToastLength.Long).Show();
            return null;
        }
    }
}
