//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Utilities
{
    /// <summary>
    /// Implementation of <see cref="INotifyPropertyChanged"/> to simplify models.
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public abstract class BindableObject : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        /// <summary>
        /// Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Checks if a property already matches a desired value.  Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners.  This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected bool SetProperty<T>(ref T storage, T value, string error = null, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            OnPropertyChanged(propertyName);

            if ((error != null) && !_errors.ContainsKey(propertyName))
            {
                _errors[propertyName] = true;
                OnErrorsChanged(propertyName);
            }
            else if ((error == null) && _errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }

            return true;
        }

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">Name of the property used to notify listeners.  This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // raven extensions: INotifyDataErrorInfo
        //
        // ------------------------------------------------------------------------------------------------------------

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        [JsonIgnore]
        private readonly Dictionary<string, bool> _errors = new();

        [JsonIgnore]
        public bool HasErrors { get => (_errors.Count > 0); }

        public System.Collections.IEnumerable GetErrors(string propertyName = null)
        {
            if (propertyName == null)
            {
                // TODO: probably make this a List<string>?
                return _errors.Keys;
            }
            return (_errors.ContainsKey(propertyName)) ? new List<string>() { propertyName } : new List<string>();
        }

        public void ClearErrors(string propertyName = null)
        {
            if (propertyName == null)
            {
                _errors.Clear();
            }
            else
            {
                _errors.Remove(propertyName);
            }
            OnErrorsChanged(null);
        }

        protected void OnErrorsChanged([CallerMemberName] string propertyName = null)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // raven extensions: Utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Bind a control to a data object that is posting changes.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="property"></param>
        /// <param name="source"></param>
        /// <param name="path"></param>
        public static void BindControlToData(Control control, DependencyProperty property, object source, string path)
        {
            Binding bdef = new()
            {
                Mode = BindingMode.TwoWay,
                Source = source,
                Path = new PropertyPath(path),
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
            };
            control.SetBinding(property, bdef);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // raven extensions: validation
        //
        // ------------------------------------------------------------------------------------------------------------

        public static bool IsIntegerFieldValid(string value, int min, int max, bool isNoEValid = true)
        {
            return (( string.IsNullOrEmpty(value) && isNoEValid) ||
                    (!string.IsNullOrEmpty(value) && int.TryParse(value, out int ival)
                                                  && (ival >= min) && (ival <= max)));
        }

        public static bool IsDecimalFieldValid(string value, double min, double max, bool isNoEValid = true)
        {
            return (( string.IsNullOrEmpty(value) && isNoEValid) ||
                    (!string.IsNullOrEmpty(value) && double.TryParse(value, out double dval)
                                                  && (dval >= min) && (dval <= max)));
        }

        public static bool IsBooleanFieldValid(string value, bool isNoEValid = true)
        {
            return (( string.IsNullOrEmpty(value) && isNoEValid) ||
                    (!string.IsNullOrEmpty(value) && ((value == bool.TrueString) || (value == bool.FalseString))));
        }

        public static bool IsRegexFieldValid(string value, Regex regex, bool isNoEValid = true)
        {
            return (( string.IsNullOrEmpty(value) && isNoEValid) ||
                    (!string.IsNullOrEmpty(value) && regex.IsMatch(value)));
        }

        public static string FixupIntegerField(string value, string format = null)
        {
            return (int.TryParse(value, out int parsedValue)) ? parsedValue.ToString(format) : "";
        }

        public static string FixupDecimalField(string value, string format = null)
        {
            return (double.TryParse(value, out double parsedValue)) ? parsedValue.ToString(format) : "";
        }
    }
}