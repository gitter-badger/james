﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using James.Workflows;

namespace James
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _showLargeType;

        private static MainWindow _mainWindow;
        private static readonly object SingeltonLock = new object();

        public static MainWindow GetInstance(bool showOnStartup = false)
        {
            lock (SingeltonLock)
            {
                return _mainWindow ?? (_mainWindow = new MainWindow(showOnStartup));
            }
        }

        public MainWindow(bool showOnStartup)
        {
            if (!showOnStartup)
            {
                Visibility = Visibility.Hidden;
            }
            InitializeComponent();
            new HotKey(Key.Space, KeyModifier.Alt, OnHotKeyHandler);
            LargeType.GetInstance().Deactivated += LargeType_Deactivated;
            LargeType.GetInstance().Activated += LargeType_Activated;
        }

        private void LargeType_Activated(object sender, EventArgs e)
        {
            _showLargeType = false;
        }

        private void LargeType_Deactivated(object sender, EventArgs e)
        {
            WorkflowManager.GetInstance().CancelWorkflows();
            if (!Keyboard.IsKeyDown(Key.Escape) && !Keyboard.IsKeyDown(Key.L) && !Keyboard.IsKeyDown(Key.LeftAlt))
            {
                HideWindow();
            }
            else
            {
                Show();
                Activate();
                SearchTextBox.Focus();
            }
        }

        private void OnHotKeyHandler(HotKey hotKey)
        {
            if (IsVisible || hotKey == null)
            {
                HideWindow();
            }
            else
            {
                SearchTextBox.Text = "";
                Show();
                Activate();
                SearchTextBox.Focus();
            }
        }

        private void HideWindow()
        {
            LargeType.GetInstance().Hide();
            SearchTextBox.Text = "";
            Hide();
        }

        #region Window-Events

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    searchResultControl.MoveDown();
                    break;
                case Key.Up:
                    searchResultControl.MoveUp();
                    break;
                case Key.Enter:
                    searchResultControl.Open(e);
                    break;
                case Key.Escape:
                    Hide();
                    SearchTextBox.Text = "";
                    break;
            }
            if (e.KeyboardDevice.IsKeyDown(Key.L) && e.KeyboardDevice.IsKeyDown(Key.LeftAlt) &&
                SearchTextBox.Text.Trim().Length > 0)
            {
                DisplayLargeType(SearchTextBox.Text);
            }
            else if (e.KeyboardDevice.IsKeyDown(Key.S) && e.KeyboardDevice.IsKeyDown(Key.LeftAlt))
            {
                new OptionWindow().Show();
                HideWindow();
            }
        }

        public void DisplayLargeType(string message)
        {
            _showLargeType = true;
            LargeType.GetInstance().DisplayMessage(message);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var str = SearchTextBox.Text.Trim();
            new Task(() => searchResultControl.Search(str)).Start();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!_showLargeType)
            {
                OnHotKeyHandler(null);
            }
        }

        #endregion
    }
}