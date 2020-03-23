﻿using System;
using System.Windows.Input;

namespace nUpdate.Administration.Infrastructure
{
    /// <summary>
    /// A relay command that does not take parameters.
    /// </summary>
    public class RelayCommand : ICommand
    {
        readonly Action<object> _methodToExecute;
        readonly Func<bool> _canExecuteEvaluator;

        /// <summary>
        /// Is raised when the ability of this command to be executed changes.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Creates a relay command.
        /// </summary>
        /// <param name="methodToExecute">A delegate that is invoked when the command is executed.</param>
        /// <param name="canExecuteEvaluator">A delegate that specifies if the command can be executed.</param>
        public RelayCommand(Action<object> methodToExecute, Func<bool> canExecuteEvaluator)
        {
            _methodToExecute = methodToExecute;
            _canExecuteEvaluator = canExecuteEvaluator;
        }

        /// <summary>
        /// Creates a relay command.
        /// </summary>
        /// <param name="methodToExecute">A delegate that is invoked when the command is executed.</param>
        public RelayCommand(Action<object> methodToExecute)
            : this(methodToExecute, () => true)
        {
        }

        /// <summary>
        /// Evaluates if this command can be executed.
        /// </summary>
        /// <returns>Returns true if this command can be executed, otherwise false.</returns>
        public bool CanExecute()
        {
            return _canExecuteEvaluator.Invoke();
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        /// <summary>
        /// Executes this command.
        /// </summary>
        public void Execute(object parameter)
        {
            _methodToExecute.Invoke(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            Execute(parameter);
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}