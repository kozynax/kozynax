using System;
using System.Collections.Generic;
using System.ComponentModel;
using Kozynax.UI;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;
namespace ZXMAK2.Host.SdlBackend.Views
{
    public sealed class TerminalMachineSettingsView : IMachineSettingsView
    {
        private readonly ITerminal _terminal;
        private MachineSettings _ui;
        private bool _loopActive;
        private bool _closeRequested;
        private readonly Dictionary<BusDeviceBase, DeviceSettingsPanelFactory.DevicePanel> _panels =
            new Dictionary<BusDeviceBase, DeviceSettingsPanelFactory.DevicePanel>();

        public TerminalMachineSettingsView(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public void Init(MachineSettings machineSettings)
        {
            DetachUi();
            _ui = machineSettings ?? throw new ArgumentNullException(nameof(machineSettings));
            _ui.Init();
            _ui.Closed += OnClosed;
            _ui.Applying += OnApplying;
            _ui.ShowWizard += OnShowWizard;
            _ui.Devices.SelectedIndexChanged += OnDeviceSelected;
            _ui.Devices.List.ListChanged += OnDeviceListChanged;
        }

        public void Init(IHostService host, IVirtualMachine vm)
        {
            _ui.Init(host, vm);
            SyncPanels();
            SyncSelectedPanel();
        }

        public DlgResult ShowDialog(object owner)
        {
            if (_ui == null || !_terminal.IsAvailable)
                return DlgResult.Cancel;

            _closeRequested = false;
            _loopActive = true;
            _terminal.PrepareForUiInput();
            var presenter = new TerminalKozuiPresenter(_terminal);
            presenter.Attach(_ui.Root);

            try
            {
                TerminalUiSession.Run(
                    _terminal,
                    presenter,
                    () => _closeRequested,
                    ev =>
                    {
                        if (ev.Kind == TerminalEventKind.Quit)
                        {
                            _closeRequested = true;
                            return true;
                        }

                        if (TerminalDialogInput.IsEscape(ev))
                        {
                            _ui.Cancel.Click(_ui.Cancel, EventArgs.Empty);
                            return true;
                        }

                        TerminalDialogInput.Route(presenter, ev);
                        return false;
                    });
            }
            finally
            {
                _loopActive = false;
                _terminal.EndUiInput();
            }

            return DlgResult.OK;
        }

        public void Dispose()
            => DetachUi();

        private void OnClosed(object sender, EventArgs e)
            => _closeRequested = true;

        private void OnApplying(object sender, EventArgs e)
        {
            foreach (var panel in _panels.Values)
                panel.Apply();
        }

        private void OnShowWizard(object sender, IList<MachineSettings.MachineConfiguration> machines)
        {
            if (machines == null || machines.Count == 0 || !_loopActive)
                return;

            var picked = PickMachine(machines);
            if (picked != null)
            {
                _ui.CreateMachine(picked);
                SyncPanels();
                SyncSelectedPanel();
            }
        }

        private MachineSettings.MachineConfiguration PickMachine(
            IList<MachineSettings.MachineConfiguration> machines)
        {
            var list = new ListView<MachineSettings.MachineConfiguration>
            {
                ItemTextSelector = m => m?.Name ?? string.Empty,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinHeight = 10,
            };
            list.Reset(machines);
            list.SelectedIndex = 0;

            var ok = new Button { Text = "OK" };
            var cancel = new Button { Text = "Cancel" };
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Dock = Dock.Bottom,
                Margin = new Thickness(0, 1, 0, 0),
            };
            buttons.Add(ok);
            buttons.Add(cancel);

            var root = new DockPanel { Margin = new Thickness(1) };
            root.Add(new Label { Text = "Select machine", Dock = Dock.Top, Margin = new Thickness(0, 0, 0, 1) });
            root.Add(buttons);
            root.Add(list);

            _terminal.PrepareForUiInput();
            var presenter = new TerminalKozuiPresenter(_terminal);
            presenter.Attach(root);

            MachineSettings.MachineConfiguration result = null;
            var done = false;
            ok.Clicked += (_, __) =>
            {
                if (list.SelectedIndex >= 0 && list.SelectedIndex < list.List.Count)
                    result = list.List[list.SelectedIndex];
                done = true;
            };
            cancel.Clicked += (_, __) => done = true;

            try
            {
                var cancelled = false;
                TerminalUiSession.Run(
                    _terminal,
                    presenter,
                    () => done || cancelled,
                    ev =>
                    {
                        if (ev.Kind == TerminalEventKind.Quit || TerminalDialogInput.IsEscape(ev))
                        {
                            cancelled = true;
                            result = null;
                            return true;
                        }

                        TerminalDialogInput.Route(presenter, ev);
                        return false;
                    });

                return cancelled ? null : result;
            }
            finally
            {
                _terminal.EndUiInput();
            }
        }

        private void OnDeviceSelected(object sender, int index)
            => SyncSelectedPanel();

        private void OnDeviceListChanged(object sender, ListChangedEventArgs e)
        {
            SyncPanels();
            SyncSelectedPanel();
        }

        private void SyncPanels()
        {
            if (_ui?.WorkBus == null)
                return;

            var devices = _ui.Devices.List;
            foreach (var device in devices)
            {
                if (_panels.ContainsKey(device))
                    continue;
                _panels[device] = DeviceSettingsPanelFactory.Create(_ui.WorkBus, _ui.Host, device);
            }

            foreach (var key in new List<BusDeviceBase>(_panels.Keys))
            {
                if (!devices.Contains(key))
                    _panels.Remove(key);
            }
        }

        private void SyncSelectedPanel()
        {
            if (_ui == null)
                return;

            var index = _ui.Devices.SelectedIndex;
            if (index < 0 || index >= _ui.Devices.List.Count)
            {
                _ui.DeviceProperties.Content = new Label { Text = "No device selected" };
                return;
            }

            var device = _ui.Devices.List[index];
            if (!_panels.TryGetValue(device, out var panel))
            {
                panel = DeviceSettingsPanelFactory.Create(_ui.WorkBus, _ui.Host, device);
                _panels[device] = panel;
            }

            _ui.DeviceProperties.Content = panel.Root;
        }

        private void DetachUi()
        {
            if (_ui == null)
                return;
            _ui.Closed -= OnClosed;
            _ui.Applying -= OnApplying;
            _ui.ShowWizard -= OnShowWizard;
            _ui.Devices.SelectedIndexChanged -= OnDeviceSelected;
            _ui.Devices.List.ListChanged -= OnDeviceListChanged;
            _panels.Clear();
            _ui = null;
        }
    }
}
