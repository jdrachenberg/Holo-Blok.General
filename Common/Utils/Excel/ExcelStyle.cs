using NPOI.SS.UserModel;
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
    internal class ExcelStyle
    {
        internal ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.IsBold = true;
            style.SetFont(font);
            style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            return style;
        }

        internal ICellStyle CreateDataStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            style.VerticalAlignment = VerticalAlignment.Center;
            return style;
        }

        internal ICellStyle CreateNumberStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            style.VerticalAlignment = VerticalAlignment.Center;
            style.DataFormat = workbook.CreateDataFormat().GetFormat("0");
            return style;
        }

    }
}
