// <copyright file="MenuPage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using MauiMAM.Platforms.Android.Authentication;

namespace MauiMAM.Views;

public partial class MenuPage : ContentPage
{
    private Authenticator authenticator;

    public MenuPage(Authenticator authenticator)
    {
        this.authenticator = authenticator;
        InitializeComponent();
    }

    private async void OnSignOutClicked(object sender, EventArgs e)
    {
        await authenticator.SignOutAsync();
        await Navigation.PushAsync(new MainPage(authenticator));
    }

}