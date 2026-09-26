using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Kozynax.UI;
using Kozynax.UI.Helpers;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Mvvm;

namespace ZXMAK2.Host.SdlBackend.Platform.MacOS
{
    /// <summary>
    /// Builds an AppKit main menu from <see cref="MainMenuFactory"/> and routes actions to MVVM commands.
    /// </summary>
    internal sealed class MacNativeMenuBar : IDisposable
    {
        private static readonly IntPtr SelInitWithTitleActionKeyEquivalent =
            MacObjC.Sel("initWithTitle:action:keyEquivalent:");
        private static readonly IntPtr SelSetSubmenu = MacObjC.Sel("setSubmenu:");
        private static readonly IntPtr SelAddItem = MacObjC.Sel("addItem:");
        private static readonly IntPtr SelSetTarget = MacObjC.Sel("setTarget:");
        private static readonly IntPtr SelSetAction = MacObjC.Sel("setAction:");
        private static readonly IntPtr SelSetTag = MacObjC.Sel("setTag:");
        private static readonly IntPtr SelSetState = MacObjC.Sel("setState:");
        private static readonly IntPtr SelSetEnabled = MacObjC.Sel("setEnabled:");
        private static readonly IntPtr SelSetTitle = MacObjC.Sel("setTitle:");
        private static readonly IntPtr SelSetMainMenu = MacObjC.Sel("setMainMenu:");
        private static readonly IntPtr SelSharedApplication = MacObjC.Sel("sharedApplication");
        private static readonly IntPtr SelSetActivationPolicy = MacObjC.Sel("setActivationPolicy:");
        private static readonly IntPtr SelActivateIgnoringOtherApps =
            MacObjC.Sel("activateIgnoringOtherApps:");
        private static readonly IntPtr SelSeparatorItem = MacObjC.Sel("separatorItem");
        private static readonly IntPtr SelTag = MacObjC.Sel("tag");

        private static IntPtr _menuTargetClass;
        private static IntPtr _menuTargetInstance;
        private static readonly IntPtr MenuActionSelector = MacObjC.Sel("kozynaxMenuAction:");
        private static MacNativeMenuBar _current;

        private readonly object _commandParameter;
        private readonly ISynchronizeInvoke _sync;
        private readonly List<MenuEntry> _entries = new List<MenuEntry>();
        private readonly List<ICommand> _hookedCommands = new List<ICommand>();
        private int _nextTag = 1;
        private bool _disposed;

        private MacNativeMenuBar(object commandParameter, ISynchronizeInvoke sync)
        {
            _commandParameter = commandParameter;
            _sync = sync;
        }

        public static MacNativeMenuBar Install(
            MainViewModel viewModel,
            IEnumerable<ICommand> toolCommands,
            object commandParameter,
            ISynchronizeInvoke sync = null)
        {
            if (!OperatingSystem.IsMacOS())
                return null;
            if (viewModel == null)
                return null;

            var bar = new MacNativeMenuBar(commandParameter, sync);
            _current = bar;
            EnsureMenuTargetClass();
            bar.BuildAndAttach(viewModel, toolCommands);
            return bar;
        }

        public void Refresh()
        {
            if (_disposed)
                return;
            foreach (var entry in _entries)
                entry.ApplyVisualState(_commandParameter);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (var command in _hookedCommands)
            {
                command.CanExecuteChanged -= Command_OnCanExecuteChanged;
                command.PropertyChanged -= Command_OnPropertyChanged;
            }
            _hookedCommands.Clear();
            if (ReferenceEquals(_current, this))
                _current = null;
        }

        internal void InvokeByTag(int tag)
        {
            if (_disposed)
                return;
            if (_sync != null && _sync.InvokeRequired)
            {
                _sync.Invoke(new Action(() => InvokeByTagOnUiThread(tag)), null);
                return;
            }

            InvokeByTagOnUiThread(tag);
        }

        private void InvokeByTagOnUiThread(int tag)
        {
            foreach (var entry in _entries)
            {
                if (entry.Tag != tag)
                    continue;
                entry.Execute(_commandParameter);
                Refresh();
                return;
            }
        }

