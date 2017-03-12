﻿using System.Collections.Generic;

namespace EvotoClient.ViewModel
{
    public class QuestionViewModel : EvotoViewModelBase
    {
        #region Properties

        private string _question;

        public string Question
        {
            get { return _question; }
            set { Set(ref _question, value); }
        }

        public List<AnswerViewModel> Answers { get; set; }

        private AnswerViewModel _selectedAnswer;

        public AnswerViewModel SelectedAnswer
        {
            get { return _selectedAnswer; }
            set
            {
                Set(ref _selectedAnswer, value);
                RaisePropertyChanged(nameof(HasAnswer));
            }
        }

        public bool HasAnswer => SelectedAnswer != null;

        #endregion
    }
}