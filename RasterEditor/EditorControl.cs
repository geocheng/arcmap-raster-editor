using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.Desktop.AddIns;

namespace RasterEditor
{
    static class EditorControl
    {
        #region Methods

        /// <summary>
        /// Change controls when the edition starts.
        /// </summary>
        public static void StartEditing()
        {
            // While editing, the active layer cannot be changed
            LayerComboBox layerComboBox = AddIn.FromID<LayerComboBox>(ThisAddIn.IDs.LayerComboBox);
            layerComboBox.IsEnabled = false;

            // Enable the save button
            SaveEditsButton saveButton = AddIn.FromID<SaveEditsButton>(ThisAddIn.IDs.SaveEditsButton);
            saveButton.IsEnabled = true;

            // Enable the save as button
            SaveEditsAsButton saveAsButton = AddIn.FromID<SaveEditsAsButton>(ThisAddIn.IDs.SaveEditsAsButton);
            saveAsButton.IsEnabled = true;

            // Enable the stop button
            StopEditingButton stopButton = AddIn.FromID<StopEditingButton>(ThisAddIn.IDs.StopEditingButton);
            stopButton.IsEnabled = true;

            // Enable the select tool
            SelectTool selectTool = AddIn.FromID<SelectTool>(ThisAddIn.IDs.SelectTool);
            selectTool.IsEnabled = true;

            // Disable the start button
            StartEditingButton startEditingButton = AddIn.FromID<StartEditingButton>(ThisAddIn.IDs.StartEditingButton);
            startEditingButton.IsEnabled = false;

            // Enable the ShowEditsButton
            ShowEditsButton showEditsButton = AddIn.FromID<ShowEditsButton>(ThisAddIn.IDs.ShowEditsButton);
            showEditsButton.IsEnabled = true;
        }

        /// <summary>
        /// Change controls when the edition stops.
        /// </summary>
        public static void StopEditing()
        {
            StopEditingButton stopEditingButton = AddIn.FromID<StopEditingButton>(ThisAddIn.IDs.StopEditingButton);
            stopEditingButton.IsEnabled = false;

            StartEditingButton startEditionButton = AddIn.FromID<StartEditingButton>(ThisAddIn.IDs.StartEditingButton);
            startEditionButton.IsEnabled = true;

            SaveEditsButton saveEditsButton = AddIn.FromID<SaveEditsButton>(ThisAddIn.IDs.SaveEditsButton);
            saveEditsButton.IsEnabled = false;

            SaveEditsAsButton saveEditsAsButton = AddIn.FromID<SaveEditsAsButton>(ThisAddIn.IDs.SaveEditsAsButton);
            saveEditsAsButton.IsEnabled = false;

            LayerComboBox layerComboBox = AddIn.FromID<LayerComboBox>(ThisAddIn.IDs.LayerComboBox);
            layerComboBox.IsEnabled = true;

            SelectTool selectTool = AddIn.FromID<SelectTool>(ThisAddIn.IDs.SelectTool);
            selectTool.IsEnabled = false;

            ShowEditsButton showEditsButton = AddIn.FromID<ShowEditsButton>(ThisAddIn.IDs.ShowEditsButton);
            showEditsButton.IsEnabled = false;
        }

        #endregion
    }
}
