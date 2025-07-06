using HoloBlok.Common.DataSets;
using HoloBlok.Tools.Electrical.DataSync;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BorderStyle = NPOI.SS.UserModel.BorderStyle;
using FillPattern = NPOI.SS.UserModel.FillPattern;
using HorizontalAlignment = NPOI.SS.UserModel.HorizontalAlignment;

namespace HoloBlok.Common.Utils.Excel
{
    internal class ExcelExporter
    {
        public string ExportMechanicalEquipment(List<MechEquipData> equipmentData, string projectName)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    Title = "Save Mechanical Equipment Data",
                    FileName = $"{projectName}_MechEquipData_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return null;

                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet("Mechanical Equipment");

                var excelStyle = new ExcelStyle();

                var headerStyle = excelStyle.CreateHeaderStyle(workbook);
                var dataStyle = excelStyle.CreateDataStyle(workbook);
                var numberStyle = excelStyle.CreateNumberStyle(workbook);

                CreateHeaderRow(worksheet, headerStyle);

                for (int i = 0; i < equipmentData.Count; i++)
                {
                    CreateDataRow(worksheet, equipmentData[i], i + 1, dataStyle, numberStyle);
                }

                for (int i = 0; i < 16; i++) worksheet.AutoSizeColumn(i); // FIX HARD-CODED VALUE

                using (FileStream file = new FileStream(saveDialog.FileName, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(file);
                }

                return saveDialog.FileName;
            }

            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to export to Excel: {ex.Message}", "Export Error");
                return null;
            }
        }


        private void CreateHeaderRow(ISheet worksheet, ICellStyle headerStyle)
        {
            var headerRow = worksheet.CreateRow(0);
            string[] headers = {
                "Mark", "Description", "Space Name", "Space Number", "Voltage (V)",
                "Apparent Load Phase 1 (VA)", "Apparent Load Phase 2 (VA)", "Total Apparent Load (VA)", "Total Amps (A)",
                "MCA (A)", "Power Factor", "HP", "Breaker Size", "Wire Size", "Panel Name", "Circuit Number"
            }; // FIX HARD CODING

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }
        }

        private void CreateDataRow(ISheet worksheet, MechEquipData data, int rowIndex, ICellStyle dataStyle, ICellStyle numberStyle)
        {
            var row = worksheet.CreateRow(rowIndex);

            // Text columns
            CreateCell(row, 0, data.Mark ?? "", dataStyle);
            CreateCell(row, 1, data.Description ?? "", dataStyle);
            CreateCell(row, 2, data.SpaceName ?? "", dataStyle);
            CreateCell(row, 3, data.SpaceNumber ?? "", dataStyle);

            // Numeric columns
            CreateCell(row, 4, data.Voltage, numberStyle);
            CreateCell(row, 5, data.ApparentLoadPhase1, numberStyle);
            CreateCell(row, 6, data.ApparentLoadPhase2, numberStyle);
            CreateCell(row, 7, data.TotalApparentLoad, numberStyle);
            CreateCell(row, 8, data.PowerFactor, numberStyle);
            CreateCell(row, 9, data.HP, numberStyle);
            CreateCell(row, 10, data.MCA, numberStyle);

            // Calculated fields (empty for now)
            CreateCell(row, 11, data.BreakerSize ?? "", dataStyle);
            CreateCell(row, 12, data.WireSize ?? "", dataStyle);
            CreateCell(row, 13, data.PanelName ?? "", dataStyle);
            CreateCell(row, 14, data.CircuitNumber ?? "", dataStyle);
        }

        private void CreateCell(IRow row, int column, string value, ICellStyle style)
        {
            var cell = row.CreateCell(column);
            cell.SetCellValue(value);
            cell.CellStyle = style;
        }

        private void CreateCell(IRow row, int column, double value, ICellStyle style)
        {
            var cell = row.CreateCell(column);
            if (value != 0)
            {
                cell.SetCellValue(value);
            }
            cell.CellStyle = style;
        }


    }
}
