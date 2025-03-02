﻿#pragma warning disable SA1402 // File may only contain a single class
namespace FluentTest;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ControlzEx;
using ControlzEx.Theming;
using Fluent;
using Fluent.Extensions;
using Fluent.Internal;
using Fluent.Localization;
using FluentTest.Adorners;
using FluentTest.Commanding;
using FluentTest.Helpers;
using FluentTest.ViewModels;
#if MahApps_Metro
    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;
#endif
using Button = Fluent.Button;

/// <summary>
/// Test-Content
/// </summary>
public partial class TestContent
{
    private readonly MainViewModel viewModel;
    private string windowTitle;

    public TestContent()
    {
        this.InitializeComponent();

        //RibbonLocalization.Current.Localization.Culture = new CultureInfo("ru-RU");

        this.HookEvents();

        this.viewModel = new MainViewModel();
        this.DataContext = this.viewModel;

        this.Loaded += this.TestContent_Loaded;

        this.InputBindings.Add(new InputBinding(new RelayCommand(() =>
        {
            this.Backstage.IsOpen = !this.Backstage.IsOpen;
            if (this.Backstage.IsOpen
                && this.Backstage.Content is BackstageTabControl backstageTabControl)
            {
                var recentTabItem = backstageTabControl.Items.OfType<BackstageTabItem>().FirstOrDefault(x => x.Header is "Recent");
                if (recentTabItem is not null)
                {
                    recentTabItem.IsSelected = true;
                }
            }
        }), new KeyGesture(Key.F11, ModifierKeys.Control)));
    }

    public string WindowTitle => this.windowTitle ?? (this.windowTitle = GetVersionText(Window.GetWindow(this).GetType().BaseType));

#pragma warning disable WPF0060
    /// <summary>Identifies the <see cref="Colors"/> dependency property.</summary>
    public static readonly DependencyProperty ColorsProperty = DependencyProperty.Register(nameof(Colors), typeof(List<KeyValuePair<string, Color?>>), typeof(TestContent), new PropertyMetadata(default(List<KeyValuePair<string, Color>>)));
#pragma warning restore WPF0060

    public List<KeyValuePair<string, Color?>> Colors
    {
        get => (List<KeyValuePair<string, Color?>>)this.GetValue(ColorsProperty);
        set => this.SetValue(ColorsProperty, value);
    }

    public List<RibbonLocalizationBase> Localizations { get; } = GetLocalizations();

    private static List<RibbonLocalizationBase> GetLocalizations()
    {
        return RibbonLocalization.Current.LocalizationMap.Values
            .Select(x => (RibbonLocalizationBase)Activator.CreateInstance(x))
            .ToList();
    }

    private static IEnumerable<KeyValuePair<string, Color?>> GetColors()
    {
        var colors = typeof(Colors)
            .GetProperties()
            .Where(prop =>
                typeof(Brush).IsAssignableFrom(prop.PropertyType))
            .Select(prop =>
                new KeyValuePair<string, Color?>(prop.Name, (Color)prop.GetValue(null, null)));
        return ThemeManager.Current.Themes.GroupBy(x => x.ColorScheme)
            .Select(x => x.First())
            .Select(x => new KeyValuePair<string, Color?>(x.ColorScheme, ((SolidColorBrush)x.ShowcaseBrush).Color))
            .Concat(colors)
            .OrderBy(x => x.Key);
    }

    private static string GetVersionText(Type type)
    {
        var version = type.Assembly.GetName().Version;

        var assemblyProductAttribute = type.Assembly.GetCustomAttribute<AssemblyProductAttribute>();

        var assemblyInformationalVersionAttribute = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        return $"{assemblyProductAttribute?.Product} {version} ({assemblyInformationalVersionAttribute?.InformationalVersion})";
    }

    private string selectedMenu = "Backstage";

