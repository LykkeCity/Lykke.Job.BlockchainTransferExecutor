﻿using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;
using Lykke.Job.BlockchainOperationsExecutor.Core.Services.Blockchains;
using Lykke.Job.BlockchainOperationsExecutor.Workflow.Commands;
using Lykke.Job.BlockchainOperationsExecutor.Workflow.Events;
using Lykke.Service.BlockchainApi.Client.Models;

namespace Lykke.Job.BlockchainOperationsExecutor.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class BroadcastTransactionCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly IBlockchainApiClientProvider _apiClientProvider;
        private readonly RetryDelayProvider _retryDelayProvider;

        public BroadcastTransactionCommandsHandler(
            IChaosKitty chaosKitty,
            ILog log,
            IBlockchainApiClientProvider apiClientProvider, 
            RetryDelayProvider retryDelayProvider)
        {
            _chaosKitty = chaosKitty;
            _log = log.CreateComponentScope(nameof(BroadcastTransactionCommandsHandler));
            _apiClientProvider = apiClientProvider;
            _retryDelayProvider = retryDelayProvider;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(BroadcastTransactionCommand command, IEventPublisher publisher)
        {
            var apiClient = _apiClientProvider.Get(command.BlockchainType);
            var broadcastingResult = await apiClient.BroadcastTransactionAsync(command.TransactionId, command.SignedTransaction);

            switch (broadcastingResult)
            {
                case TransactionBroadcastingResult.Success:
                    break;
                
                case TransactionBroadcastingResult.AlreadyBroadcasted:
                    _log.WriteInfo
                    (
                        nameof(BroadcastTransactionCommand),
                        command,
                        "API said that transaction is already broadcasted"
                    );
                    break;
                case TransactionBroadcastingResult.AmountIsTooSmall:
                    _log.WriteInfo
                    (
                        nameof(BroadcastTransactionCommand),
                        command,
                        "API said, that amount is too small"
                    );

                    publisher.PublishEvent(new TransactionBroadcastingFailedEvent
                    {
                        OperationId = command.OperationId,
                        TransactionId = command.TransactionId
                    });

                    return CommandHandlingResult.Ok();
                case TransactionBroadcastingResult.NotEnoughBalance:
                    _log.WriteInfo
                    (
                        nameof(BroadcastTransactionCommand),
                        command,
                        "API said, that amount is too small"
                    );

                    return CommandHandlingResult.Fail(_retryDelayProvider.NotEnoughBalanceRetryDelay);
                case TransactionBroadcastingResult.BuildingShouldBeRepeated:
                    _log.WriteInfo
                    (
                        nameof(BroadcastTransactionCommand),
                        command,
                        "API said, that building should be repeated"
                    );

                    publisher.PublishEvent(new TransactionReBuildingIsRequestedOnBroadcastingEvent
                    {
                        OperationId = command.OperationId,
                        TransactionId = command.TransactionId
                    });

                    return CommandHandlingResult.Ok();
                default:
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(broadcastingResult),
                        $"Transaction broadcastring result [{broadcastingResult}] is not supported."
                    );
            }
            
            _chaosKitty.Meow(command.OperationId);
            
            publisher.PublishEvent(new TransactionBroadcastedEvent
            {
                OperationId = command.OperationId,
                TransactionId = command.TransactionId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
