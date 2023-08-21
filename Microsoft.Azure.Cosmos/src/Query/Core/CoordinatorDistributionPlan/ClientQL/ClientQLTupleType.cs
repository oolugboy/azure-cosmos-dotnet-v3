﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Query.Core.CoordinatorDistributionPlan.ClientQL
{
    using System.Collections.Generic;

    internal class ClientQLTupleType : ClientQLType
    {
        public ClientQLTupleType(List<ClientQLType> vecTypes)
            : base(ClientQLTypeKind.Tuple)
        {
            this.VecTypes = vecTypes;
        }

        public List<ClientQLType> VecTypes { get; }
    }

}