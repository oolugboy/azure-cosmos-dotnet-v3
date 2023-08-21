﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Query.Core.CoordinatorDistributionPlan.ClientQL
{
    using System.Collections.Generic;

    internal class ClientQLObjectLiteral : ClientQLLiteral
    {
        public ClientQLObjectLiteral(IReadOnlyList<ClientQLObjectLiteral> vecProperties)
            : base(ClientQLLiteralKind.Object)
        {
            this.VecProperties = vecProperties;
        }

        public IReadOnlyList<ClientQLObjectLiteral> VecProperties { get; }
    }
}