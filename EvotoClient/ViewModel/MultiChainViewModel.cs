﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Api.Clients;
using Api.Exceptions;
using Blockchain;
using Blockchain.Models;
using GalaSoft.MvvmLight.Ioc;
using Models;
using Models.Exception;
using MultiChainLib.Model;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;

namespace EvotoClient.ViewModel
{
    public class MultiChainViewModel : EvotoViewModelBase
    {
        private readonly MultiChainHandler _multiChainHandler;

        private MultichainModel _multichain;

        public MultiChainViewModel()
        {
            _multiChainHandler = SimpleIoc.Default.GetInstance<MultiChainHandler>();
        }

        #region Properties

        public MultichainModel Model
        {
            get
            {
                if (!Connected)
                    throw new Exception("Not connected to blockchain");
                return _multichain;
            }
        }

        public bool Connected => (_multichain != null) && _multichain.Connected;

        #endregion

        #region Methods

        public async Task Connect(string hostname, int port, string blockchainName)
        {
            // Only connect to one multichain at a time
            if (Connected)
                throw new Exception("Must disconnect from blockchain before connecting to a new one");

            var localPort = MultiChainTools.GetNewPort(EPortType.ClientMultichainD);
            var rpcPort = MultiChainTools.GetNewPort(EPortType.ClientRpc);
            _multichain = await _multiChainHandler.Connect(hostname, blockchainName, port, localPort, rpcPort, false);
        }

        public async Task Disconnect()
        {
            if (!Connected)
                return;

            await _multiChainHandler.DisconnectAndClose(_multichain);
        }

        public override void Cleanup()
        {
            Debug.WriteLine("Cleaning Up");
            if (_multichain != null)
                Task.Run(async () => { await _multiChainHandler.DisconnectAndClose(_multichain); }).Wait();

            base.Cleanup();
        }

        public async Task<string> Vote(List<QuestionViewModel> questions, BlockchainDetails blockchain, IProgress<int> progress)
        {
            var voteClient = new VoteClient();

            progress.Report(1);

            try
            {
                // Create our random voting token
                var token = GenerateRandomToken();

                // Get the registrar's public key, to use for the blind signature
                var keyStr = await voteClient.GetPublicKey(Model.Name);
                var key = RsaTools.PublicKeyFromString(keyStr);

                // Blind our token
                progress.Report(2);
                var blindedToken = RsaTools.BlindMessage(token, key);

                // Get the token signed by the registrar
                var blindSignature = await voteClient.GetBlindSignature(Model.Name, blindedToken.Blinded.ToString());

                // Unblind the token
                var unblindedToken = RsaTools.UnblindMessage(new BigInteger(blindSignature), blindedToken.Random,
                    key);

                // Here we would normally sleep until a random number of other voters have had tokens blindly signed by the registrar.
                // This would be possible through observation of the root stream using the voters key, or a maximum timeout.
                // However, given that this is a prototype, there is not enough traffic to make this feasible.
                progress.Report(3);

                // Create a wallet address to vote from
                var walletId = await Model.GetNewWalletAddress();

                // Request a voting asset (currency)
                progress.Report(4);
                var regiMeta = await voteClient.GetVote(Model.Name, walletId, token, unblindedToken.ToString());

                // Wait until the currency has been confirmed
                progress.Report(5);
                await Model.ConfirmVoteAllocated();

                // Where the transaction's currency comes from
                progress.Report(6);
                var txIds = new List<CreateRawTransactionTxIn>
                {
                    new CreateRawTransactionTxIn {TxId = regiMeta.TxId, Vout = 0}
                };

                // Where the transaction's currency goes to (registrar)
                var toInfo = new List<CreateRawTransactionAmount>
                {
                    new CreateRawTransactionAsset
                    {
                        Address = regiMeta.RegistrarAddress,
                        Qty = 1,
                        Name = MultiChainTools.VOTE_ASSET_NAME
                    }
                };

                // Create our list of answers
                var answers = questions.Select(q => new BlockchainVoteAnswerModel
                {
                    Answer = q.SelectedAnswer.Answer,
                    Question = q.QuestionNumber
                }).ToList();


                // Send our vote, encrytped if required
                if (blockchain.ShouldEncryptResults)
                {
                    var encryptKey = RsaTools.PublicKeyFromString(blockchain.EncryptKey);
                    var encryptedAnswers = new BlockchainVoteModelEncrypted
                    {
                        MagicWords = regiMeta.Words,
                        Answers = RsaTools.EncryptMessage(JsonConvert.SerializeObject(answers), encryptKey)
                    };

                    await Model.WriteTransaction(txIds, toInfo, encryptedAnswers);
                }
                else
                {
                    // Send vote in plaintext (live readable results)
                    var answerModel = new BlockchainVoteModelPlainText
                    {
                        MagicWords = regiMeta.Words,
                        Answers = answers
                    };

                    await Model.WriteTransaction(txIds, toInfo, answerModel);
                }

                return regiMeta.Words;
            }
            catch (ApiException)
            {
                progress.Report(-1);
                throw new CouldNotVoteException();
            }
            catch (Exception e)
            {
                progress.Report(-1);
                Debug.WriteLine(e.Message);
                throw new CouldNotVoteException();
            }
        }

        private static string GenerateRandomToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        #endregion
    }
}