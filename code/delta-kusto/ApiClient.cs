﻿using DeltaKustoIntegration;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal class ApiClient
    {
        #region Inner Types
        private class ClientInfo
        {
            public string ClientVersion { get; set; } = Program.AssemblyVersion;

            public string OS { get; set; } = Environment.OSVersion.Platform.ToString();

            public string OsVersion { get; set; } = Environment.OSVersion.VersionString;
        }

        private class ApiInfo
        {
            public string ApiVersion { get; set; } = string.Empty;
        }

        private class ActivationOutput
        {
            public ApiInfo ApiInfo { get; set; } = new ApiInfo();

            public string SessionId { get; set; } = string.Empty;

            public IImmutableList<string> NewestVersions { get; set; } = ImmutableArray<string>.Empty;
        }

        private class ErrorInput
        {
            public ErrorInput(string sessionId, Exception ex)
            {
                SessionId = sessionId;
                Source = ex.Source ?? string.Empty;
                Exceptions = ExceptionInfo.FromException(ex);
            }

            public string SessionId { get; set; } = string.Empty;

            public string Source { get; set; }

            public ExceptionInfo[] Exceptions { get; set; }
        }

        private class ExceptionInfo
        {
            private ExceptionInfo(Exception ex)
            {
                Message = ex.Message;
                ExceptionType = ex.GetType().FullName ?? string.Empty;
                StackTrace = ex.StackTrace ?? string.Empty;
            }

            internal static ExceptionInfo[] FromException(Exception ex)
            {
                var list = new List<ExceptionInfo>();
                Exception? current = ex;

                while (current != null)
                {
                    list.Add(new ExceptionInfo(current));
                    current = current.InnerException;
                }

                return list.ToArray();
            }

            public string Message { get; set; }

            public string ExceptionType { get; set; }

            public string StackTrace { get; set; }
        }

        private class ErrorOutput
        {
            public ApiInfo ApiInfo { get; set; } = new ApiInfo();

            public Guid OperationID { get; set; } = Guid.NewGuid();
        }

        private class EndSessionInput
        {
            public EndSessionInput(string sessionId, bool isSuccess)
            {
                SessionId = sessionId;
                IsSuccess = isSuccess;
            }

            public string SessionId { get; set; } = string.Empty;

            public bool IsSuccess { get; set; }
        }

        private class EndSessionOutput
        {
            public ApiInfo ApiInfo { get; set; } = new ApiInfo();
        }

        private class LogParameterTelemetryInput
        {
            public LogParameterTelemetryInput(string sessionId, MainParameterization parameters)
            {
                SessionId = sessionId;
                SendErrorOptIn = parameters.SendErrorOptIn;
                FailIfDataLoss = parameters.FailIfDataLoss;
                TokenProvider = ExtractTokenProvider(parameters.TokenProvider);
                Jobs = parameters.Jobs.Values.Select(j => new JobInfo(j)).ToArray();
            }

            public string SessionId { get; set; } = string.Empty;

            public bool SendErrorOptIn { get; set; }

            public bool FailIfDataLoss { get; set; }

            public string TokenProvider { get; set; }

            public JobInfo[] Jobs { get; set; }

            private string ExtractTokenProvider(TokenProviderParameterization tokenProvider)
            {
                if (tokenProvider.Login != null)
                {
                    return "Login";
                }
                else if (tokenProvider.Tokens != null)
                {
                    return "Tokens";
                }
                else
                {
                    return "None";
                }
            }
        }

        private class JobInfo
        {
            public JobInfo(JobParameterization job)
            {
                Current = ExtractSource(job.Current);
                Target = ExtractSource(job.Target);
                FilePath = job.Action!.FilePath != null;
                FolderPath = job.Action!.FolderPath != null;
                CsvPath = job.Action!.CsvPath != null;
                UsePluralForms = job.Action!.UsePluralForms;
                PushToConsole = job.Action!.PushToConsole;
            }

            public string Current { get; set; }

            public string Target { get; set; }

            public bool FilePath { get; set; }

            public bool FolderPath { get; set; }

            public bool CsvPath { get; set; }

            public bool UsePluralForms { get; set; }

            public bool PushToConsole { get; set; }

            public bool PushToCurrent { get; set; }

            private string ExtractSource(SourceParameterization? current)
            {
                if (current == null)
                {
                    return "None";
                }
                else if (current.Adx != null)
                {
                    return "Cluster";
                }
                else if (current.Scripts != null)
                {
                    if (current.Scripts.FirstOrDefault() != null)
                    {
                        if (current.Scripts.First().FilePath != null)
                        {
                            return "File";
                        }
                        else
                        {
                            return "Folder";
                        }
                    }
                    else
                    {
                        return "NoScript";
                    }
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        private class LogParameterTelemetryOutput
        {
            public ApiInfo ApiInfo { get; set; } = new ApiInfo();
        }
        #endregion

        private const string DEFAULT_ROOT_URL = "https://delta-kusto.azurefd.net/";
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(10);

        private static readonly string ROOT_URL = ComputeRootUrl();
        private static readonly bool _doApiCalls = ComputeDoApiCalls();

        private readonly ITracer _tracer;
        private readonly SimpleHttpClientFactory _httpClientFactory;
        private string _sessionId = string.Empty;

        public ApiClient(ITracer tracer, SimpleHttpClientFactory httpClientFactory)
        {
            _tracer = tracer;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IImmutableList<string>?> ActivateAsync()
        {
            if (_doApiCalls)
            {
                var tokenSource = new CancellationTokenSource(TIMEOUT);
                var ct = tokenSource.Token;

                _tracer.WriteLine(true, "ActivateAsync - Start");
                try
                {
                    var output = await PostAsync<ActivationOutput>(
                        "/activation",
                        new
                        {
                            ClientInfo = new ClientInfo()
                        },
                        ct);

                    _tracer.WriteLine(true, "ActivateAsync - End");

                    if (output != null)
                    {
                        _sessionId = output.SessionId;

                        return output.NewestVersions;
                    }
                }
                catch
                {
                    _tracer.WriteLine(true, "ActivateAsync - Failed");
                }
            }

            return null;
        }

        public async Task LogParameterTelemetryAsync(MainParameterization parameters)
        {
            if (_doApiCalls)
            {
                var tokenSource = new CancellationTokenSource(TIMEOUT);
                var ct = tokenSource.Token;

                _tracer.WriteLine(true, "LogParameterTelemetryAsync - Start");
                try
                {
                    var output = await PostAsync<LogParameterTelemetryOutput>(
                        "/logparametertelemetry",
                        new LogParameterTelemetryInput(_sessionId, parameters), ct);

                    _tracer.WriteLine(true, "LogParameterTelemetryAsync - End");
                }
                catch
                {
                    _tracer.WriteLine(true, "LogParameterTelemetryAsync - Failed");
                }
            }
        }

        public async Task<string?> RegisterExceptionAsync(Exception ex)
        {
            if (_doApiCalls)
            {
                var tokenSource = new CancellationTokenSource(TIMEOUT);
                var ct = tokenSource.Token;

                _tracer.WriteLine(true, "RegisterExceptionAsync - Start");
                try
                {
                    var output = await PostAsync<ErrorOutput>(
                        "/error",
                        new ErrorInput(_sessionId, ex), ct);

                    _tracer.WriteLine(true, "RegisterExceptionAsync - End");

                    return _sessionId;
                }
                catch
                {
                    _tracer.WriteLine(true, "RegisterExceptionAsync - Failed");
                }
            }

            return null;
        }

        public async Task EndSessionAsync(bool success)
        {
            if (_doApiCalls)
            {
                var tokenSource = new CancellationTokenSource(TIMEOUT);
                var ct = tokenSource.Token;

                _tracer.WriteLine(true, "EndSessionAsync - Start");
                try
                {
                    var output = await PostAsync<EndSessionOutput>(
                        "/endsession",
                        new EndSessionInput(_sessionId, success), ct);

                    _tracer.WriteLine(true, "EndSessionAsync - End");
                }
                catch
                {
                    _tracer.WriteLine(true, "EndSessionAsync - Failed");
                }
            }
        }

        private static string ComputeRootUrl()
        {
            return Environment.GetEnvironmentVariable("api-url") ?? DEFAULT_ROOT_URL;
        }

        private static bool ComputeDoApiCalls()
        {
            return Environment.GetEnvironmentVariable("disable-api-calls") != "true";
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        private async Task<T?> GetAsync<T>(
            string urlSuffix,
            CancellationToken ct,
            params (string name, string value)[] queryParameters)
            where T : class
        {
            try
            {
                using (var client = _httpClientFactory.CreateHttpClient())
                {
                    var queryParametersText = queryParameters
                        .Select(q => WebUtility.UrlEncode(q.name) + "=" + WebUtility.UrlEncode(q.value));
                    var url = new Uri(new Uri(ROOT_URL), urlSuffix);
                    var urlWithQuery = new Uri(url, "?" + string.Join("&", queryParametersText));

                    var response = await client.GetAsync(
                        urlWithQuery,
                        HttpCompletionOption.ResponseContentRead,
                        ct);
                    var responseText =
                        await response.Content.ReadAsStringAsync(ct);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var output = JsonSerializer.Deserialize<T>(
                            responseText,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        return output!;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        private async Task<T?> PostAsync<T>(
            string urlSuffix,
            object telemetry,
            CancellationToken ct)
            where T : class
        {
            try
            {
                var bodyText = JsonSerializer.Serialize(
                    telemetry,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                using (var client = _httpClientFactory.CreateHttpClient())
                {
                    var url = new Uri(new Uri(ROOT_URL), urlSuffix);
                    var response = await client.PostAsync(
                        url,
                        new StringContent(bodyText, null, "application/json"),
                        ct);
                    var responseText =
                        await response.Content.ReadAsStringAsync(ct);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var output = JsonSerializer.Deserialize<T>(
                            responseText,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        return output!;
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}