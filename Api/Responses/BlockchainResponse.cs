﻿using System;
using System.Runtime.Serialization;
using Models;

namespace Api.Responses
{
    [Serializable]
    public class BlockchainResponse
    {
        [DataMember(Name = "name")]
        public string Name { get; private set; }

        [DataMember(Name = "expiryDate")]
        public DateTime ExpiryDate { get; private set; }

        [DataMember(Name = "chainString")]
        public string ChainString { get; private set; }

        [DataMember(Name = "port")]
        public int Port { get; private set; }

        [DataMember(Name = "walletId")]
        public string WalletId { get; private set; }

        [DataMember(Name = "blocks")]
        public int Blocks { get; private set; }

        public BlockchainDetails ToModel()
        {
            return new BlockchainDetails
            {
                Name = Name,
                ExpiryDate = ExpiryDate,
                ChainString = ChainString,
                Port = Port,
                WalletId = WalletId,
                Blocks = Blocks
            };
        }
    }
}