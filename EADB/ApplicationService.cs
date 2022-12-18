using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DataService
{
    public class ApplicationService : CommonService
    {
		public static void UploadSenateBill272FromList(DataTable datalist, List<string> errorList, List<string> successList, List<SelectListItem> FrequencyList)
		{
			int rowCount = datalist.Rows.Count;
			if (errorList == null)
				errorList = new List<string>();

			using (var db = new AppsEADBEntities())
			{
				for (int i = 0; i < rowCount; i++)
				{
					int rowNumber = i + 1;
					try
					{
						string idStr = (string)datalist.Rows[i].ItemArray[0];
						string includeSenateBill272Report = (string)datalist.Rows[i].ItemArray[1];
						string dataCollectionFreq = (string)datalist.Rows[i].ItemArray[2];
						string otherDataCollectionFreq = (string)datalist.Rows[i].ItemArray[3];
						string dataUpdateFreq = (string)datalist.Rows[i].ItemArray[4];
						string otherDataUpdateFreq = (string)datalist.Rows[i].ItemArray[5];

						string error = "";

						bool senateBillBool;
						if (bool.TryParse(includeSenateBill272Report, out senateBillBool))
						{ }
						else
						{
							error += "Include Senate Bill 272 Report must be 'True' or 'False'. ";
						}

						int id;
						if (Int32.TryParse(idStr, out id))
						{
							error += ValidateSenateBillEntry(
								senateBillBool,
								dataCollectionFreq,
								dataUpdateFreq,
								FrequencyList);
						}
						else
						{
							error += "Id is not valid. ";
						}

						if (!String.IsNullOrEmpty(error))
						{
							errorList.Add("Row " + rowNumber + ": " + error);
						}
						else
						{
							Application dbEntity = null;
							// Add or Update
							if (id > 0)
							{
								dbEntity = db.Applications.Where(x => x.Id == id).FirstOrDefault();
								if (null != dbEntity)
								{
									dbEntity.ReportSenateBill272 = senateBillBool;
									if(dataCollectionFreq.Trim().ToLower() == "other")
									{
										if (!String.IsNullOrWhiteSpace(otherDataCollectionFreq))
										{
											dbEntity.DataCollectionFrequency = "Other:" + otherDataCollectionFreq;
										}
									}
									else
									{
										dbEntity.DataCollectionFrequency = dataCollectionFreq;
									}
									if (dataUpdateFreq.Trim().ToLower() == "other")
									{
										if (!String.IsNullOrWhiteSpace(otherDataUpdateFreq))
										{
											dbEntity.DataUpdateFrequency = "Other:" + otherDataUpdateFreq;
										}
									}
									else
									{
										dbEntity.DataUpdateFrequency = dataUpdateFreq;
									}

									successList.Add("Row " + rowNumber + ": Updated");
								}
								else
								{
									errorList.Add("Row " + rowNumber + " Error: Skipped because Id could not be found.");
								}
							}
							else
							{
								errorList.Add("Row " + rowNumber + " Error: Skipped because Id is not valid.");
							}
						}
					}
					catch (Exception ex)
					{
						errorList.Add("Row " + rowNumber + " Error: Skipped because " + ex.Message);
					}
				}//end loop
				db.SaveChanges();
			}
		}

		public static string ValidateSenateBillEntry(bool senateBillBool, string dataCollectionFreq, string dataUpdateFreq, List<SelectListItem> FrequencyList)
		{
			string error = "";
			List<string> validFrequency = new List<string>();
			for(int i = 0; i < FrequencyList.Count; i++)
			{
				validFrequency.Add(FrequencyList[i].Text.ToLower());
			}

			if (senateBillBool == true && String.IsNullOrWhiteSpace(dataCollectionFreq))
			{
				error = "Data Collection Frequency is required. ";
			}
			else if(senateBillBool == true && !validFrequency.Contains(dataCollectionFreq.ToLower().Trim()))
			{
				error = "Data Collection Frequency is not valid. ";
			}

			if (senateBillBool == true && String.IsNullOrWhiteSpace(dataUpdateFreq))
			{
				error = "Data Update Frequency is required. ";
			}
			else if (senateBillBool == true && !validFrequency.Contains(dataUpdateFreq.ToLower().Trim()))
			{
				error = "Data Update Frequency is not valid. ";
			}

			return error;
		}


        public static string GetHeaderInStringFormat()
        {
            return string.Join(",", GetHeaderInArrayFormat());
        }
    }
}