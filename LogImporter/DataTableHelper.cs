using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.VisualBasic.FileIO;

namespace LogImporter
{
    public class DataTableHelper
    {
        public static DataTable GetDataTableFromCSVFile(byte[] fileBytes)
        {
            DataTable csvData = new DataTable();

            MemoryStream stream = new MemoryStream(fileBytes);

            using (TextFieldParser csvReader = new TextFieldParser(stream))
            {
                csvReader.SetDelimiters(new string[] { "," });
                csvReader.HasFieldsEnclosedInQuotes = true;
                string[] colFields = csvReader.ReadFields();
                foreach (string column in colFields)
                {
                    DataColumn datecolumn = new DataColumn(column);
                    datecolumn.AllowDBNull = true;
                    csvData.Columns.Add(datecolumn);
                }

                while (!csvReader.EndOfData)
                {
                    string[] fieldData = csvReader.ReadFields();

                    if (fieldData == null)
                        continue;

                    bool rowHasData = false;
                    DataRow newRow = csvData.NewRow();
                    //Making empty value as null
                    for (int i = 0; i < fieldData.Length; i++)
                    {
                        newRow[i] = fieldData[i];

                        if (!rowHasData && !string.IsNullOrWhiteSpace(fieldData[i]))
                        {
                            rowHasData = true;
                        }
                    }
                    if (rowHasData)
                    {
                        csvData.Rows.Add(newRow);
                    }
                }
            }
            return csvData;
        }
    }
}