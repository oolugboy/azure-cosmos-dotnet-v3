﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Query.Core.CoordinatorDistributionPlan.ClientQL
{
    internal class ClientQLInputEnumerableExpression : ClientQLEnumerableExpression
    {
        public ClientQLInputEnumerableExpression(string name) 
            : base(ClientQLEnumerableExpressionKind.Input)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

}