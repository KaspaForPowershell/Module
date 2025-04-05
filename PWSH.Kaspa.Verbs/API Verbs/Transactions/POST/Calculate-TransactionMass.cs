using System.Management.Automation;
using System.Text.Json;
using PWSH.Kaspa.Base;
using PWSH.Kaspa.Constants;

namespace PWSH.Kaspa.Verbs
{
    [CmdletBinding(DefaultParameterSetName = ParameterSetNames.Set0)]
    [Cmdlet(KaspaVerbNames.Calculate, "TransactionMass")]
    [OutputType(typeof(ResponseSchema))]
    public sealed partial class CalculateTransactionMass : KaspaPSCmdlet
    {
        private KaspaJob<ResponseSchema?>? _job;

/* -----------------------------------------------------------------
CONSTRUCTORS                                                       |
----------------------------------------------------------------- */

        public CalculateTransactionMass()
        {
            this._httpClient = KaspaModuleInitializer.Instance?.HttpClient;
            this._deserializerOptions = KaspaModuleInitializer.Instance?.ResponseDeserializer;

            if (this._httpClient is null || this._deserializerOptions is null)
                ThrowTerminatingError(new ErrorRecord(new NullReferenceException(), "NullHttpClient", ErrorCategory.InvalidOperation, this));
        }

/* -----------------------------------------------------------------
PROCESS                                                            |
----------------------------------------------------------------- */

        protected override void BeginProcessing()
        {
            async Task<(ResponseSchema?, ErrorRecord?)> processLogic(CancellationToken cancellation_token) { return await DoProcessLogicAsync(this._httpClient!, this._deserializerOptions!, cancellation_token); }

            var thisName = this.MyInvocation.MyCommand.Name;
            this._job = new KaspaJob<ResponseSchema?>(processLogic, thisName);
        }

