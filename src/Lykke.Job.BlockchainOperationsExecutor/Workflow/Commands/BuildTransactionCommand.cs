﻿using System;
using MessagePack;

namespace Lykke.Job.BlockchainOperationsExecutor.Workflow.Commands
{
    [MessagePackObject]
    public class BuildTransactionCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }

        [Key(1)]
        public Guid TransactionId { get; set; }

        [Key(2)]
        public string AssetId { get; set; }

        [Key(3)]
        public string FromAddress { get; set; }

        [Key(4)]
        public string ToAddress { get; set; }

        [Key(5)]
        public decimal Amount { get; set; }

        [Key(6)]
        public bool IncludeFee { get; set; }
    }
}
