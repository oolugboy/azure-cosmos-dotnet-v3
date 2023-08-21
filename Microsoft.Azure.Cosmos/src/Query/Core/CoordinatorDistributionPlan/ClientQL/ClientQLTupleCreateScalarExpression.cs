﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Query.Core.CoordinatorDistributionPlan.ClientQL
{
    using System.Collections.Generic;

    internal class ClientQLTupleCreateScalarExpression : ClientQLScalarExpression
    {
        public ClientQLTupleCreateScalarExpression(List<ClientQLScalarExpression> vecItems) 
            : base(ClientQLScalarExpressionKind.TupleCreate)
        {
            this.VecItems = vecItems;
        }

        public List<ClientQLScalarExpression> VecItems { get; }
    }

}