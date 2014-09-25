﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geodatabase;

using RasterEditor.Raster;

namespace RasterEditor
{
    /// <summary>
    /// Provide access to global attributes and functions.
    /// </summary>
    public static class Editor
    {
        #region Attributes

        private static ILayer activeLayer = null;

        private static bool isEditing = false;

        private static bool showEdits = true;

        private static PixelCollection editRecord = new PixelCollection();

        private static PixelCollection selectRecord = new PixelCollection();
                 
        #endregion

        #region Properties

        /// <summary>
        /// Get or set the active layer.
        /// </summary>
        public static ILayer ActiveLayer
        {
            set { activeLayer = value; }
            get { return activeLayer; }
        }

        /// <summary>
        /// Get or set a value indicating whether the active layer is under editing
        /// </summary>
        public static bool IsEditing
        {
            set { isEditing = value; }
            get { return isEditing; }
        }

        /// <summary>
        /// Get or set a value indicating whether edits are shown.
        /// </summary>
        public static bool ShowEdits
        {
            set { showEdits = value; }
            get { return showEdits; }
        }

        /// <summary>
        /// Get the cell collection of edits.
        /// </summary>
        public static PixelCollection EditRecord
        {
            get { return editRecord; }
        }

        /// <summary>
        /// Get the cell collection of selections
        /// </summary>
        public static PixelCollection SelectionRecord
        {
            get { return selectRecord; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Prepare the editor for editing.
        /// </summary>
        public static void StartEditing()
        {
            Editor.IsEditing = true;
            //Drawing.Clear();
            Editor.EditRecord.Clear();
            //Editor.SelectionRecord.Clear();
        }

        /// <summary>
        /// Stop editing.
        /// </summary>
        public static void StopEditing()
        {
            Editor.isEditing = false;
            Display.Clear();
            Editor.EditRecord.Clear();
            Editor.SelectionRecord.Clear();

            if (ArcMap.Application.CurrentTool.Caption == "Select")
            {
                ArcMap.Application.CurrentTool = null;
            }
        }

        /// <summary>
        /// Add new layers to ArcMap.
        /// </summary>
        /// <param name="fileName">The location of raster file.</param>
        /// <param name="activeView"></param>
        public static void AddLayer(string fileName)           
        {
            IRasterLayer rasterLayer = new RasterLayerClass();
            rasterLayer.CreateFromFilePath(fileName);
            ArcMap.Document.AddLayer((ILayer)rasterLayer);
            ArcMap.Document.ActiveView.Refresh();
        }

        /// <summary>
        /// Gets the layer with the specified name.
        /// </summary>
        /// <param name="layerName"></param>
        public static ILayer GetLayer(string layerName)
        {
            for (int i = 0; i < ArcMap.Document.FocusMap.LayerCount; i++)
            {
                ILayer layer = ArcMap.Document.FocusMap.Layer[i];
                if (layer.Name == layerName)
                {
                    return layer;
                }
            }

            return null;
        }

        /// <summary>
        /// Convert screen coordinates to map coordinates.
        /// </summary>         
        /// <param name="x">X-coordinate of the position on screen</param>
        /// <param name="y">Y-coordinate of the position on screen</param>
        /// <returns></returns>
        public static IPoint ScreenCoor2MapCoor(int x, int y)
        {
            IActiveView activeView = (IActiveView)ArcMap.Document.FocusMap;
            return  activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
        }

        /// <summary>
        /// Covert map coordinates to raster coordinates
        /// </summary>
        /// <param name="mapCoors">Coordinates of map</param>
        /// <returns></returns>
        public static Position MapCoor2RasterCoor(IPoint mapCoors)
        {
            if (ActiveLayer != null)
            {
                IRasterLayer layer = (IRasterLayer)Editor.ActiveLayer;
                IRaster2 raster = (IRaster2)layer.Raster;

                int row, col;
                raster.MapToPixel(mapCoors.X, mapCoors.Y, out col, out row);
                return new Position(col, row);
            }

            return null;
        }

        /// <summary>
        /// Convert screen coordinats to raster coordinates.
        /// </summary>
        /// <param name="x">X-coordinate of the position on screen</param>
        /// <param name="y">Y-coordinate of the position on screen</param>
        /// <returns></returns>
        public static Position ScreenCoor2RasterCoor(int x, int y)
        {
            IPoint mapCoor = ScreenCoor2MapCoor(x, y);
            return MapCoor2RasterCoor(mapCoor);
        }

        /// <summary>
        /// Save the edition as a specified file.
        /// </summary>
        /// <param name="fileName"></param>
        public static void SaveEditsAs(string fileName)
        {
            if (ActiveLayer == null)
                return;

            Random rnd = new Random();
            string tempFile = rnd.Next().ToString();

            ESRI.ArcGIS.RuntimeManager.BindLicense(ESRI.ArcGIS.ProductCode.EngineOrDesktop);

            // Get the original file
            IRasterLayer rasterLayer = (IRasterLayer)Editor.ActiveLayer;
            IWorkspaceFactory workspaceFactory = new RasterWorkspaceFactoryClass();
            IRasterWorkspace rasterWorkspace = (IRasterWorkspace)workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(rasterLayer.FilePath), 0);
            IRasterDataset rasterDataset = rasterWorkspace.OpenRasterDataset(System.IO.Path.GetFileName(rasterLayer.FilePath));

            // Open the new file save location
            IWorkspace mWorkspace = workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(fileName), 0);

            // Copy file to the new location   
            rasterDataset.Copy(tempFile, mWorkspace);

            // Save the original file to a new file
            ISaveAs saveAs = (ISaveAs)rasterDataset;
            IRasterDataset2 mRasterDataset = (IRasterDataset2)saveAs.SaveAs(System.IO.Path.GetFileName(fileName),
                                                                            mWorkspace,
                                                                            RasterFile.GetFormat(System.IO.Path.GetExtension(fileName)));
            IRaster mRaster = mRasterDataset.CreateFullRaster();

            // Save edits to file
            Editor.WriteEdits(mRaster);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(mRaster);
        }

