// <copyright file="AuthenticationCallback.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Java.Net;
using Microsoft.Intune.Mam.Policy;

namespace MauiMAM.Platforms.Android.Authentication;

/// <summary>
/// Authentication callback for MAM token acquisition.
/// </summary>
/// <remarks>
/// This is required by the MAM SDK.
/// This callback must be registered in `onCreate` in the app's `MainApplication`.
/// See https://learn.microsoft.com/mem/intune/developer/app-sdk-android-phase4#mamenrollmentmanager-and-authentication .
/// </remarks>
public class AuthenticationCallback : Java.Lang.Object, IMAMServiceAuthenticationCallback
{
    private readonly Authenticator _authenticator;

    public AuthenticationCallback(Authenticator authenticator)
    {
        _authenticator = authenticator;
    }

    public string AcquireToken(string upn, string aadId, string resourceId)
    {
        return _authenticator.GetAccessTokenForMAM(aadId, resourceId).GetAwaiter().GetResult();
    }
}