﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Telemetry
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Core.Trace;
    using Microsoft.Azure.Cosmos.Handler;
    using Microsoft.Azure.Cosmos.Query.Core.Monads;
    using Microsoft.Azure.Cosmos.Resource.Settings;
    using Microsoft.Azure.Cosmos.Routing;
    using Microsoft.Azure.Cosmos.Telemetry.Collector;
    using Microsoft.Azure.Cosmos.Tracing;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Collections;

    internal class TelemetryToServiceHelper : IClientTelemetryCollectors, IDisposable
    {
        internal static int DefaultBackgroundRefreshClientConfigTimeIntervalInMS = 10 * 60 * 1000;

        internal ClientTelemetry clientTelemetry = null;

        private readonly AuthorizationTokenProvider cosmosAuthorization;
        private readonly CosmosHttpClient httpClient;
        private readonly Uri serviceEnpoint;
        private readonly ConnectionPolicy connectionPolicy;
        private readonly string clientId;
        private readonly GlobalEndpointManager globalEndpointManager;
        private readonly CancellationTokenSource cancellationTokenSource;

        private Task accountClientConfigTask = null;

        internal TelemetryToServiceHelper()
        {
            //NoOp constructor
        }

        private TelemetryToServiceHelper(
            string clientId,
            ConnectionPolicy connectionPolicy,
            AuthorizationTokenProvider cosmosAuthorization,
            CosmosHttpClient httpClient,
            Uri serviceEndpoint,
            GlobalEndpointManager globalEndpointManager,
            CancellationTokenSource cancellationTokenSource)
        {
            this.clientId = clientId;
            this.cosmosAuthorization = cosmosAuthorization;
            this.httpClient = httpClient;
            this.connectionPolicy = connectionPolicy;
            this.serviceEnpoint = serviceEndpoint;
            this.globalEndpointManager = globalEndpointManager;
            this.cancellationTokenSource = cancellationTokenSource;
        }

        public static TelemetryToServiceHelper CreateAndInitializeClientConfigAndTelemetryJob(string clientId,
            ConnectionPolicy connectionPolicy,
            AuthorizationTokenProvider cosmosAuthorization,
            CosmosHttpClient httpClient,
            Uri serviceEndpoint,
            GlobalEndpointManager globalEndpointManager,
            CancellationTokenSource cancellationTokenSource)
        {
#if INTERNAL
            return new TelemetryToServiceHelper();
#else

            if (connectionPolicy.DisableClientTelemetryToService)
            {
                return new TelemetryToServiceHelper(); //NoOpscontructor
            }

            TelemetryToServiceHelper telemetryToServiceHelper = new TelemetryToServiceHelper(
                clientId, connectionPolicy, cosmosAuthorization, httpClient, serviceEndpoint, globalEndpointManager, cancellationTokenSource);

            telemetryToServiceHelper.Initialize();

            return telemetryToServiceHelper;
#endif
        }

        public bool IsClientTelemetryJobNotRunning()
        {
            return this.clientTelemetry == null;
        }

        public void CollectCacheInfo(Func<CacheTelemetryData> functionFordata)
        {
            if (this.IsClientTelemetryJobNotRunning())
            {
                return;
            }

            CacheTelemetryData data = functionFordata();

            if (data.collectionLink != null)
            {
                TelemetryToServiceHelper.GetDatabaseAndCollectionName(data.collectionLink, out string databaseName, out string collectionName);

                data.databaseId = databaseName;
                data.containerId = collectionName;
            }

            this.clientTelemetry.CollectCacheInfo(data);
        }

        public void CollectOperationInfo(Func<OperationTelemetryData> functionFordata)
        {
            if (this.IsClientTelemetryJobNotRunning())
            {
                return;
            }

            this.clientTelemetry.CollectOperationInfo(functionFordata());
        }

        private void Initialize()
        {
            this.accountClientConfigTask = this.RefreshDatabaseAccountClientConfigInternalAsync();
        }

        private async Task RefreshDatabaseAccountClientConfigInternalAsync()
        {
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                Uri serviceEndpointWithPath = new Uri(this.serviceEnpoint + Paths.ClientConfigPathSegment);

                TryCatch<AccountClientConfigProperties> databaseAccountClientConfigs = await this.GetDatabaseAccountClientConfigAsync(this.cosmosAuthorization, this.httpClient, serviceEndpointWithPath);
                if (databaseAccountClientConfigs.Succeeded)
                {
                    this.InitializeClientTelemetry(databaseAccountClientConfigs.Result);
                }
                else if (!this.cancellationTokenSource.IsCancellationRequested)
                {
                    DefaultTrace.TraceWarning($"Exception while calling client config " + databaseAccountClientConfigs.Exception.ToString());
                }

                await Task.Delay(TelemetryToServiceHelper.DefaultBackgroundRefreshClientConfigTimeIntervalInMS);
            }
        }

        private async Task<TryCatch<AccountClientConfigProperties>> GetDatabaseAccountClientConfigAsync(AuthorizationTokenProvider cosmosAuthorization,
            CosmosHttpClient httpClient,
            Uri serviceEndpoint)
        {
            Uri clientConfigEndpoint = new Uri(serviceEndpoint + Paths.ClientConfigPathSegment);

            INameValueCollection headers = new RequestNameValueCollection();
            await cosmosAuthorization.AddAuthorizationHeaderAsync(
                headersCollection: headers,
                clientConfigEndpoint,
                HttpConstants.HttpMethods.Get,
                AuthorizationTokenType.PrimaryMasterKey);

            using (ITrace trace = Trace.GetRootTrace("Account Client Config Read", TraceComponent.Transport, TraceLevel.Info))
            {
                try
                {
                    using (HttpResponseMessage responseMessage = await httpClient.GetAsync(
                        uri: clientConfigEndpoint,
                        additionalHeaders: headers,
                        resourceType: ResourceType.DatabaseAccount,
                        timeoutPolicy: HttpTimeoutPolicyControlPlaneRead.Instance,
                        clientSideRequestStatistics: null,
                        cancellationToken: default))
                    using (DocumentServiceResponse documentServiceResponse = await ClientExtensions.ParseResponseAsync(responseMessage))
                    {
                        return TryCatch<AccountClientConfigProperties>.FromResult(CosmosResource.FromStream<AccountClientConfigProperties>(documentServiceResponse));
                    }
                }
                catch (Exception ex)
                {
                    DefaultTrace.TraceWarning($"Exception while calling client config " + ex.StackTrace);
                    return TryCatch<AccountClientConfigProperties>.FromException(ex);
                }
            }
        }

        /// <summary>
        /// Trigger Client Telemetry job when it is enabled and not already running.
        /// </summary>
        private void InitializeClientTelemetry(AccountClientConfigProperties databaseAccountClientConfigs)
        {
            if (databaseAccountClientConfigs.IsClientTelemetryEnabled())
            {
                if (this.IsClientTelemetryJobNotRunning())
                {
                    try
                    {
                        this.clientTelemetry = ClientTelemetry.CreateAndStartBackgroundTelemetry(
                            clientId: this.clientId,
                            httpClient: this.httpClient,
                            userAgent: this.connectionPolicy.UserAgentContainer.BaseUserAgent,
                            connectionMode: this.connectionPolicy.ConnectionMode,
                            authorizationTokenProvider: this.cosmosAuthorization,
                            diagnosticsHelper: DiagnosticsHandlerHelper.Instance,
                            preferredRegions: this.connectionPolicy.PreferredLocations,
                            globalEndpointManager: this.globalEndpointManager,
                            databaseAccountClientConfigs: databaseAccountClientConfigs);

                        DefaultTrace.TraceVerbose("Client Telemetry Enabled.");
                    }
                    catch (Exception ex)
                    {
                        DefaultTrace.TraceWarning($"Error While starting Telemetry Job : {ex.Message}");
                    }
                }
                else
                {
                    DefaultTrace.TraceVerbose("Client Telemetry Job already running.");
                }
            }
            else
            {
                if (!this.IsClientTelemetryJobNotRunning())
                {
                    DefaultTrace.TraceInformation("Stopping Client Telemetry Job.");

                    this.clientTelemetry.Dispose();

                    this.clientTelemetry = null;
                }
                else
                {
                    DefaultTrace.TraceInformation("Client Telemetry Disabled.");
                }
            }
        }

        private static void GetDatabaseAndCollectionName(string path, out string databaseName, out string collectionName)
        {
            string[] segments = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            PathsHelper.ParseDatabaseNameAndCollectionNameFromUrlSegments(segments, out databaseName, out collectionName);
        }

        public void Dispose()
        {
            this.accountClientConfigTask = null;
            if (this.cancellationTokenSource != null && !this.cancellationTokenSource.IsCancellationRequested)
            {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
            }

            this.clientTelemetry?.Dispose();
        }

    }
}
