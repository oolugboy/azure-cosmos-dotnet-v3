﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Query.Core.CoordinatorDistributionPlan.ClientQL
{
    using System.Collections.Generic;

    internal class ClientQLUserDefinedFunctionCallScalarExpression : ClientQLScalarExpression
    {
        public ClientQLUserDefinedFunctionCallScalarExpression(ClientQLFunctionIdentifier identifier, List<ClientQLScalarExpression> vecArguments, bool builtin) 
            : base(ClientQLScalarExpressionKind.UserDefinedFunctionCall)
        {
            this.Identifier = identifier;
            this.VecArguments = vecArguments;
            this.Builtin = builtin;
        }

        public ClientQLFunctionIdentifier Identifier { get; }

        public List<ClientQLScalarExpression> VecArguments { get; }
        
        public bool Builtin { get; }
    }

}