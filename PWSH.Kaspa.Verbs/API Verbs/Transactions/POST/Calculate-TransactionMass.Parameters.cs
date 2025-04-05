using System.Management.Automation;
using PWSH.Kaspa.Constants;

namespace PWSH.Kaspa.Verbs
{
    public sealed partial class CalculateTransactionMass 
    {
        private static class ParameterSetNames
        {
            public const string Set0 = "set-0";
            public const string Set1 = "set-1";
            public const string Set2 = "set-2";
        }

/* -----------------------------------------------------------------
PARAMETERS                                                         |
----------------------------------------------------------------- */

        [ValidateNotNull]
        [Parameter(Mandatory = true, ParameterSetName = ParameterSetNames.Set0)]
        public GetBlock.BlockTransactionResponseSchema? GetBlockTransaction { get; set; }

        [ValidateNotNull]
        [Parameter(Mandatory = true, ParameterSetName = ParameterSetNames.Set1)]
        public GetBlocks.BlockTransactionResponseSchema? GetBlocksTransaction { get; set; }

        [ValidateNotNull]
        [Parameter(Mandatory = true, ParameterSetName = ParameterSetNames.Set2)]
        public GetBlocksFromBlueScore.BlockTransactionResponseSchema? GetBlocksFromBlueScoreTransaction { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Http client timeout.")]
        public ulong TimeoutSeconds { get; set; } = Globals.DEFAULT_TIMEOUT_SECONDS;

        [Parameter(Mandatory = false)]
        public SwitchParameter AsJob { get; set; }
    }
}