        private void BuildAndAttach(MainViewModel viewModel, IEnumerable<ICommand> toolCommands)
        {
            var root = MainMenuFactory.BuildRoot(viewModel, toolCommands);
            var mainMenu = MacObjC.AllocInit("NSMenu");
            var emptyKey = MacObjC.NsString(string.Empty);

            AddApplicationMenu(mainMenu, viewModel, emptyKey);
            if (root?.Children != null)
            {
                foreach (var top in root.Children)
                {
                    if (top == null)
                        continue;
                    AddTopLevelBranch(mainMenu, top, emptyKey);
                }
            }

            var nsApp = MacObjC.Send(MacObjC.Class("NSApplication"), SelSharedApplication);
            MacObjC.SendVoid(nsApp, SelSetActivationPolicy, MacObjC.ActivationPolicyRegular);
            MacObjC.SendVoid(nsApp, SelActivateIgnoringOtherApps, 1);
            MacObjC.SendVoid(nsApp, SelSetMainMenu, mainMenu);
            Refresh();
        }

        private void AddApplicationMenu(IntPtr mainMenu, MainViewModel viewModel, IntPtr emptyKey)
        {
            var appMenu = MacObjC.AllocInit("NSMenu");
            var appTitle = MacObjC.NsString(MainWindowTitle.ProductName);
            var appItem = CreateMenuItem(appTitle, IntPtr.Zero, emptyKey);
            MacObjC.SendVoid(appItem, SelSetSubmenu, appMenu);
            MacObjC.SendVoid(mainMenu, SelAddItem, appItem);

            AddCommandItem(appMenu, "About " + MainWindowTitle.ProductName, viewModel.CommandHelpAbout, null, emptyKey);
            MacObjC.SendVoid(appMenu, SelAddItem, MacObjC.Send(MacObjC.Class("NSMenuItem"), SelSeparatorItem));
            AddCommandItem(appMenu, "Quit " + MainWindowTitle.ProductName, viewModel.CommandFileExit, null, emptyKey, "q");
        }

        private void AddTopLevelBranch(IntPtr mainMenu, MenuNode node, IntPtr emptyKey)
        {
            var title = MacObjC.NsString(node.Caption ?? string.Empty);
            var item = CreateMenuItem(title, IntPtr.Zero, emptyKey);
            var submenu = MacObjC.AllocInit("NSMenu");
            MacObjC.SendVoid(item, SelSetSubmenu, submenu);
            MacObjC.SendVoid(mainMenu, SelAddItem, item);
            PopulateSubmenu(submenu, node.Children, emptyKey);
        }

        private void PopulateSubmenu(IntPtr menu, IList<MenuNode> children, IntPtr emptyKey)
        {
            if (children == null)
                return;
            foreach (var node in children)
            {
                if (node == null)
                    continue;
                if (node.HasChildren)
                {
                    var title = MacObjC.NsString(node.Caption ?? string.Empty);
                    var item = CreateMenuItem(title, IntPtr.Zero, emptyKey);
                    var submenu = MacObjC.AllocInit("NSMenu");
                    MacObjC.SendVoid(item, SelSetSubmenu, submenu);
                    MacObjC.SendVoid(menu, SelAddItem, item);
                    PopulateSubmenu(submenu, node.Children, emptyKey);
                    continue;
                }

                if (node.Command == null)
                    continue;

                var caption = node.Command.Text ?? node.Caption ?? "Command";
                AddCommandItem(menu, caption, node.Command, node.Parameter, emptyKey, isChecked: node.IsChecked);
            }
        }

