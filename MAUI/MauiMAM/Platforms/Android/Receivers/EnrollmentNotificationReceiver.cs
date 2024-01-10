// <copyright file="EnrollmentNotificationReceiver.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Intune.Mam.Client.Notification;
using Microsoft.Intune.Mam.Policy.Notification;
using Microsoft.Intune.Mam.Policy;
using Android.Runtime;
using Android.OS;
using Android.Widget;
using Application = MauiMAM.MainApplication;
using Resource = MauiMAM.Resource;
using MauiMAM.Platforms.Android.Authentication;

namespace MauiMAM.Platforms.Android.Receivers;

/// <summary>
/// Receives enrollment notifications from the Intune service and performs the corresponding action for the enrollment result.
/// See: https://learn.microsoft.com/mem/intune/developer/app-sdk-android-phase7#implementing-mamnotificationreceiver
/// </summary>
class EnrollmentNotificationReceiver : Java.Lang.Object, IMAMNotificationReceiver
{
    private readonly Authenticator _authenticator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnrollmentNotificationReceiver"/> class.
    /// </summary>
    /// <param name="authenticator">IAuthenticator Instance.</param>
    public EnrollmentNotificationReceiver(Authenticator authenticator)
    {
        this._authenticator = authenticator;
    }

    /// <summary>
    /// When using the MAM-WE APIs found in IMAMEnrollManager, your app wil receive 
    /// IMAMEnrollmentNotifications back to signal the result of your calls.
    /// 
    /// More information can be found here: https://learn.microsoft.com/mem/intune/developer/app-sdk-android-phase4#registration-result-and-status-codes
    /// </summary>
    public bool OnReceive(IMAMNotification notification)
    {
        if (notification.Type != MAMNotificationType.MamEnrollmentResult)
        {
            return true;
        }

        IMAMEnrollmentNotification enrollmentNotification = notification.JavaCast<IMAMEnrollmentNotification>();
        IMAMEnrollmentManager.Result result = enrollmentNotification.EnrollmentResult;
        string upn = enrollmentNotification.UserIdentity;

        string message = string.Format(
            "Received MAM Enrollment result {0} for user {1}.", result.Name(), upn);

        Handler handler = new Handler(Application.Context.MainLooper);
        handler.Post(() => { Toast.MakeText(Application.Context, message, ToastLength.Long).Show(); });

        if (result.Equals(IMAMEnrollmentManager.Result.AuthorizationNeeded))
        {
            // Attempt to re-authorize.
            _authenticator.UpdateAccessTokenForMAM();
        }
        else if (result.Equals(IMAMEnrollmentManager.Result.EnrollmentFailed)
            || result.Equals(IMAMEnrollmentManager.Result.WrongUser))
        {
            string blockMessage = Application.Context.GetString(Resource.String.err_blocked, result.Name());
            BlockUser(handler, blockMessage);
        }

        return true;
    }

    /// <summary>
    /// Blocks the user from accessing the application.
    /// </summary>
    /// <remarks>
    /// In a real application, the user would need to be blocked from proceeding forward and accessing corporate data. 
    /// </remarks>
    /// <param name="handler">Associated handler.</param>
    /// <param name="blockMessage">Message to display to the user.</param>
    private void BlockUser(Handler handler, string blockMessage)
    {
        throw new NotImplementedException("Add your own implementation here.");
    }
}