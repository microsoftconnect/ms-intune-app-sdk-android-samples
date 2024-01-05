// <copyright file="MainApplication.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Android.App;
using Android.Runtime;
using MauiMAM.Platforms.Android.Authentication;
using MauiMAM.Platforms.Android.Receivers;
using Microsoft.Intune.Mam.Client.App;
using Microsoft.Intune.Mam.Client.Notification;
using Microsoft.Intune.Mam.Client.Strict;
using Microsoft.Intune.Mam.Log;
using Microsoft.Intune.Mam.Policy.Notification;
using Microsoft.Intune.Mam.Policy;

namespace MauiMAM;

[Application(Debuggable = false)]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        MAMStrictMode.Enable();

        // Register the MAMAuthenticationCallback as soon as possible.
        // This will handle acquiring the necessary access token for MAM.
        IMAMEnrollmentManager mgr = MAMComponents.Get<IMAMEnrollmentManager>();
        IServiceProvider services = IPlatformApplication.Current.Application.Handler.MauiContext.Services;
        mgr.RegisterAuthenticationCallback(services.GetService<AuthenticationCallback>());

        // Register the notification receivers to receive MAM notifications.
        // Applications can receive notifications from the MAM SDK at any time.
        // More information can be found here: https://learn.microsoft.com/mem/intune/developer/app-sdk-android-phase4#register-for-notifications-from-the-sdk
        IMAMNotificationReceiverRegistry registry = MAMComponents.Get<IMAMNotificationReceiverRegistry>();
        registry.RegisterReceiver(services.GetService<EnrollmentNotificationReceiver>(), MAMNotificationType.MamEnrollmentResult);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
