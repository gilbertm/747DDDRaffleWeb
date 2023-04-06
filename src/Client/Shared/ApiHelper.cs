﻿using RAFFLE.BlazorWebAssembly.Client.Components.Common;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using MudBlazor;

namespace RAFFLE.BlazorWebAssembly.Client.Shared;

public static class ApiHelper
{
    public static async Task<T?> ExecuteCallGuardedAsync<T>(
        Func<Task<T>> call,
        ISnackbar snackbar,
        CustomValidation? customValidation = null,
        string? successMessage = null)
    {
        customValidation?.ClearErrors();
        try
        {
            var result = await call();

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                snackbar.Add(successMessage, Severity.Info);
            }

            return result;
        }
        catch (ApiException<HttpValidationProblemDetails> ex)
        {
            if (ex.Result.Errors is not null)
            {
                customValidation?.DisplayErrors(ex.Result.Errors);
            }
            else
            {
                snackbar.Add("Something went wrong!", Severity.Error);
            }
        }
        catch (ApiException<ErrorResult> ex)
        {
            snackbar.Add(ex.Result.Exception, Severity.Error);
        }
        catch (Exception ex)
        {
            snackbar.Add(ex.Message, Severity.Error);
        }

        return default;
    }

    public static async Task<bool> ExecuteCallGuardedAsync(
        Func<Task> call,
        ISnackbar snackbar,
        CustomValidation? customValidation = null,
        string? successMessage = null)
    {
        customValidation?.ClearErrors();
        try
        {
            await call();

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                snackbar.Add(successMessage, Severity.Success);
            }

            return true;
        }
        catch (ApiException<HttpValidationProblemDetails> ex)
        {
            if (ex.Result.Errors is not null)
            {
                customValidation?.DisplayErrors(ex.Result.Errors);
            }
            else
            {
                snackbar.Add("Something went wrong!", Severity.Error);
            }
        }
        catch (ApiException<ErrorResult> ex)
        {
            snackbar.Add(ex.Result.Exception, Severity.Error);
        }

        return false;
    }

    public static async Task<T?> ExecuteCallGuardedCustomSuppressAsync<T>(
        Func<Task<T>> call,
        ISnackbar snackbar,
        CustomValidation? customValidation = null,
        string? successMessage = null)
    {
        customValidation?.ClearErrors();
        try
        {
            var result = await call();

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                snackbar.Add(successMessage, Severity.Info);
            }

            return result;
        }
        catch (ApiException<HttpValidationProblemDetails> ex)
        {
            if (ex.Result.Errors is not null)
            {
                customValidation?.DisplayErrors(ex.Result.Errors);
            }
            else
            {
            }
        }
        catch (ApiException<ErrorResult>)
        {
        }
        catch (Exception)
        {
        }

        return default;
    }

    public static async Task<bool> ExecuteCallGuardedCustomSuppressAsync(
        Func<Task> call,
        ISnackbar snackbar,
        CustomValidation? customValidation = null,
        string? successMessage = null)
    {
        customValidation?.ClearErrors();
        try
        {
            await call();

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                snackbar.Add(successMessage, Severity.Success);
            }

            return true;
        }
        catch (ApiException<HttpValidationProblemDetails> ex)
        {
            if (ex.Result.Errors is not null)
            {
                customValidation?.DisplayErrors(ex.Result.Errors);
            }
        }
        catch (ApiException<ErrorResult>)
        {
        }

        return false;
    }
}