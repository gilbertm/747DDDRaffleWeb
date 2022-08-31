# Blazor WebAssembly Boilerplate 
Built with .NET 6.0 and the goodness of MudBlazor Component Library. Incorporates the most essential Packages your projects will ever need. Follows Clean Architecture Principles.

## Goals

The goal of this repository is to help developers / companies kickstart their Web Application Development with a pre-built Blazor WebAssembly Boilerplate that includes several much needed components and features.

> Note that this is a frontend / client application only! The backend for this application is available in a seperate repository. 
> - Find fullstackhero's .NET 6 Web API Boilerplate here - https://github.com/fullstackhero/dotnet-webapi-boilerplate

## Prerequisites

- Make sure you have the API Running. Here is FSH Backend - https://github.com/fullstackhero/dotnet-webapi-boilerplate
- Once fullstackhero's .NET 6 Web API is up and running, run the Blazor WebAssembly Project to consume it's services.

## Getting Started

Open up your Command Prompt / Powershell and run the following command to install the solution template.

```powershell
dotnet new --install FullStackHero.BlazorWebAssembly.Boilerplate
```
or, if you want to use a specific version of the boilerplate, use

```powershell
dotnet new --install FullStackHero.BlazorWebAssembly.Boilerplate::0.0.1-rc
```
This would install the `fullstackhero Blazor WebAssembly Boilerplate` template globally on your machine. Do note that, at the time of writing this documentation, the latest available version is **0.0.1-rc** which is also one of the first stable pre-release version of the package. It is highly likely that there is already a newer version available when you are reading this.

> *To get the latest version of the package, visit [nuget.org](https://www.nuget.org/packages/FullStackHero.BlazorWebAssembly.Boilerplate/)*
>
> *FullStackHero.BlazorWebAssembly.Boilerplate is now in pre-release state. You can find the latest version on NuGet.org*

**FullStackHero.BlazorWebAssembly.Boilerplate::0.0.1-rc is compatible only with FullStackHero.WebAPI.Boilerplate::0.0.6-rc and above.**

Get the .NET WebApi Boilerplate by running the following command

```
dotnet new --install FullStackHero.WebAPI.Boilerplate::0.0.6-rc
```

For more details on getting started, [read this article](https://fullstackhero.net/blazor-webassembly-boilerplate/general/getting-started/)