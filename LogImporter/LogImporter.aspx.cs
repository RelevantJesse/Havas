using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace LogImporter
{
    public partial class LogImporter : System.Web.UI.Page
    {
        private DataTable _mediaDetails;
        private DataTable MediaDetails
        {
            get
            {
                if (_mediaDetails == null)
                {
                    _mediaDetails = (DataTable)ViewState["MediaDetailsForLogs"];
                }
                return _mediaDetails;
            }
            set
            {
                ViewState["MediaDetailsForLogs"] = value;
                _mediaDetails = value;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                SetOriginalData();
                PopulateAutoComplete();
            }
        }

        private void PopulateAutoComplete()
        {
            List<string> tempString = new List<string>();
            tempString.Add("Hello");
            tempString.Add("World");

            StringBuilder sb = new StringBuilder();
            sb.Append("<script>");
            sb.Append("var stations = [");
            foreach (string str in tempString)
            {
                sb.Append("testArray.push('" + str + "');");
            }
            sb.Append("</script>");

            ClientScript.RegisterStartupScript(this.GetType(), "TestArrayScript", sb.ToString());
        }

        private void SetOriginalData()
        {
            MediaDetails = Database.GetAllMediaOrderDetail();
            
            grdMediaDetails.DataSource = MediaDetails;
            grdMediaDetails.DataBind();
            grdUnmatched.DataSource = null;
            grdUnmatched.DataBind();
        }

        protected void btnImport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fuCsvPath.FileName))
            {
                //Show error
                return;
            }

            DataTable fileData = DataTableHelper.GetDataTableFromCSVFile(fuCsvPath.FileBytes);
            DataTable gridData = MediaDetails;
            DataColumn matchedColumn;

            if (!gridData.Columns.Contains("Matched"))
            {
                matchedColumn = new DataColumn("Matched", typeof (bool));
                matchedColumn.DefaultValue = false;
                gridData.Columns.Add(matchedColumn);
            }

            matchedColumn = new DataColumn("Matched", typeof(bool));
            matchedColumn.DefaultValue = false;
            fileData.Columns.Add(matchedColumn);

            matchedColumn = new DataColumn("PotentialMatch", typeof(bool));
            matchedColumn.DefaultValue = false;
            fileData.Columns.Add(matchedColumn);

            DataView dv = gridData.DefaultView;
            dv.Sort = "AIRDATE desc, AIRTIME desc";
            DataTable sortedGridData = dv.ToTable();

            dv = fileData.DefaultView;
            dv.Sort = "AIRDATE desc, AIRTIME desc";
            DataTable sortedFileData = dv.ToTable();

            FindMatches(sortedGridData, sortedFileData);

            grdUnmatched.DataSource = sortedFileData.Select("Matched = false").CopyToDataTable();
            grdUnmatched.DataBind();
        }

        private void FindMatches(DataTable sortedGridData, DataTable sortedFileData)
        {
            for (int gridIndex = 0; gridIndex < sortedGridData.Rows.Count; gridIndex++)
            {
                DataRow gridRow = sortedGridData.Rows[gridIndex];
                
                if (rbPreLog.Checked && (gridRow["EType"].ToString() == "Post Log" ||
                                         gridRow["EType"].ToString() == "Monitored"))
                {
                    gridRow["matched"] = true;

                    continue;
                }

                for (int fileIndex = 0; fileIndex < sortedFileData.Rows.Count; fileIndex++)
                {
                    DataRow fileRow = sortedFileData.Rows[fileIndex];

                    if (Convert.ToBoolean(fileRow["matched"]))
                    {
                        continue;
                    }

                    decimal fileRate;
                    if (!decimal.TryParse(fileRow["rate"].ToString(), out fileRate))
                    {
                        //TODO: Error handling
                    }

                    if (fileRow["station"].ToString().ToUpper() != gridRow["station"].ToString().ToUpper() ||
                        fileRow["client"].ToString().ToUpper() != gridRow["client"].ToString().ToUpper() ||
                        !gridRow["isci"].ToString().ToUpper().Contains(fileRow["isci"].ToString().ToUpper()) ||
                        fileRate != Convert.ToDecimal(gridRow["rate"].ToString()))
                    {
                        continue;
                    }

                    DateTime gridRowTrueAirTime;
                    DateTime.TryParse(gridRow["airtime"].ToString(), out gridRowTrueAirTime);

                    DateTime fileRowAirDate;
                    if (!DateTime.TryParse(fileRow["airdate"].ToString(), out fileRowAirDate))
                    {
                        continue;
                    }

                    DateTime fileRowAirTime;
                    if (!DateTime.TryParse("1/1/1900 " + fileRow["airtime"], out fileRowAirTime))
                    {
                        continue;
                    }

                    if (gridRowTrueAirTime == DateTime.MinValue)
                    {
                        // Match it
                        LinkRows(fileRow, gridRow, fileRowAirDate, fileRowAirTime);
                        break;
                    }

                    DateTime gridRowAirDate;
                    if (!DateTime.TryParse(gridRow["airdate"].ToString(), out gridRowAirDate))
                    {
                        continue;
                    }

                    // Check for matching Station, Client, ISCI, Rate
                    if ((fileRowAirDate - gridRowAirDate).Days != 0)
                    {
                        continue;
                    }
                    
                    DateTime gridRowAirTime;
                    if (!DateTime.TryParse("1/1/1900 " + Convert.ToDateTime(gridRow["airtime"]).ToShortTimeString(), out gridRowAirTime))
                    {
                        continue;
                    }

                    if (Math.Abs((gridRowAirTime - fileRowAirTime).Minutes) > 5)
                    {
                        if (fileIndex != 0 && Convert.ToBoolean(sortedFileData.Rows[fileIndex - 1]["PotentialMatch"]))
                        {
                            // PotentialMatch is the match!
                            DateTime potentialMatchAirDate;
                            if (!DateTime.TryParse(sortedFileData.Rows[fileIndex - 1]["airdate"].ToString(), out potentialMatchAirDate))
                            {
                                continue;
                            }

                            DateTime potentialMatchAirTime;
                            if (!DateTime.TryParse("1/1/1900 " + sortedFileData.Rows[fileIndex - 1]["airtime"], out potentialMatchAirTime))
                            {
                                continue;
                            }

                            LinkRows(sortedFileData.Rows[fileIndex - 1], gridRow, potentialMatchAirDate, potentialMatchAirTime);
                            sortedFileData.Rows[fileIndex - 1]["PotentialMatch"] = false;
                            break;
                        }
                        continue;
                    }

                    if (fileIndex == 0 || !Convert.ToBoolean(sortedFileData.Rows[fileIndex - 1]["PotentialMatch"]))
                    {
                        fileRow["PotentialMatch"] = true;
                    }
                    else
                    {
                        if (FoundWhichRowWasTheMatch(sortedFileData.Rows[fileIndex - 1], fileRow, gridRow,
                            fileRowAirTime, gridRowAirTime))
                        {
                            break;
                        }
                    }
                }
            }

            // Set last grid row
            if (Convert.ToBoolean(sortedFileData.Rows[sortedFileData.Rows.Count - 1]["PotentialMatch"]))
            {
                DateTime potentialMatchAirDate;
                if (!DateTime.TryParse(sortedFileData.Rows[sortedFileData.Rows.Count - 1]["airdate"].ToString(), out potentialMatchAirDate))
                {
                    //Do something
                }

                DateTime potentialMatchAirTime;
                if (!DateTime.TryParse("1/1/1900 " + sortedFileData.Rows[sortedFileData.Rows.Count - 1]["airtime"], out potentialMatchAirTime))
                {
                    //Do something
                }

                LinkRows(sortedFileData.Rows[sortedFileData.Rows.Count - 1], 
                         sortedGridData.Rows[sortedGridData.Rows.Count - 1], 
                         potentialMatchAirDate, 
                         potentialMatchAirTime);
            }

            MediaDetails = sortedGridData;
            grdMediaDetails.DataSource = MediaDetails;
            grdMediaDetails.DataBind();
        }

        private void LinkRows(DataRow fileRow, DataRow gridRow, DateTime airDate, DateTime airTime)
        {
            gridRow["airdate"] = airDate;
            gridRow["airtime"] = airTime;
            gridRow["EType"] = rbPreLog.Checked ? "Pre Log" : "Post Log";
            fileRow["matched"] = true;
            gridRow["matched"] = true;
        }

        private bool FoundWhichRowWasTheMatch(DataRow potentialRow, DataRow fileRow, DataRow gridRow, DateTime fileRowAirTime, DateTime gridRowAirTime)
        {
            DateTime potentialMatchAirTime;
            if (!DateTime.TryParse("1/1/1900 " + potentialRow["airtime"], out potentialMatchAirTime))
            {
                return false;
            }

            int potentialMatchTimeDiff = Math.Abs((potentialMatchAirTime - gridRowAirTime).Minutes);
            int fileRowTimeDiff = Math.Abs((fileRowAirTime - gridRowAirTime).Minutes);

            if (potentialMatchTimeDiff <= fileRowTimeDiff)
            {
                // PotentialMatch is the match!
                DateTime potentialMatchAirDate;
                if (!DateTime.TryParse(potentialRow["airdate"].ToString(), out potentialMatchAirDate))
                {
                    return false;
                }

                LinkRows(potentialRow, gridRow, potentialMatchAirDate, potentialMatchAirTime);
                potentialRow["PotentialMatch"] = false;
                return true;
            }

            // File Row is the match! 
            DateTime fileRowAirDate;
            if (!DateTime.TryParse(fileRow["airdate"].ToString(), out fileRowAirDate))
            {
                return false;
            }

            LinkRows(fileRow, gridRow, fileRowAirDate, fileRowAirTime);
            potentialRow["PotentialMatch"] = false;
            return true;

        }
        
        protected void btnReset_Click(object sender, EventArgs e)
        {
            SetOriginalData();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            DataTable gridData = MediaDetails;

            if (!gridData.Columns.Contains("Matched"))
            {
                return;
            }

            foreach (DataRow row in gridData.Rows)
            {
                if (Convert.ToBoolean(row["Matched"]))
                {
                    Database.UpdateMODFromLogs(Convert.ToInt32(row["modid"]),
                                               Convert.ToDateTime(row["airdate"]),
                                               Convert.ToDateTime(row["airtime"]),
                                               row["EType"].ToString());
                }
            }
        }

        protected void grdMediaDetails_Sorting(object sender, System.Web.UI.WebControls.GridViewSortEventArgs e)
        {
            //grdMediaDetails.DataSource = MediaDetails;
            //grdMediaDetails.DataBind();
        }
    }
}