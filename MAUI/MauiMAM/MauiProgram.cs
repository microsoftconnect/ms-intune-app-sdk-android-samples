// <copyright file="MauiProgram.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

using MauiMAM.Platforms.Android.Authentication;
using MauiMAM.Platforms.Android.Receivers;
using MauiMAM.Views;

namespace MauiMAM;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        }).UseMauiCommunityToolkit();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<MainPage>();

        builder.Services.AddSingleton<MenuPage>();
        builder.Services.AddSingleton<Authenticator>();
        builder.Services.AddSingleton<AuthenticationCallback>();
        builder.Services.AddSingleton<EnrollmentNotificationReceiver>();

        return builder.Build();
    }
}