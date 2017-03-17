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
        Register,
        ForgotPassword,
        ResetPassword,
        Home,
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

        private ViewModelBase _currentView;

        public ViewModelBase CurrentView
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
                EvotoViewModelBase newView;
                switch (view)
                {
                    case EvotoView.Login:
                        newView = ServiceLocator.Current.GetInstance<LoginViewModel>();
                        break;
                    case EvotoView.Register:
                        newView = ServiceLocator.Current.GetInstance<RegisterViewModel>();
                        break;
                    case EvotoView.ForgotPassword:
                        newView = ServiceLocator.Current.GetInstance<ForgotPasswordViewModel>();
                        break;
                    case EvotoView.ResetPassword:
                        newView = ServiceLocator.Current.GetInstance<ResetPasswordViewModel>();
                        break;
                    case EvotoView.Home:
                        newView = ServiceLocator.Current.GetInstance<HomeViewModel>();
                        break;
                    case EvotoView.Vote:
                        newView = ServiceLocator.Current.GetInstance<VoteViewModel>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(view), view, null);
                }

                // Check we're not already on the right view before switching
                if (CurrentView != newView)
                    CurrentView = newView;
            });
        }

        #endregion
    }
}