        private void AddCommandItem(
            IntPtr menu,
            string title,
            ICommand command,
            object parameter,
            IntPtr emptyKey,
            string keyEquivalent = null,
            Func<bool> isChecked = null)
        {
            var nsTitle = MacObjC.NsString(title);
            var key = string.IsNullOrEmpty(keyEquivalent)
                ? emptyKey
                : MacObjC.NsString(keyEquivalent);
            var item = CreateMenuItem(nsTitle, MenuActionSelector, key);
            MacObjC.SendVoid(item, SelSetTarget, _menuTargetInstance);

            var tag = _nextTag++;
            MacObjC.SendVoid(item, SelSetTag, tag);
            var entry = new MenuEntry(tag, item, command, parameter, title, isChecked);
            _entries.Add(entry);
            HookCommand(command);
            MacObjC.SendVoid(menu, SelAddItem, item);
        }

        private IntPtr CreateMenuItem(IntPtr title, IntPtr action, IntPtr keyEquivalent)
        {
            var alloc = MacObjC.Alloc("NSMenuItem");
            return MacObjC.Send(alloc, SelInitWithTitleActionKeyEquivalent, title, action, keyEquivalent);
        }

        private void HookCommand(ICommand command)
        {
            if (command == null || _hookedCommands.Contains(command))
                return;
            _hookedCommands.Add(command);
            command.CanExecuteChanged += Command_OnCanExecuteChanged;
            command.PropertyChanged += Command_OnPropertyChanged;
        }

        private void Command_OnCanExecuteChanged(object sender, EventArgs e) => Refresh();

        private void Command_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null
                || e.PropertyName == nameof(ICommand.Text)
                || e.PropertyName == nameof(ICommand.Checked))
            {
                Refresh();
            }
        }

        private static unsafe void EnsureMenuTargetClass()
        {
            if (_menuTargetInstance != IntPtr.Zero)
                return;

            var super = MacObjC.Class("NSObject");
            _menuTargetClass = MacObjC.objc_allocateClassPair(super, "KozynaxMenuTarget", 0);
            var imp = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&MenuActionHandler;
            MacObjC.class_addMethod(_menuTargetClass, MenuActionSelector, imp, "v@:@");
            MacObjC.objc_registerClassPair(_menuTargetClass);
            _menuTargetInstance = MacObjC.class_createInstance(_menuTargetClass, 0);
        }

        [UnmanagedCallersOnly]
        private static void MenuActionHandler(IntPtr self, IntPtr cmd, IntPtr sender)
        {
            try
            {
                var tag = (int)MacObjC.SendLong(sender, SelTag);
                _current?.InvokeByTag(tag);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "MacNativeMenuBar.MenuActionHandler");
            }
        }

        private sealed class MenuEntry
        {
            public MenuEntry(
                int tag,
                IntPtr item,
                ICommand command,
                object parameter,
                string displayTitle,
                Func<bool> isChecked)
            {
                Tag = tag;
                Item = item;
                Command = command;
                Parameter = parameter;
                DisplayTitle = displayTitle ?? string.Empty;
                IsChecked = isChecked;
            }

            public int Tag { get; }
            public IntPtr Item { get; }
            public ICommand Command { get; }
            public object Parameter { get; }
            public string DisplayTitle { get; }
            public Func<bool> IsChecked { get; }

            public void Execute(object commandParameter)
            {
                if (Command == null)
                    return;

                object param;
                if (Parameter != null)
                    param = Parameter;
                else if (Command.CanExecute(null))
                    param = null;
                else
                    param = commandParameter;

                if (!Command.CanExecute(param))
                    return;

                Command.Execute(param);
            }

            public void ApplyVisualState(object commandParameter)
            {
                object param;
                if (Parameter != null)
                    param = Parameter;
                else if (Command.CanExecute(null))
                    param = null;
                else
                    param = commandParameter;

                MacObjC.SendVoid(Item, SelSetEnabled, (byte)(Command.CanExecute(param) ? 1 : 0));

                var title = !string.IsNullOrEmpty(Command.Text) ? Command.Text : DisplayTitle;
                MacObjC.SendVoid(Item, SelSetTitle, MacObjC.NsString(title));

                if (IsChecked != null)
                {
                    MacObjC.SendVoid(
                        Item,
                        SelSetState,
                        IsChecked() ? MacObjC.ControlStateOn : MacObjC.ControlStateOff);
                }
            }
        }
    }
}
