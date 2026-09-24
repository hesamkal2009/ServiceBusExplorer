using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using ServiceBusExplorer.Controls;
using ServiceBusExplorer.Properties;

namespace ServiceBusExplorer.UIHelpers
{
    public enum ThemeMode
    {
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private static readonly Color FormBackColor = Color.FromArgb(28, 30, 34);
        private static readonly Color ControlBackColor = Color.FromArgb(43, 46, 52);
        private static readonly Color InputBackColor = Color.FromArgb(23, 25, 29);
        private static readonly Color BorderColor = Color.FromArgb(93, 99, 108);
        private static readonly Color ForeColor = Color.FromArgb(242, 244, 247);
        private static readonly Color SecondaryForeColor = Color.FromArgb(198, 203, 211);
        private static readonly Color SelectionBackColor = Color.FromArgb(47, 111, 179);

        private static readonly Dictionary<Control, ControlColors> OriginalColors = new Dictionary<Control, ControlColors>();
        private static readonly Dictionary<ToolStripItem, ToolStripColors> OriginalToolStripColors = new Dictionary<ToolStripItem, ToolStripColors>();
        private static readonly Dictionary<ToolStrip, ToolStripColors> OriginalToolStrips = new Dictionary<ToolStrip, ToolStripColors>();
        private static readonly Dictionary<Button, ButtonState> OriginalButtonStates = new Dictionary<Button, ButtonState>();
        private static readonly Dictionary<DataGridView, DataGridViewState> OriginalDataGridViewStates = new Dictionary<DataGridView, DataGridViewState>();
        private static readonly Dictionary<Grouper, GrouperColors> OriginalGrouperColors = new Dictionary<Grouper, GrouperColors>();
        private static readonly Dictionary<HeaderPanel, HeaderPanelState> OriginalHeaderPanelStates = new Dictionary<HeaderPanel, HeaderPanelState>();
        private static readonly HashSet<Form> AppliedForms = new HashSet<Form>();
        private static bool initialized;
        private static bool isDarkEnabled;
        private static bool needsApply;

        public static Color SurfaceColor => ControlBackColor;
        public static Color SurfaceBorderColor => BorderColor;
        public static Color InputColor => InputBackColor;
        public static Color TextColor => ForeColor;

        public static ThemeMode CurrentMode
        {
            get => GetConfiguredMode();
            set
            {
                Settings.Light.ThemeMode = value.ToString();
                Settings.Light.Save();
                needsApply = true;
                ApplyToOpenForms();
            }
        }

        public static bool IsDarkEnabled => isDarkEnabled;

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            UpdateEffectiveTheme();
            needsApply = true;
            Application.Idle += Application_Idle;
        }

        public static void ApplyToOpenForms()
        {
            UpdateEffectiveTheme();

            if (!isDarkEnabled)
            {
                if (!needsApply && AppliedForms.Count == 0)
                {
                    return;
                }

                RestoreOpenForms();
                AppliedForms.Clear();
                needsApply = false;
                RefreshOpenForms();
                return;
            }

            var hasNewForm = false;
            foreach (Form form in Application.OpenForms)
            {
                if (!AppliedForms.Contains(form))
                {
                    hasNewForm = true;
                    break;
                }
            }

            if (!needsApply && !hasNewForm)
            {
                return;
            }

            foreach (Form form in Application.OpenForms)
            {
                ApplyControl(form);
                AppliedForms.Add(form);
            }
            needsApply = false;
            RefreshOpenForms();
        }

        public static void ReapplyToOpenForms()
        {
            needsApply = true;
            AppliedForms.Clear();
            ApplyToOpenForms();
        }

