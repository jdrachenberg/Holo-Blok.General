using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellType = NPOI.SS.UserModel.CellType;

namespace HoloBlok.Common.Utils.Excel
{
    internal class ExcelImporter : IDisposable
    {
        private IWorkbook _workbook;
        private string _currentFilePath;
        private bool _disposed = false;

        /// <summary>
        /// Opens a file dialog to select an Excel file
        /// </summary>
        /// <returns>The selected file path, or null if cancelled</returns>
        public string SelectExcelFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Excel File";
                openFileDialog.Filter = "Excel Files|*.xlsx;*.xls;*.xlsm|All Files|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }
            return null;
        }

        /// <summary>
        /// Opens an Excel file and loads it into memory
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool OpenFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    TaskDialog.Show("Error", $"File not found: {filePath}");
                    return false;
                }

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    string extension = Path.GetExtension(filePath).ToLower();

                    if (extension == ".xlsx" || extension == ".xlsm")
                    {
                        _workbook = new XSSFWorkbook(fileStream);
                    }
                    else if (extension == ".xls")
                    {
                        _workbook = new HSSFWorkbook(fileStream);
                    }
                    else
                    {
                        TaskDialog.Show("Error", "Unsupported file format");
                        return false;
                    }
                }

                _currentFilePath = filePath;
                return true;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to open Excel file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the contents of a column as a list of doubles
        /// </summary>
        /// <param name="columnLetter">The column letter (e.g., "A", "B", "AA")</param>
        /// <param name="sheetIndex">Sheet index (0-based), default is 0</param>
        /// <param name="startRow">Starting row (1-based), default is 1</param>
        /// <param name="includeEmptyCells">Whether to include null for empty cells</param>
        /// <returns>List of cell values as nullable doubles</returns>
        public List<double> GetColumnDataAsDouble(string columnLetter, int sheetIndex = 0, int startRow = 1, bool includeEmptyCells = false)
        {
            var result = new List<double>();

            if (_workbook == null)
            {
                TaskDialog.Show("Error", "No Excel file is currently open");
                return result;
            }

            try
            {
                ISheet sheet = _workbook.GetSheetAt(sheetIndex);
                if (sheet == null)
                {
                    TaskDialog.Show("Error", $"Sheet at index {sheetIndex} not found");
                    return result;
                }

                int columnIndex = ColumnLetterToIndex(columnLetter);
                int lastRowNum = sheet.LastRowNum;

                for (int rowIndex = startRow - 1; rowIndex <= lastRowNum; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row == null)
                    {
                        if (includeEmptyCells)
                            result.Add(0);
                        continue;
                    }

                    ICell cell = row.GetCell(columnIndex);
                    double cellValue = GetCellValueAsDouble(cell);

                    if (cellValue > 0 || includeEmptyCells)
                    {
                        result.Add(cellValue);
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to read column data: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets the contents of a column as a list of strings
        /// </summary>
        /// <param name="columnLetter">The column letter (e.g., "A", "B", "AA")</param>
        /// <param name="sheetIndex">Sheet index (0-based), default is 0</param>
        /// <param name="startRow">Starting row (1-based), default is 1</param>
        /// <param name="includeEmptyCells">Whether to include empty cells in the result</param>
        /// <returns>List of cell values as strings</returns>
        public List<string> GetColumnDataAsString(string columnLetter, int startRow = 1, bool includeEmptyCells = false, int sheetIndex = 0)
        {
            var result = new List<string>();

            if (_workbook == null)
            {
                TaskDialog.Show("Error", "No Excel file is currently open");
                return result;
            }

            try
            {
                ISheet sheet = _workbook.GetSheetAt(sheetIndex);
                if (sheet == null)
                {
                    TaskDialog.Show("Error", $"Sheet at index {sheetIndex} not found");
                    return result;
                }

                int columnIndex = ColumnLetterToIndex(columnLetter);
                int lastRowNum = sheet.LastRowNum;

                for (int rowIndex = startRow - 1; rowIndex <= lastRowNum; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row == null)
                    {
                        if (includeEmptyCells)
                            result.Add(string.Empty);
                        continue;
                    }

                    ICell cell = row.GetCell(columnIndex);
                    string cellValue = GetCellValueAsString(cell);

                    if (!string.IsNullOrEmpty(cellValue) || includeEmptyCells)
                    {
                        result.Add(cellValue);
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to read column data: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets the contents of a column as a list of strings from a specific sheet by name
        /// </summary>
        /// <param name="columnLetter">The column letter (e.g., "A", "B", "AA")</param>
        /// <param name="sheetName">Name of the sheet</param>
        /// <param name="startRow">Starting row (1-based), default is 1</param>
        /// <param name="includeEmptyCells">Whether to include empty cells in the result</param>
        /// <returns>List of cell values as strings</returns>
        public List<string> GetColumnDataBySheetName(string columnLetter, string sheetName, int startRow = 1, bool includeEmptyCells = false)
        {
            if (_workbook == null)
            {
                TaskDialog.Show("Error", "No Excel file is currently open");
                return new List<string>();
            }

            ISheet sheet = _workbook.GetSheet(sheetName);
            if (sheet == null)
            {
                TaskDialog.Show("Error", $"Sheet '{sheetName}' not found");
                return new List<string>();
            }

            int sheetIndex = _workbook.GetSheetIndex(sheet);
            return GetColumnDataAsString(columnLetter, startRow, includeEmptyCells, sheetIndex);
        }

        /// <summary>
        /// Gets all sheet names in the workbook
        /// </summary>
        /// <returns>List of sheet names</returns>
        public List<string> GetSheetNames()
        {
            var sheetNames = new List<string>();

            if (_workbook == null)
                return sheetNames;

            for (int i = 0; i < _workbook.NumberOfSheets; i++)
            {
                sheetNames.Add(_workbook.GetSheetName(i));
            }

            return sheetNames;
        }

        /// <summary>
        /// Converts column letter to zero-based column index
        /// </summary>
        private int ColumnLetterToIndex(string columnLetter)
        {
            columnLetter = columnLetter.ToUpper();
            int columnIndex = 0;

            for (int i = 0; i < columnLetter.Length; i++)
            {
                columnIndex *= 26;
                columnIndex += (columnLetter[i] - 'A' + 1);
            }

            return columnIndex - 1; // Convert to zero-based
        }

        /// <summary>
        /// Gets cell value as double, handling different cell types
        /// </summary>
        private double GetCellValueAsDouble(ICell cell)
        {
            if (cell == null)
                return default;

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    return cell.NumericCellValue;

                case CellType.String:
                    // Try to parse string as double
                    if (double.TryParse(cell.StringCellValue, out double stringValue))
                        return stringValue;
                    return default;

                case CellType.Formula:
                    // Evaluate formula
                    switch (cell.CachedFormulaResultType)
                    {
                        case CellType.Numeric:
                            return cell.NumericCellValue;
                        case CellType.String:
                            if (double.TryParse(cell.StringCellValue, out double formulaStringValue))
                                return formulaStringValue;
                            return default;
                        default:
                            return default;
                    }

                case CellType.Boolean:
                    return cell.BooleanCellValue ? 1.0 : 0.0;

                case CellType.Blank:
                case CellType.Error:
                default:
                    return default;
            }
        }

        /// <summary>
        /// Gets cell value as string, handling different cell types
        /// </summary>
        private string GetCellValueAsString(ICell cell)
        {
            if (cell == null)
                return string.Empty;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue.ToString();
                    }
                    return cell.NumericCellValue.ToString();

                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();

                case CellType.Formula:
                    // Evaluate formula
                    switch (cell.CachedFormulaResultType)
                    {
                        case CellType.String:
                            return cell.StringCellValue;
                        case CellType.Numeric:
                            return cell.NumericCellValue.ToString();
                        case CellType.Boolean:
                            return cell.BooleanCellValue.ToString();
                        default:
                            return cell.ToString();
                    }

                case CellType.Blank:
                    return string.Empty;

                case CellType.Error:
                    return $"#ERROR: {cell.ErrorCellValue}";

                default:
                    return cell.ToString();
            }
        }

        /// <summary>
        /// Closes the current workbook
        /// </summary>
        public void CloseWorkbook()
        {
            if (_workbook != null)
            {
                _workbook.Close();
                _workbook = null;
                _currentFilePath = null;
            }
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CloseWorkbook();
                }
                _disposed = true;
            }
        }
    }
}