        protected override void ProcessRecord()
        {
            var stoppingToken = this.CreateStoppingToken();

            if (AsJob.IsPresent)
            {
                if (this._job is null)
                {
                    WriteError(new ErrorRecord(new NullReferenceException("The job was not initialized."), "JobExecutionFailure", ErrorCategory.InvalidOperation, this));
                    return;
                }

                JobRepository.Add(this._job);
                WriteObject(this._job);

                var jobTask = Task.Run(async () => await this._job.ProcessJob(stoppingToken));
                jobTask.ContinueWith(t =>
                {
                    if (t.Exception is not null) WriteError(new ErrorRecord(t.Exception, "JobExecutionFailure", ErrorCategory.OperationStopped, this));
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            else
            {
                var (responce, err) = DoProcessLogicAsync(this._httpClient!, this._deserializerOptions!, stoppingToken).GetAwaiter().GetResult();
                if (err is not null)
                {
                    WriteError(err);
                    return;
                }

                WriteObject(responce);
            }
        }

/* -----------------------------------------------------------------
HELPERS                                                            |
----------------------------------------------------------------- */

        protected override string BuildQuery()
            => "transactions/mass";

        private (RequestSchema?, ErrorRecord?) BuildRequest()
        {
            switch (this.ParameterSetName)
            {
                case ParameterSetNames.Set0:
                    {
                        var err = HandleGetBlockTransaction(out var request);
                        return (request, err);
                    }

                case ParameterSetNames.Set1:
                    {
                        var err = HandleGetBlocksTransaction(out var request);
                        return (request, err);
                    }

                case ParameterSetNames.Set2:
                    {
                        var err = HandleGetBlocksFromBlueScoreTransaction(out var request);
                        return (request, err);
                    }

                default:
                    return (null, new ErrorRecord(new NotSupportedException($"Parameter set '{this.ParameterSetName}' is not supported."), "NotSupportedCase", ErrorCategory.InvalidOperation, this));
            }
        }

        private ErrorRecord? HandleGetBlockTransaction(out RequestSchema? request)
        {
            if (GetBlockTransaction is null)
            {
                request = null;
                return new ErrorRecord(new NullReferenceException("Processed transaction is null."), "NullParameter", ErrorCategory.InvalidOperation, this);
            }

            request = new()
            {
                Version = GetBlockTransaction.Version,
                SubnetworkID = GetBlockTransaction.SubnetworkID,
                LockTime = GetBlockTransaction.LockTime
            };

            var inputs = GetBlockTransaction.Inputs;
            if (inputs is not null)
            {
                request.Inputs = [];

                foreach (var input in inputs)
                {
                    var newRequestInput = new TransactionInputRequestSchema { SignatureScript = input.SignatureScript };

                    if (input.PreviousOutpoint is not null)
                        newRequestInput.PreviousOutpoint = new()
                        {
                            Index = input.PreviousOutpoint.Index,
                            TransactionID = input.PreviousOutpoint.TransactionID
                        };

                    newRequestInput.Sequence = input.Sequence;

                    newRequestInput.SigOpCount = input.SigOpCount;

                    request.Inputs.Add(newRequestInput);
                }
            }

            var outputs = GetBlockTransaction.Outputs;
            if (outputs is not null)
            {
                request.Outputs = [];

                foreach (var output in outputs)
                {
                    var newRequestOutput = new TransactionOutputRequestSchema { Amount = output.Amount };

                    if (output.ScriptPublicKey is not null)
                        newRequestOutput.ScriptPublicKey = new ScriptPublicKeyRequestSchema()
                        {
                            ScriptPublicKey = output.ScriptPublicKey.ScriptPublicKey,
                            Version = output.ScriptPublicKey.Version
                        };

                    request.Outputs.Add(newRequestOutput);
                }
            }

            return null;
        }

        private ErrorRecord? HandleGetBlocksTransaction(out RequestSchema? request)
        {
            if (GetBlocksTransaction is null)
            {
                request = null;
                return new ErrorRecord(new NullReferenceException("Processed transaction is null."), "NullParameter", ErrorCategory.InvalidOperation, this);
            }

            request = new()
            {
                Version = GetBlocksTransaction.Version,
                SubnetworkID = GetBlocksTransaction.SubnetworkID,
                LockTime = GetBlocksTransaction.LockTime
            };

            var inputs = GetBlocksTransaction.Inputs;
            if (inputs is not null)
            {
                request.Inputs = [];

                foreach (var input in inputs)
                {
                    var newRequestInput = new TransactionInputRequestSchema();

                    if (input.PreviousOutpoint is not null)
                        newRequestInput.PreviousOutpoint = new()
                        {
                            Index = input.PreviousOutpoint.Index,
                            TransactionID = input.PreviousOutpoint.TransactionID
                        };

                    newRequestInput.SignatureScript = input.SignatureScript;

                    newRequestInput.Sequence = input.Sequence;
                    newRequestInput.SigOpCount = input.SigOpCount;

                    request.Inputs.Add(newRequestInput);
                }
            }

            var outputs = GetBlocksTransaction.Outputs;
            if (outputs is not null)
            {
                request.Outputs = [];

                foreach (var output in outputs)
                {
                    var newRequestOutput = new TransactionOutputRequestSchema { Amount = output.Amount };

                    if (output.ScriptPublicKey is not null)
                        newRequestOutput.ScriptPublicKey = new ScriptPublicKeyRequestSchema()
                        {
                            ScriptPublicKey = output.ScriptPublicKey.ScriptPublicKey,
                            Version = output.ScriptPublicKey.Version
                        };

                    request.Outputs.Add(newRequestOutput);
                }
            }

            return null;
        }

        private ErrorRecord? HandleGetBlocksFromBlueScoreTransaction(out RequestSchema? request)
        {
            if (GetBlocksFromBlueScoreTransaction is null)
            {
                request = null;
                return new ErrorRecord(new NullReferenceException("Processed transaction is null."), "NullParameter", ErrorCategory.InvalidOperation, this);
            }

            request = new()
            {
                Version = GetBlocksFromBlueScoreTransaction.Version,
                SubnetworkID = GetBlocksFromBlueScoreTransaction.SubnetworkID,
                LockTime = GetBlocksFromBlueScoreTransaction.LockTime
            };

            var inputs = GetBlocksFromBlueScoreTransaction.Inputs;
            if (inputs is not null)
            {
                request.Inputs = [];

                foreach (var input in inputs)
                {
                    var newRequestInput = new TransactionInputRequestSchema();

                    if (input.PreviousOutpoint is not null)
                        newRequestInput.PreviousOutpoint = new()
                        {
                            Index = input.PreviousOutpoint.Index,
                            TransactionID = input.PreviousOutpoint.TransactionID
                        };

                    if (input.SignatureScript is not null)
                        newRequestInput.SignatureScript = input.SignatureScript;

   
                    newRequestInput.Sequence = input.Sequence;
                    newRequestInput.SigOpCount = input.SigOpCount;

                    request.Inputs.Add(newRequestInput);
                }
            }

            var outputs = GetBlocksFromBlueScoreTransaction.Outputs;
            if (outputs is not null)
            {
                request.Outputs = [];

                foreach (var output in outputs)
                {
                    var newRequestOutput = new TransactionOutputRequestSchema { Amount = output.Amount };

                    if (output.ScriptPublicKey is not null)
                        newRequestOutput.ScriptPublicKey = new ScriptPublicKeyRequestSchema()
                        {
                            ScriptPublicKey = output.ScriptPublicKey.ScriptPublicKey,
                            Version = output.ScriptPublicKey.Version
                        };

                    request.Outputs.Add(newRequestOutput);
                }
            }

            return null;
        }

        private async Task<(ResponseSchema?, ErrorRecord?)> DoProcessLogicAsync(HttpClient http_client, JsonSerializerOptions deserializer_options, CancellationToken cancellation_token)
        {
            try
            {
                var (requestSchema, err) = BuildRequest();
                if (err is not null) return (default, err);
               
                (var request, err) = await http_client.SendRequestAsync(this, Globals.KASPA_API_ADDRESS, BuildQuery(), HttpMethod.Post, requestSchema, TimeoutSeconds, cancellation_token);
                if (err is not null) return (default, err);
                if (request is null) return (default, new ErrorRecord(new InvalidOperationException("Received a null response from the API."), "TaskNull", ErrorCategory.InvalidOperation, this));

                (var response, err) = await request.ProcessResponseAsync<ResponseSchema>(deserializer_options, this, TimeoutSeconds, cancellation_token);
                if (err is not null) return (default, err);
                if (response is null) return (default, new ErrorRecord(new InvalidOperationException("The API response was not initialized."), "TaskNull", ErrorCategory.InvalidOperation, this));

                return (response, null);
            }
            catch (OperationCanceledException)
            { return (default, new ErrorRecord(new OperationCanceledException("Task was canceled."), "TaskCanceled", ErrorCategory.OperationStopped, this)); }
            catch (Exception e)
            { return (default, new ErrorRecord(e, "TaskInvalid", ErrorCategory.InvalidOperation, this)); }
        }
    }
}
