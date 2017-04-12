﻿using System.Windows;
using GalaSoft.MvvmLight.Command;
using Models;

namespace EvotoClient.ViewModel
{
    public class UserBarViewModel : EvotoViewModelBase
    {
        public UserBarViewModel()
        {
            LogoutCommand = new RelayCommand(DoLogout);

            MainVm.OnLogin += (sender, details) => { UpdateDetails(details); };
        }

        #region Commands

        public RelayCommand LogoutCommand { get; }

        #endregion

        #region Methods

        private void UpdateDetails(UserDetails details)
        {
            Ui(() => { Email = $"Logged in as: {details.Email}"; });
        }

        private void DoLogout()
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Properties

        private string _email;

        public string Email
        {
            get { return _email; }
            set { Set(ref _email, value); }
        }

        #endregion
    }
}