﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Api.Clients;
using Blockchain.Exceptions;
using Blockchain.Models;
using EvotoClient.Views;
using GalaSoft.MvvmLight.Command;
using Models;

namespace EvotoClient.ViewModel
{
    public class ResultsViewModel : EvotoViewModelBase
    {
        private readonly VoteClient _voteClient;
        private List<BlockchainVoteModelPlainText> _answers;
        private List<BlockchainQuestionModel> _questions;
        private BlockchainDetails _currentDetails;

        public ResultsViewModel()
        {
            _voteClient = new VoteClient();

            FindVoteCommand = new RelayCommand(DoFindVote, CanFindVote);
            BackCommand = new RelayCommand(DoBack);

            NextCommand = new RelayCommand(DoNext, CanNext);
            PrevCommand = new RelayCommand(DoPrev, CanPrev);

            ReconnectCommand = new RelayCommand(Connect);

            Loaded += (sender, args) => { TransitionView = ((ResultsView) sender).pageTransition; };
        }

        #region Commands

        public RelayCommand FindVoteCommand { get; }
        public RelayCommand BackCommand { get; }

        public RelayCommand NextCommand { get; }
        public RelayCommand PrevCommand { get; }

        public RelayCommand ReconnectCommand { get; }

        #endregion

        #region Properties

        private MultiChainViewModel _multiChainVm;
        public MultiChainViewModel MultiChainVm => _multiChainVm ?? (_multiChainVm = GetVm<MultiChainViewModel>());

        public ObservableRangeCollection<ChartViewModel> Results =
            new ObservableRangeCollection<ChartViewModel>();

        public PageTransition TransitionView { private get; set; }

        public bool _loading;

        public bool Loading
        {
            get { return _loading; }
            set
            {
                Set(ref _loading, value);
                RaisePropertyChanged(nameof(ResultsVisible));
                FindVoteCommand.RaiseCanExecuteChanged();
            }
        }

        public bool ResultsVisible => !Loading;

        private int _currentResults;

        public int CurrentResults
        {
            get { return _currentResults; }
            set
            {
                Set(ref _currentResults, value);
                NextCommand.RaiseCanExecuteChanged();
                PrevCommand.RaiseCanExecuteChanged();
            }
        }

        private int _totalResults;

        public int TotalResults
        {
            get { return _totalResults; }
            set { Set(ref _totalResults, value); }
        }

        private string _loadingText;

        public string LoadingText
        {
            get { return _loadingText; }
            set { Set(ref _loadingText, value); }
        }

        private bool _cannotConnect;

        public bool CannotConnect
        {
            get { return _cannotConnect; }
            set { Set(ref _cannotConnect, value); }
        }

        #endregion

        #region Methods

        public void SelectVote(BlockchainDetails blockchain)
        {
            _currentDetails = blockchain;

            Connect();
        }

        private void Connect()
        {
            if (Loading)
                return;

            Ui(() =>
            {
                Loading = true;
                CannotConnect = false;
                TotalResults = 0;
                CurrentResults = 0;
            });

            // Default progress message. Will be shown for a short while until the blockchain size has been calculated.
            ResetProgress();

            Task.Run(async () =>
            {
                try
                {
                    await ConnectToBlockchain(_currentDetails);
                    await GetResults(_currentDetails);
                }
                catch (CouldNotConnectToBlockchainException)
                {
                    Ui(() =>
                    {
                        Loading = false;
                        CannotConnect = true;
                    });
                }
            });
        }

        private async Task ConnectToBlockchain(BlockchainDetails blockchain)
        {
            if (MultiChainVm.Connected)
                if (MultiChainVm.Model.Name != blockchain.ChainString)
                    await MultiChainVm.Disconnect();
                else
                    return;

            await MultiChainVm.Connect(blockchain.Host, blockchain.Port, blockchain.ChainString);
        }

        private void ResetProgress()
        {
            Ui(() => { LoadingText = "Synchronising Blockchain"; });
        }

        private void UpdateProgress(BlockchainSyncProgress model)
        {
            Ui(() =>
            {
                LoadingText =
                    $"Synchronising Blockchain {Math.Round(model.Percentage)}% Complete\n({model.CurrentBlocks}/{model.TotalBlocks} blocks)";
            });
        }

        private async Task GetResults(BlockchainDetails blockchain)
        {
            // Ensure the blockchain is up to date
            var progressModel = new Progress<BlockchainSyncProgress>(UpdateProgress);
            await MultiChainVm.Model.WaitUntilBlockchainSynced(blockchain.Blocks, progressModel);

            try
            {
                // Read the questions from the blockchain
                _questions = await MultiChainVm.Model.GetQuestions();

                var decryptKey = "";

                // Blockchain encrypted, so we need the decryption key to read the results
                if (blockchain.ShouldEncryptResults)
                    decryptKey = await _voteClient.GetDecryptKey(blockchain.ChainString);

                // Get answers from blockchain
                _answers =
                    await MultiChainVm.Model.GetResults(blockchain.WalletId, decryptKey);

                // For each question, get its total for each answer
                var results = _questions.Select(question =>
                {
                    // Setup response dictionary, answer -> num votes
                    var options = question.Answers.ToDictionary(a => a.Answer, a => 0);
                    foreach (var answer in _answers)
                        foreach (var questionAnswer in answer.Answers.Where(a => a.Question == question.Number))
                        {
                            // In case we have anything unusual going on
                            if (!options.ContainsKey(questionAnswer.Answer))
                            {
                                Debug.WriteLine(
                                    $"Unexpected answer for question {questionAnswer.Question}: {questionAnswer.Answer}");
                                continue;
                            }
                            options[questionAnswer.Answer]++;
                        }

                    return new
                    {
                        question.Number,
                        question.Question,
                        Results = options
                    };
                }).ToList();

                var chartVms = results.Select(q => new ChartViewModel(q.Results)
                {
                    Question = q.Question
                }).ToList();

                Ui(() =>
                {
                    Loading = false;
                    Results.Clear();
                    Results.AddRange(chartVms);

                    TotalResults = chartVms.Count;
                    CurrentResults = 1;
                    TransitionView.ShowPage(Results.First());
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void DoBack()
        {
            MainVm.ChangeView(EvotoView.Home);
        }

        private void DoNext()
        {
            CurrentResults++;
            ShowPage();
        }

        private bool CanNext()
        {
            return CurrentResults < TotalResults;
        }

        private void DoPrev()
        {
            CurrentResults--;
            ShowPage();
        }

        private bool CanPrev()
        {
            return CurrentResults > 1;
        }

        private void DoFindVote()
        {
            var findVoteVm = GetVm<FindVoteViewModel>();
            findVoteVm.SetResults(_questions, _answers);
            MainVm.ChangeView(EvotoView.FindVote);
        }

        private bool CanFindVote()
        {
            return !Loading;
        }

        private void ShowPage(bool forwards = true)
        {
            var page = Results[CurrentResults - 1];
            TransitionView.ShowPage(page, forwards);
        }

        public void ReInit()
        {
            Connect();
        }

        #endregion
    }
}