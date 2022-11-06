using Xunit;
using Bunit;
using Bunit.TestDoubles;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;
using Microsoft.Extensions.DependencyInjection;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Preferences;
using Blazored.LocalStorage;
using MudBlazor;
using MudBlazor.Services;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth.AzureAd;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.JSInterop;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EHULOG.BlazorWebAssembly.Client.Infrastructure;
using System.Collections;
using Microsoft.AspNetCore.Components;
using OpenQA.Selenium.Chrome;
using Xunit.Abstractions;
using Xunit.Sdk;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Client.Tests;
using OpenQA.Selenium;
using EHULOG.BlazorWebAssembly.Client.Pages.Authentication;
using System;
using static MudBlazor.CategoryTypes;

namespace EHULOG.BlazorWebAssembly.Client.Tests;

[Collection("Sequence")]
public class TestAnonymousFrontPage : IDisposable
{

    private readonly BrowserOperations _browser = new BrowserOperations();
    private readonly string _testBlazorUrl = "https://localhost:5002/";

    [Fact]
    public void TestAnonymousFrontMainPage()
    {
        // Arrange
        _browser.InitBrowser();

        // Act
        _browser.Goto(_testBlazorUrl);
        System.Threading.Thread.Sleep(5000);

        string actualUrl = _browser.GetUrl;

        // Assert
        Assert.Equal(actualUrl, _testBlazorUrl);

    }

    [Fact]
    public void TestAnonymousFrontLoaded()
    {
        // Arrange
        _browser.InitBrowser();

        // Act
        _browser.Goto(_testBlazorUrl);
        IWebElement getAvailableLoansDisplayParagraph = _browser.WaitAndFindElement(By.XPath("//p[contains(text(), 'Available loans. Closest to your')]"));

        // Assert
        Assert.Contains("Available loans. Closest to your", getAvailableLoansDisplayParagraph.Text);

    }

    [Fact]
    public void TestAnonymousActivatorLoginRegisterButtonSuccessfulClickRedirectLogin()
    {
        // Arrange
        _browser.InitBrowser();

        // Act
        _browser.Goto(_testBlazorUrl);

        IWebElement activatorLoginRegisterIconLink = _browser.WaitAndFindElement(By.Id("activator-login-register"));
        activatorLoginRegisterIconLink.Click();

        IWebElement activatorLoginDropDownButtonLink = _browser.WaitAndFindElement(By.Id("activator-login-register--login"));
        activatorLoginDropDownButtonLink.Click();

        string actualUrl = _browser.GetUrl;

        // Assert
        Assert.NotEqual(actualUrl, _testBlazorUrl);

    }

    [Fact]
    public void TestAnonymousActivatorLoginRegisterButtonSuccessfulClickRedirectRegister()
    {
        // Arrange
        _browser.InitBrowser();

        // Act
        _browser.Goto(_testBlazorUrl);

        IWebElement activatorLoginRegisterIconLink = _browser.WaitAndFindElement(By.Id("activator-login-register"));
        activatorLoginRegisterIconLink.Click();

        IWebElement activatorLoginDropDownButtonLink = _browser.WaitAndFindElement(By.Id("activator-login-register--register"));
        activatorLoginDropDownButtonLink.Click();

        string actualUrl = _browser.GetUrl;

        // Assert
        Assert.NotEqual(actualUrl, _testBlazorUrl);

    }

    [Fact]
    public void TestAnonymousFrontPageLoadedWithMapIsWorking()
    {
        // Arrange
        _browser.InitBrowser();

        // Act
        _browser.Goto(_testBlazorUrl);
        IWebElement mapCanvas = _browser.WaitAndFindElement(By.XPath("//canvas[contains(@class,\"mapboxgl-canvas\")]"));
        IWebElement imageInsideMapCanvas = _browser.WaitAndFindElement(By.XPath("//div[contains(@class,\"marker mapboxgl-marker\")]"));

        string backgroundImage = imageInsideMapCanvas.GetCssValue("background-image");

        // Assert
        Assert.Contains("marker50px.png", backgroundImage);
    }

    public void Dispose()
    {
        _browser.Close();
    }
}