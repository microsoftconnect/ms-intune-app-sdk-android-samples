// <copyright file="MainPage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MauiMAM.Platforms.Android.Authentication;
using Microsoft.Identity.Client;

namespace MauiMAM.Views;

public partial class MainPage : ContentPage
{
    private Authenticator authenticator;

    public MainPage(Authenticator authenticator)
    {
        this.authenticator = authenticator;
        InitializeComponent();
    }

    private async void OnSignInClicked(object sender, EventArgs e)
    {
        AuthenticationResult authenticationResult = await authenticator.Authenticate();

        // If the authentication was successful then enter the application.
        if (authenticationResult != null && authenticationResult.AccessToken != null)
        {
            await Navigation.PushAsync(new MenuPage(authenticator));
        }
        else
        {
            await Toast.Make("Authentication failed.", ToastDuration.Long).Show();
        }
    }
}
