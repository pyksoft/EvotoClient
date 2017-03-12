﻿using System;
using System.Diagnostics;
using Blockchain;
using GalaSoft.MvvmLight;
using Microsoft.Practices.ServiceLocation;
using Models;

namespace EvotoClient.ViewModel
{
    public enum EvotoView
    {
        Login,
        Home,
        Register,
        Vote
    }

    public class MainViewModel : EvotoViewModelBase
    {
        private readonly LoginViewModel _loginVm = ServiceLocator.Current.GetInstance<LoginViewModel>();

        public MainViewModel()
        {
            MultiChainTools.SubDirectory = "client";
            CurrentView = _loginVm;
        }

        #region Events

        public event EventHandler<UserDetails> OnLogin;

        #endregion

        #region Properties

        private EvotoViewModelBase _currentView;

        public EvotoViewModelBase CurrentView
        {
            get { return _currentView; }
            set { Set(ref _currentView, value); }
        }

        private bool _loggedIn;

        public bool LoggedIn
        {
            get {  return _loggedIn;}
            set { Set(ref _loggedIn, value); }
        }

        #endregion

        #region Methods

        public void InvokeLogin(object caller, UserDetails details)
        {
            OnLogin?.Invoke(caller, details);
        }

        public void ChangeView(EvotoView view)
        {
            Debug.WriteLine($"Changing view to: {view}");
            Ui(() =>
            {
                switch (view)
                {
                    case EvotoView.Login:
                        CurrentView = ServiceLocator.Current.GetInstance<LoginViewModel>();
                        break;
                    case EvotoView.Home:
                        CurrentView = ServiceLocator.Current.GetInstance<HomeViewModel>();
                        break;
                    case EvotoView.Register:
                        CurrentView = ServiceLocator.Current.GetInstance<RegisterViewModel>();
                        break;
                    case EvotoView.Vote:
                        CurrentView = ServiceLocator.Current.GetInstance<VoteViewModel>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(view), view, null);
                }
            });
        }

        #endregion
    }
}