    public string SelectedMenu
    {
        get => this.selectedMenu;
        set
        {
            this.selectedMenu = value;

            switch (this.selectedMenu)
            {
                case "ApplicationMenu":
                    this.ApplicationMenu.Visibility = Visibility.Visible;
                    this.Backstage.Visibility = Visibility.Collapsed;
                    break;

                case "Backstage":
                    this.ApplicationMenu.Visibility = Visibility.Collapsed;
                    this.Backstage.Visibility = Visibility.Visible;
                    break;

                case "Empty menu":
                    this.ApplicationMenu.Visibility = Visibility.Collapsed;
                    this.Backstage.Visibility = Visibility.Collapsed;
                    break;
            }
        }
    }

    private void HookEvents()
    {
        this.Loaded += this.HandleTestContentLoaded;

        this.buttonBold.Checked += (s, e) => Debug.WriteLine("Checked");
        this.buttonBold.Unchecked += (s, e) => Debug.WriteLine("Unchecked");

        this.PreviewMouseWheel += this.OnPreviewMouseWheel;
    }

    private void TestContent_Loaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= this.TestContent_Loaded;

        this.InitializeColors();
    }

    private void InitializeColors()
    {
        ColorGallery.RecentColors.Add((Color)this.FindResource("Fluent.Ribbon.Colors.AccentBase"));

        var currentColors = new[]
        {
            new KeyValuePair<string, Color?>("None", null),
            new KeyValuePair<string, Color?>("Initial glow", GetCurrentGlowColor()),
            new KeyValuePair<string, Color?>("Initial non active glow", GetCurrentNonActiveGlowBrush()),
        };

        this.Colors = currentColors.Concat(GetColors()).ToList();

        Color? GetCurrentGlowColor()
        {
            switch (Window.GetWindow(this))
            {
                case WindowChromeWindow x:
                    return x.GlowColor;
            }

            return null;
        }

        Color? GetCurrentNonActiveGlowBrush()
        {
            switch (Window.GetWindow(this))
            {
                case WindowChromeWindow x:
                    return x.NonActiveGlowColor;
            }

            return null;
        }
    }

    private static void OnScreenTipHelpPressed(object sender, ScreenTipHelpEventArgs e)
    {
        Process.Start((string)e.HelpTopic);
    }

    private void HandleTestContentLoaded(object sender, RoutedEventArgs e)
    {
        ScreenTip.HelpPressed += OnScreenTipHelpPressed;
    }

    private void OnLauncherButtonClick(object sender, RoutedEventArgs e)
    {
        var groupBox = (RibbonGroupBox)sender;

        var wnd = new Window
        {
            Content = $"Launcher-Window for: {groupBox.Header}",
            Width = 300,
            Height = 100,
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        wnd.ShowDialog();
    }

    private void OnSplitClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Split Clicked!!!");
    }

    private void OnEnlargeClick(object sender, RoutedEventArgs e)
    {
        if (this.InRibbonGallery.IsLoaded)
        {
            this.InRibbonGallery.Enlarge();
        }
    }

    private void OnReduceClick(object sender, RoutedEventArgs e)
    {
        if (this.InRibbonGallery.IsLoaded)
        {
            this.InRibbonGallery.Reduce();
        }
    }

    private void SyncThemeNow_OnClick(object sender, RoutedEventArgs e)
    {
        ThemeManager.Current.SyncTheme();
    }

    public Button CreateRibbonButton()
    {
        var fooCommand1 = new TestRoutedCommand();

        var button = new Button
        {
            Command = fooCommand1.ItemCommand,
            Header = "Foo",
            Icon = new BitmapImage(new Uri("pack://application:,,,/Fluent.Ribbon.Showcase;component/Images/Green.png", UriKind.Absolute)),
            LargeIcon = new BitmapImage(new Uri("pack://application:,,,/Fluent.Ribbon.Showcase;component/Images/GreenLarge.png", UriKind.Absolute)),
        };

        this.CommandBindings.Add(fooCommand1.ItemCommandBinding);
        return button;
    }

    #region Logical tree

    private void OnShowLogicalTreeClick(object sender, RoutedEventArgs e)
    {
        this.CheckLogicalTree(this.ribbon);
        this.logicalTreeView.Items.Clear();
        this.BuildLogicalTree(this.ribbon, this.logicalTreeView);
    }

    private static string GetDebugInfo(DependencyObject element)
    {
        if (element is null)
        {
            return "NULL";
        }

        var debugInfo = $"[{element}]";

        if (element is IHeaderedControl headeredControl)
        {
            debugInfo += $" Header: \"{headeredControl.Header}\"";
        }

        if (element is FrameworkElement frameworkElement
            && string.IsNullOrEmpty(frameworkElement.Name) == false)
        {
            debugInfo += $" Name: \"{frameworkElement.Name}\"";
        }

        return debugInfo;
    }

    private void CheckLogicalTree(DependencyObject root)
    {
        var children = LogicalTreeHelper.GetChildren(root);
        foreach (var child in children.OfType<DependencyObject>())
        {
            if (ReferenceEquals(LogicalTreeHelper.GetParent(child), root) == false)
            {
                Debug.WriteLine($"Incorrect logical parent for {GetDebugInfo(child)}");
                Debug.WriteLine($"\tExpected: {GetDebugInfo(root)}");
                Debug.WriteLine($"\tFound: {GetDebugInfo(LogicalTreeHelper.GetParent(child))}");
            }

            this.CheckLogicalTree(child);
        }
    }

    private void BuildLogicalTree(DependencyObject current, ItemsControl parentControl)
    {
        var newItem = new TreeViewItem
        {
            Header = GetDebugInfo(current),
            Tag = current
        };

        parentControl.Items.Add(newItem);

        var children = LogicalTreeHelper.GetChildren(current);
        foreach (var child in children.OfType<DependencyObject>())
        {
            this.BuildLogicalTree(child, newItem);
        }
    }

    private void OnTreeDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var treeView = sender as TreeView;

        var item = treeView?.SelectedItem as TreeViewItem;
        if (item is null)
        {
            return;
        }

        var stringBuilder = new StringBuilder();
        this.BuildBackLogicalTree(item.Tag as DependencyObject, stringBuilder);

        MessageBox.Show($"From buttom to top:\n{stringBuilder}");
    }

    private void BuildBackLogicalTree(DependencyObject current, StringBuilder stringBuilder)
    {
        if (current is null
            || ReferenceEquals(current, this.ribbon))
        {
            return;
        }

        stringBuilder.AppendFormat(" -> {0}\n", GetDebugInfo(current));

        var parent = LogicalTreeHelper.GetParent(current);

        this.BuildBackLogicalTree(parent, stringBuilder);
    }

    #endregion Logical tree

    private void OnFormatPainterClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("FP");
    }

    private void OnHelpClick(object sender, RoutedEventArgs e)
    {
        this.viewModel.AreContextGroupsVisible = !this.viewModel.AreContextGroupsVisible;
    }

    private void OnSpinnerValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // MessageBox.Show(String.Format("Changed from {0} to {1}", e.OldValue, e.NewValue));
    }

    private void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        var wnd = new TestWindow
        {
            Owner = Window.GetWindow(this)
        };
        wnd.Show();
    }

    private void AddRibbonTab_OnClick(object sender, RoutedEventArgs e)
    {
        var tab = new RibbonTabItem
        {
            Header = "Test"
        };

        var group = new RibbonGroupBox();
        for (var i = 0; i < 20; i++)
        {
            group.Items.Add(this.CreateRibbonButton());
        }

        tab.Groups.Add(group);

        this.ribbon.Tabs.Add(tab);
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var group = this.ribbon.SelectedTabItem.Groups.Last();

        if (group.ItemsSource is not null)
        {
            return;
        }

        var button = new Button
        {
            Header = "Foo",
            Icon = new BitmapImage(new Uri("pack://application:,,,/Fluent.Ribbon.Showcase;component/Images/Green.png", UriKind.Absolute)),
            LargeIcon = new BitmapImage(new Uri("pack://application:,,,/Fluent.Ribbon.Showcase;component/Images/GreenLarge.png", UriKind.Absolute)),
            SizeDefinition = new RibbonControlSizeDefinition(RibbonControlSize.Middle, RibbonControlSize.Middle, RibbonControlSize.Middle)
        };
        group.Items.Add(button);
    }

    private async void HandleSaveAsClick(object sender, RoutedEventArgs e)
    {
        var progressAdornerChild = new Border
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(System.Windows.Media.Colors.Black) { Opacity = 0.25 },
            IsHitTestVisible = true,
            Child = new ProgressBar
            {
                IsIndeterminate = true,
                Width = 300,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        BindingOperations.SetBinding(progressAdornerChild, WidthProperty, new Binding(nameof(this.Backstage.AdornerLayer.ActualWidth)) { Source = this.Backstage.AdornerLayer });
        BindingOperations.SetBinding(progressAdornerChild, HeightProperty, new Binding(nameof(this.Backstage.AdornerLayer.ActualHeight)) { Source = this.Backstage.AdornerLayer });

        var progressAdorner = new SimpleControlAdorner(this.Backstage.AdornerLayer)
        {
            Child = progressAdornerChild
        };

        this.Backstage.AdornerLayer.Add(progressAdorner);

        await Task.Delay(TimeSpan.FromSeconds(3));

        this.Backstage.AdornerLayer.Remove(progressAdorner);

        BindingOperations.ClearAllBindings(progressAdornerChild);
    }

    private void OpenRegularWindow_OnClick(object sender, RoutedEventArgs e)
    {
        new RegularWindow().Show();
    }

    private void OpenMinimalRibbonWindowSample_OnClick(object sender, RoutedEventArgs e)
    {
        new MinimalWindowSample().Show();
    }

    private void OpenMahMetroWindow_OnClick(object sender, RoutedEventArgs e)
    {
#if MahApps_Metro
            new MahMetroWindow().Show();
#else
        ShowMahAppsMetroNotAvailableMessageBox();
#endif
    }

    private void OpenRibbonWindowWithoutVisibileRibbon_OnClick(object sender, RoutedEventArgs e)
    {
        new RibbonWindowWithoutVisibleRibbon().Show();
    }

    private void OpenRibbonWindowWithoutRibbon_OnClick(object sender, RoutedEventArgs e)
    {
        new RibbonWindowWithoutRibbon().Show();
    }

    private void ZoomSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (e.OldValue.AlmostEquals(0)
            || Window.GetWindow(this) is not { } window)
        {
            return;
        }

        var textFormattingMode = e.NewValue >= 1.0 || DoubleUtil.AreClose(e.NewValue, 1.0) ? TextFormattingMode.Ideal : TextFormattingMode.Display;
        TextOptions.SetTextFormattingMode(window, textFormattingMode);
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) == false
            && Keyboard.IsKeyDown(Key.RightCtrl) == false)
        {
            return;
        }

        var newZoomValue = this.zoomSlider.Value + (e.Delta > 0 ? 0.1 : -0.1);

        this.zoomSlider.Value = Math.Max(Math.Min(newZoomValue, this.zoomSlider.Maximum), this.zoomSlider.Minimum);

        e.Handled = true;
    }

    private void SleepButton_OnClick(object sender, RoutedEventArgs e)
    {
        Thread.Sleep(TimeSpan.FromSeconds(10));
    }

    private void OpenModalRibbonWindow_OnClick(object sender, RoutedEventArgs e)
    {
        var childWindow = new TestWindow { Owner = Window.GetWindow(this) };
        childWindow.ShowDialog();
    }

    private void OpenRibbonWindowOnNewThread_OnClick(object sender, RoutedEventArgs e)
    {
        var thread = new Thread(() =>
        {
            var testWindow = new TestWindow();
            testWindow.Closed += OnTestWindowOnClosed;
            testWindow.Show();
            System.Windows.Threading.Dispatcher.Run();

            void OnTestWindowOnClosed(object o, EventArgs args)
            {
                testWindow.Closed -= OnTestWindowOnClosed;
                ((Window)o).Dispatcher?.InvokeShutdown();
            }
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
    }

    private void OpenRibbonWindowColorized_OnClick(object sender, RoutedEventArgs e)
    {
        new RibbonWindowColorized().Show();
    }

    private void OpenRibbonWindowWithBackgroundImage_OnClick(object sender, RoutedEventArgs e)
    {
        new RibbonWindowWithBackgroundImage().Show();
    }

    private void ShowStartScreen_OnClick(object sender, RoutedEventArgs e)
    {
        this.startScreen.Shown = false;
        this.startScreen.IsOpen = true;
    }

    private void HandleAddItemToFontsClick(object sender, RoutedEventArgs e)
    {
        this.viewModel.FontsViewModel.FontsData.Add($"Added item {this.viewModel.FontsViewModel.FontsData.Count}");
    }

    private void CreateThemeResourceDictionaryButton_OnClick(object sender, RoutedEventArgs e)
    {
        this.ThemeResourceDictionaryTextBox.Text = ThemeHelper.CreateTheme(ThemeManager.Current.DetectTheme()?.BaseColorScheme ?? "Dark", this.ThemeColorGallery.SelectedColor ?? this.viewModel.ColorViewModel.ThemeColor, changeImmediately: this.ChangeImmediatelyCheckBox.IsChecked ?? false).Item1;
    }

    private void HandleResetSavedState_OnClick(object sender, RoutedEventArgs e)
    {
        this.ribbon.AutomaticStateManagement = false;
        this.ribbon.RibbonStateStorage.Reset();

        System.Windows.Forms.Application.Restart();
        Application.Current.Shutdown();
    }

    private async void HandleShowMetroMessage(object sender, RoutedEventArgs e)
    {
#if MahApps_Metro
            var metroWindow = Window.GetWindow(this) as MetroWindow;

            if (metroWindow is null)
            {
                return;
            }

            await metroWindow.ShowMessageAsync("Test", "Message");
#else
        ShowMahAppsMetroNotAvailableMessageBox();
        await Task.Yield();
#endif
    }

    private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        new Window().ShowDialog();
    }

    private static void ShowMahAppsMetroNotAvailableMessageBox()
    {
        MessageBox.Show("MahApps.Metro is not available in this showcase version.");
    }

    private void StartSnoop_OnClick(object sender, RoutedEventArgs e)
    {
        var snoopPath = "snoop";
        var alternativeSnoopPath = Environment.GetEnvironmentVariable("snoop_dev_path");

        if (string.IsNullOrEmpty(alternativeSnoopPath) == false
            && File.Exists(alternativeSnoopPath))
        {
            snoopPath = alternativeSnoopPath;
        }

        var startInfo = new ProcessStartInfo(snoopPath, $"inspect --targetPID {Process.GetCurrentProcess().Id}")
        {
            UseShellExecute = true
        };
        try
        {
            using var p = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}\n\nCommandline: {startInfo.FileName} {startInfo.Arguments}");
        }
    }

    private void OpenSimplifiedRibbonWindow_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new SimplifiedRibbonWindow();
        window.DataContext = this.viewModel;
        window.Show();
    }
}

public class TestRoutedCommand
{
    public static RoutedCommand TestPresenterCommand { get; } = new(nameof(TestPresenterCommand), typeof(TestRoutedCommand));

    public ICommand ItemCommand => TestPresenterCommand;

    public CommandBinding ItemCommandBinding => new(TestPresenterCommand, OnTestCommandExecuted, CanExecuteTestCommand);

    private static void CanExecuteTestCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private static void OnTestCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        MessageBox.Show("TestPresenterCommand");
    }
}