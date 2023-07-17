#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

echo "##########VM NAME###########: $DB_BINDING_NAME"
echo "##########VM NAME###########: $VM_NAME"
echo "##########MACHINE_INDEX###########: $MACHINE_INDEX"
echo "##########VM_COUNT###########: $VM_COUNT"

echo "##########BENCHMARKING_TOOLS_BRANCH_NAME###########: $BENCHMARKING_TOOLS_BRANCH_NAME"
echo "##########BENCHMARKING_TOOLS_URL###########: $BENCHMARKING_TOOLS_URL"

export OSSProjectRef=True
export DOTNET_CLI_HOME=/temp
export RESULTS_PK="runs-summary" #For test runs use different one
export PL=18

#Cloning Test Bench Repo
echo "########## Cloning Test Bench repository ##########"
git clone https://github.com/Azure/azure-cosmos-dotnet-v3.git

# Build Benchmark Project
cd 'azure-cosmos-dotnet-v3/'
git checkout ${BENCHMARKING_TOOLS_BRANCH_NAME}

cd 'Microsoft.Azure.Cosmos.Samples/Tools/Benchmark'

echo "########## Build benckmark tool ##########"
dotnet build --configuration Release -p:"OSSProjectRef=true;ShouldUnsetParentConfigurationAndPlatform=false"

echo "########## Run benchmark ##########"
nohup dotnet run -c Release -e ${COSMOS_URI} -k ${COSMOS_KEY} -t ${THROUGHPUT} -n ${DOCUMENTS} --pl ${PARALLELISM} \
--enablelatencypercentiles true --resultscontainer ${RESULTS_CONTAINER} --resultspartitionkeyvalue "pk" \
--DiagnosticsStorageConnectionString ${DIAGNOSTICS_STORAGE_CONNECTION_STRING} \
--DiagnosticLatencyThresholdInMs ${DIAGNOSTICS_LATENCY_THRESHOLD_IN_MS} \
--DiagnosticsStorageContainerPrefix ${DIAGNOSTICS_STORAGE_CONTAINER_PREFIX} \
--MetricsReportingIntervalInSec ${METRICS_REPORTINT_INTERVAL_SEC} \
--AppInsightsInstrumentationKey ${APP_INSIGHT_CONN_STR} \
-w ${WORKLOAD_TYPE} \
> "/home/${ADMIN_USER_NAME}/agent.out" 2> "/home/${ADMIN_USER_NAME}/agent.err" &
