using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Client.Tests;
public class BrowserOperations
{
    private const int WaitForElementTimeout = 30;

    private IWebDriver WebDriver { get; set; } = default!;
    private WebDriverWait WebDriverWait { get; set; } = default!;

    public void InitBrowser()
    {
        new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);

        WebDriver = new ChromeDriver();
        WebDriverWait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(WaitForElementTimeout));
        WebDriver.Manage().Window.Maximize();
    }

    public string Title
    {
        get { return WebDriver.Title; }
    }

    public void Goto(string url)
    {
        WebDriver.Url = url;
    }

    public string GetUrl
    {
        get { return WebDriver.Url; }
    }

    public void Close()
    {
        WebDriver.Quit();
    }

    public IWebDriver getDriver
    {
        get { return WebDriver; }
    }

    public IWebElement WaitAndFindElement(By locator)
    {
        return WebDriverWait.Until(ExpectedConditions.ElementExists(locator));
    }
}