        /// <summary>
        /// Save modification to the original file.
        /// </summary>
        public static void SaveEdits()
        {
            if (ActiveLayer == null || EditRecord.Count == 0)
                return;

            ESRI.ArcGIS.RuntimeManager.BindLicense(ESRI.ArcGIS.ProductCode.EngineOrDesktop);

            // Get the original file
            IRasterLayer rasterLayer = (IRasterLayer)Editor.ActiveLayer;
            IWorkspaceFactory workspaceFactory = new RasterWorkspaceFactoryClass();
            IRasterWorkspace rasterWorkspace = (IRasterWorkspace)workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(rasterLayer.FilePath), 0);
            IRasterDataset2 mRasterDataset = (IRasterDataset2)rasterWorkspace.OpenRasterDataset(System.IO.Path.GetFileName(rasterLayer.FilePath));
            IRaster mRaster = mRasterDataset.CreateFullRaster();

            Editor.WriteEdits(mRaster);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(mRaster);

            ArcMap.Document.ActiveView.Refresh();
        }

        /// <summary>
        /// Save the extent change into a new raster file.
        /// </summary>
        /// <param name="rasterLayer">Raster layer</param>
        /// <param name="fileName">Path of the output file</param>
        /// <param name="xmin">Minimum X coordinate</param>
        /// <param name="ymin">Minimum Y coordinate</param>                                                                          
        /// <param name="pixelSize">Pixel size</param>
        public static void SaveExtentAs(IRasterLayer rasterLayer, string fileName, double xmin, double ymin, double pixelSize)
        {
            IRasterProps rasterProps = (IRasterProps)rasterLayer.Raster;
            IEnvelope extent = new EnvelopeClass();
            extent.PutCoords(xmin, ymin, xmin + rasterProps.Width * pixelSize, ymin + rasterProps.Height * pixelSize);
            extent.Project(rasterProps.Extent.SpatialReference);
            rasterProps.Extent = extent;

            ISaveAs saveAs = (ISaveAs)rasterLayer.Raster;
            IWorkspaceFactory workspaceFactory = new RasterWorkspaceFactoryClass();
            IWorkspace mWorkspace = workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(fileName), 0);
            saveAs.SaveAs(System.IO.Path.GetFileName(fileName),
                          mWorkspace,
                          RasterFile.GetFormat(System.IO.Path.GetExtension(fileName)));
        }

        /// <summary>
        /// Gets value of pixel at the specified position.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rasterLayer"></param>
        /// <returns></returns>
        public static double GetValue(Position pos, IRasterLayer rasterLayer)
        {
            return GetValue(pos, rasterLayer.Raster);
        }

        /// <summary>
        /// Gets value of pixel at the specified position.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="raster"></param>
        /// <returns></returns>
        public static double GetValue(Position pos, IRaster raster)
        {
            IPnt regionSize = new PntClass();
            regionSize.SetCoords(1, 1);
            IPixelBlock pixelBlock = raster.CreatePixelBlock(regionSize);
            IPnt tl = new PntClass();
            tl.SetCoords(pos.Column, pos.Row);
            raster.Read(tl, pixelBlock);

            return Convert.ToDouble(pixelBlock.GetVal(0, 0, 0));
        }

        /// <summary>
        /// Get the pixel values in a region of the input raster.
        /// </summary>
        /// <param name="tlCorner"></param>
        /// <param name="brCorner"></param>
        /// <param name="raster"></param>
        /// <returns></returns>
        public static double[,] GetValues(Position tlCorner, Position brCorner, IRaster raster)
        {
            int colCount = brCorner.Column - tlCorner.Column + 1;
            int rowCount = brCorner.Row - tlCorner.Row + 1;

            IPnt regionSize = new PntClass();
            regionSize.SetCoords(colCount, rowCount);
            IPixelBlock pixelBlock = raster.CreatePixelBlock(regionSize);
            IPnt tl = new PntClass();
            tl.SetCoords(tlCorner.Column, tlCorner.Row);
            raster.Read(tl, pixelBlock);

            double[,] values = new double[colCount, rowCount];
            for (int x = 0; x < colCount; x++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    Pixel cell = Editor.EditRecord[x + tlCorner.Column, y + tlCorner.Row];

                    if (cell == null)
                    {
                        values[x, y] = Convert.ToDouble(pixelBlock.GetVal(0, x, y));
                    }
                    else 
                    {
                        values[x, y] = cell.NewValue;
                    }
                }
            }
            return values;
        }

        /// <summary>
        /// Convert the csharp value to the ArcObject pixel value.
        /// </summary>
        /// <param name="csharpValue">Cshapr value</param>
        /// <param name="pixelValueType">The pixel type of ouput value</param>
        /// <param name="pixelValue">Output pixel value</param>
        /// <returns>A value indicating whether the convention is successful</returns>
        public static bool CSharpValue2PixelValue(object csharpValue, rstPixelType pixelValueType, out object pixelValue)
        {
            try
            {
                switch (pixelValueType)
                {
                    case rstPixelType.PT_UCHAR:
                        pixelValue = (object)Convert.ToByte(csharpValue);
                        return true;
                    case rstPixelType.PT_CHAR:
                        pixelValue = (object)Convert.ToSByte(csharpValue);
                        return true;
                    case rstPixelType.PT_SHORT:
                        pixelValue = (object)Convert.ToInt16(csharpValue);
                        return true;
                    case rstPixelType.PT_USHORT:
                        pixelValue = (object)Convert.ToUInt16(csharpValue);
                        return true;
                    case rstPixelType.PT_LONG:
                        pixelValue = (object)Convert.ToInt32(csharpValue);
                        return true;
                    case rstPixelType.PT_ULONG:
                        pixelValue = (object)Convert.ToUInt32(csharpValue);
                        return true;
                    case rstPixelType.PT_FLOAT:
                        pixelValue = (object)Convert.ToSingle(csharpValue);
                        return true;
                    case rstPixelType.PT_DOUBLE:
                        pixelValue = (object)Convert.ToDouble(csharpValue);
                        return true;
                    default:
                        pixelValue = null;
                        return false;
                }
            }
            catch (Exception)
            {
                pixelValue = null;
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Write edits to the input raster.
        /// </summary>
        /// <param name="raster"></param>
        private static void WriteEdits(IRaster raster)
        {
            IRasterProps rasterProps = (IRasterProps)raster;

            int minRow = rasterProps.Height - 1;
            int maxRow = 0;
            int minCol = rasterProps.Width - 1;
            int maxCol = 0;

            for (int i = 0; i < Editor.EditRecord.Count; i++)
            {
                #region Get the extent of the edition region

                Position cellPos = Editor.EditRecord[i].Position;

                if (cellPos.Row > maxRow)
                {
                    maxRow = cellPos.Row;
                }

                if (cellPos.Row < minRow)
                {
                    minRow = cellPos.Row;
                }

                if (cellPos.Column > maxCol)
                {
                    maxCol = cellPos.Column;
                }

                if (cellPos.Column < minCol)
                {
                    minCol = cellPos.Column;
                }

                #endregion
            }

            IPnt pos = new PntClass();
            pos.SetCoords(maxCol - minCol + 1, maxRow - minRow + 1);
            IPixelBlock pixelBlock = raster.CreatePixelBlock(pos);
            pos.SetCoords(minCol, minRow);
            raster.Read(pos, pixelBlock);

            // Set new values
            IPixelBlock3 pixelBlock3 = (IPixelBlock3)pixelBlock;
            Array pixels = (Array)pixelBlock3.get_PixelData(0);

            for (int i = 0; i < Editor.EditRecord.Count; i++)
            {
                object value = null;
                Editor.CSharpValue2PixelValue(Editor.EditRecord[i].NewValue, rasterProps.PixelType, out value);

                pixels.SetValue(value,
                                Editor.EditRecord[i].Position.Column - minCol,
                                Editor.EditRecord[i].Position.Row - minRow);
            }

            pixelBlock3.set_PixelData(0, (System.Object)pixels);
            IRasterEdit rasterEdit = (IRasterEdit)raster;
            rasterEdit.Write(pos, (IPixelBlock)pixelBlock3);    
        }

        #endregion
    }
}
