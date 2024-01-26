// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    internal interface IActorBasedStrategy
    {
        internal bool InitializeNextIteration(uint iteration, ActorExecutionTrace prevActorTrace, ExecutionTrace prevTrace);

        internal bool Finalize(LogWriter logWriter);
    }
}