        private static void RefreshOpenForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                form.PerformLayout();
                form.Invalidate(true);
                form.Update();
            }
        }

        private static void Application_Idle(object sender, EventArgs e)
        {
            ApplyToOpenForms();
        }

        private static ThemeMode GetConfiguredMode()
        {
            return ParseConfiguredMode(Settings.Light.ThemeMode);
        }

        internal static ThemeMode ParseConfiguredMode(string configuredMode)
        {
            if (Enum.TryParse(configuredMode, true, out ThemeMode mode) && mode == ThemeMode.Dark)
            {
                return ThemeMode.Dark;
            }

            return ThemeMode.Light;
        }

        private static void UpdateEffectiveTheme()
        {
            var configuredMode = GetConfiguredMode();
            var shouldUseDarkTheme = configuredMode == ThemeMode.Dark;
            if (isDarkEnabled != shouldUseDarkTheme)
            {
                isDarkEnabled = shouldUseDarkTheme;
                needsApply = true;
            }
        }

        private static void Control_Added(object sender, ControlEventArgs e)
        {
            if (isDarkEnabled)
            {
                ApplyControl(e.Control);
            }
        }

        private static void ApplyControl(Control control)
        {
            CaptureOriginalColors(control);
            control.ControlAdded -= Control_Added;
            control.ControlAdded += Control_Added;
            control.BackColorChanged -= Control_BackColorChanged;
            control.BackColorChanged += Control_BackColorChanged;
            control.ForeColorChanged -= Control_ForeColorChanged;
            control.ForeColorChanged += Control_ForeColorChanged;

            SetDarkColors(control);

            if (control is Button button)
            {
                if (!OriginalButtonStates.ContainsKey(button))
                {
                    OriginalButtonStates.Add(button, new ButtonState(button));
                }

                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = BorderColor;
                button.FlatAppearance.MouseOverBackColor = BorderColor;
                button.FlatAppearance.MouseDownBackColor = SelectionBackColor;
            }

            if (control is DataGridView dataGridView)
            {
                if (!OriginalDataGridViewStates.ContainsKey(dataGridView))
                {
                    OriginalDataGridViewStates.Add(dataGridView, new DataGridViewState(dataGridView));
                }

                dataGridView.EnableHeadersVisualStyles = false;
                dataGridView.BackgroundColor = InputBackColor;
                dataGridView.GridColor = BorderColor;
                dataGridView.DefaultCellStyle.BackColor = InputBackColor;
                dataGridView.DefaultCellStyle.ForeColor = ForeColor;
                dataGridView.DefaultCellStyle.SelectionBackColor = SelectionBackColor;
                dataGridView.DefaultCellStyle.SelectionForeColor = Color.White;
                dataGridView.ColumnHeadersDefaultCellStyle.BackColor = ControlBackColor;
                dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
                dataGridView.RowHeadersDefaultCellStyle.BackColor = ControlBackColor;
                dataGridView.RowHeadersDefaultCellStyle.ForeColor = ForeColor;
            }

            if (control is Grouper grouper)
            {
                if (!OriginalGrouperColors.ContainsKey(grouper))
                {
                    OriginalGrouperColors.Add(grouper, new GrouperColors(grouper));
                }

                grouper.BackgroundColor = ControlBackColor;
                grouper.BackgroundGradientColor = ControlBackColor;
                grouper.BorderColor = BorderColor;
                grouper.CustomGroupBoxColor = ControlBackColor;
                grouper.ForeColor = ForeColor;
                grouper.BackColor = FormBackColor;
            }

            if (control is HeaderPanel headerPanel)
            {
                if (!OriginalHeaderPanelStates.ContainsKey(headerPanel))
                {
                    OriginalHeaderPanelStates.Add(headerPanel, new HeaderPanelState(headerPanel));
                }

                headerPanel.BackColor = InputBackColor;
                headerPanel.ForeColor = ForeColor;
                headerPanel.HeaderColor1 = ControlBackColor;
                headerPanel.HeaderColor2 = BorderColor;
            }

            if (control is ToolStrip toolStrip)
            {
                ApplyToolStrip(toolStrip);
            }

            if (control.ContextMenuStrip != null)
            {
                ApplyToolStrip(control.ContextMenuStrip);
            }

            foreach (Control child in control.Controls)
            {
                ApplyControl(child);
            }
        }

        private static void Control_BackColorChanged(object sender, EventArgs e)
        {
            if (isDarkEnabled)
            {
                SetDarkColors((Control)sender);
            }
        }

        private static void Control_ForeColorChanged(object sender, EventArgs e)
        {
            if (isDarkEnabled)
            {
                SetDarkColors((Control)sender);
            }
        }

        private static void SetDarkColors(Control control)
        {
            var backColor = control is TextBoxBase || control is ComboBox || control is NumericUpDown || control is ListControl
                ? InputBackColor
                : control is Label || control is LinkLabel
                    ? FormBackColor
                    : ControlBackColor;
            var foreColor = control is Label || control is LinkLabel ? SecondaryForeColor : ForeColor;

            if (control.BackColor != backColor)
            {
                control.BackColor = backColor;
            }

            if (control.ForeColor != foreColor)
            {
                control.ForeColor = foreColor;
            }
        }

        private static void ApplyToolStrip(ToolStrip toolStrip)
        {
            if (!OriginalToolStrips.ContainsKey(toolStrip))
            {
                OriginalToolStrips.Add(toolStrip, new ToolStripColors(toolStrip.BackColor, toolStrip.ForeColor));
            }

            toolStrip.BackColor = ControlBackColor;
            toolStrip.ForeColor = ForeColor;

            foreach (ToolStripItem item in toolStrip.Items)
            {
                CaptureOriginalColors(item);
                item.BackColor = ControlBackColor;
                item.ForeColor = ForeColor;
                if (item is ToolStripDropDownItem dropDownItem && dropDownItem.DropDown != null)
                {
                    ApplyToolStrip(dropDownItem.DropDown);
                }
            }
        }

        private static void CaptureOriginalColors(Control control)
        {
            if (!OriginalColors.ContainsKey(control))
            {
                OriginalColors.Add(control, new ControlColors(control.BackColor, control.ForeColor));
            }
        }

        private static void CaptureOriginalColors(ToolStripItem item)
        {
            if (!OriginalToolStripColors.ContainsKey(item))
            {
                OriginalToolStripColors.Add(item, new ToolStripColors(item.BackColor, item.ForeColor));
            }
        }

        private static void RestoreOpenForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                RestoreControl(form);
            }
        }

        private static void RestoreControl(Control control)
        {
            if (OriginalColors.TryGetValue(control, out var colors))
            {
                control.BackColor = colors.BackColor;
                control.ForeColor = colors.ForeColor;
            }

            if (control is Button button && OriginalButtonStates.TryGetValue(button, out var buttonState))
            {
                buttonState.Restore(button);
            }

            if (control is DataGridView dataGridView && OriginalDataGridViewStates.TryGetValue(dataGridView, out var state))
            {
                state.Restore(dataGridView);
            }

            if (control is Grouper grouper && OriginalGrouperColors.TryGetValue(grouper, out var grouperColors))
            {
                grouperColors.Restore(grouper);
            }

            if (control is HeaderPanel headerPanel && OriginalHeaderPanelStates.TryGetValue(headerPanel, out var headerPanelState))
            {
                headerPanelState.Restore(headerPanel);
            }

            if (control is ToolStrip toolStrip)
            {
                RestoreToolStrip(toolStrip);
            }

            if (control.ContextMenuStrip != null)
            {
                RestoreToolStrip(control.ContextMenuStrip);
            }

            foreach (Control child in control.Controls)
            {
                RestoreControl(child);
            }
        }

        private static void RestoreToolStrip(ToolStrip toolStrip)
        {
            if (OriginalToolStrips.TryGetValue(toolStrip, out var toolStripColors))
            {
                toolStrip.BackColor = toolStripColors.BackColor;
                toolStrip.ForeColor = toolStripColors.ForeColor;
            }

            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (OriginalToolStripColors.TryGetValue(item, out var colors))
                {
                    item.BackColor = colors.BackColor;
                    item.ForeColor = colors.ForeColor;
                }

                if (item is ToolStripDropDownItem dropDownItem && dropDownItem.DropDown != null)
                {
                    RestoreToolStrip(dropDownItem.DropDown);
                }
            }
        }

        private sealed class ControlColors
        {
            public ControlColors(Color backColor, Color foreColor)
            {
                BackColor = backColor;
                ForeColor = foreColor;
            }

            public Color BackColor { get; }
            public Color ForeColor { get; }
        }

        private sealed class ToolStripColors
        {
            public ToolStripColors(Color backColor, Color foreColor)
            {
                BackColor = backColor;
                ForeColor = foreColor;
            }

            public Color BackColor { get; }
            public Color ForeColor { get; }
        }

        private sealed class ButtonState
        {
            private readonly Color borderColor;
            private readonly FlatStyle flatStyle;
            private readonly Color mouseDownBackColor;
            private readonly Color mouseOverBackColor;

            public ButtonState(Button button)
            {
                borderColor = button.FlatAppearance.BorderColor;
                flatStyle = button.FlatStyle;
                mouseDownBackColor = button.FlatAppearance.MouseDownBackColor;
                mouseOverBackColor = button.FlatAppearance.MouseOverBackColor;
            }

            public void Restore(Button button)
            {
                button.FlatStyle = flatStyle;
                button.FlatAppearance.BorderColor = borderColor;
                button.FlatAppearance.MouseDownBackColor = mouseDownBackColor;
                button.FlatAppearance.MouseOverBackColor = mouseOverBackColor;
            }
        }

        private sealed class DataGridViewState
        {
            private readonly bool enableHeadersVisualStyles;
            private readonly Color backgroundColor;
            private readonly Color gridColor;
            private readonly DataGridViewCellStyle defaultCellStyle;
            private readonly DataGridViewCellStyle columnHeadersDefaultCellStyle;
            private readonly DataGridViewCellStyle rowHeadersDefaultCellStyle;

            public DataGridViewState(DataGridView dataGridView)
            {
                enableHeadersVisualStyles = dataGridView.EnableHeadersVisualStyles;
                backgroundColor = dataGridView.BackgroundColor;
                gridColor = dataGridView.GridColor;
                defaultCellStyle = dataGridView.DefaultCellStyle.Clone();
                columnHeadersDefaultCellStyle = dataGridView.ColumnHeadersDefaultCellStyle.Clone();
                rowHeadersDefaultCellStyle = dataGridView.RowHeadersDefaultCellStyle.Clone();
            }

            public void Restore(DataGridView dataGridView)
            {
                dataGridView.EnableHeadersVisualStyles = enableHeadersVisualStyles;
                dataGridView.BackgroundColor = backgroundColor;
                dataGridView.GridColor = gridColor;
                dataGridView.DefaultCellStyle = defaultCellStyle;
                dataGridView.ColumnHeadersDefaultCellStyle = columnHeadersDefaultCellStyle;
                dataGridView.RowHeadersDefaultCellStyle = rowHeadersDefaultCellStyle;
            }
        }

        private sealed class GrouperColors
        {
            private readonly Color backColor;
            private readonly Color backgroundColor;
            private readonly Color backgroundGradientColor;
            private readonly Color borderColor;
            private readonly Color customGroupBoxColor;
            private readonly Color foreColor;

            public GrouperColors(Grouper grouper)
            {
                backColor = grouper.BackColor;
                backgroundColor = grouper.BackgroundColor;
                backgroundGradientColor = grouper.BackgroundGradientColor;
                borderColor = grouper.BorderColor;
                customGroupBoxColor = grouper.CustomGroupBoxColor;
                foreColor = grouper.ForeColor;
            }

            public void Restore(Grouper grouper)
            {
                grouper.BackColor = backColor;
                grouper.BackgroundColor = backgroundColor;
                grouper.BackgroundGradientColor = backgroundGradientColor;
                grouper.BorderColor = borderColor;
                grouper.CustomGroupBoxColor = customGroupBoxColor;
                grouper.ForeColor = foreColor;
            }
        }

        private sealed class HeaderPanelState
        {
            private readonly Color backColor;
            private readonly Color foreColor;
            private readonly Color headerColor1;
            private readonly Color headerColor2;

            public HeaderPanelState(HeaderPanel headerPanel)
            {
                backColor = headerPanel.BackColor;
                foreColor = headerPanel.ForeColor;
                headerColor1 = headerPanel.HeaderColor1;
                headerColor2 = headerPanel.HeaderColor2;
            }

            public void Restore(HeaderPanel headerPanel)
            {
                headerPanel.BackColor = backColor;
                headerPanel.ForeColor = foreColor;
                headerPanel.HeaderColor1 = headerColor1;
                headerPanel.HeaderColor2 = headerColor2;
            }
        }
    }
}