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
						string idStr = (string)datalist.Rows[i].ItemArray[0];                           //Application Id?
						string includeSenateBill272Report = (string)datalist.Rows[i].ItemArray[1];      //Must be True or False
						string dataCollectionFreq = (string)datalist.Rows[i].ItemArray[2];              //There are a few Valid options
						string otherDataCollectionFreq = (string)datalist.Rows[i].ItemArray[3];         //Can be anything? is used it the Freq is set to other
						string dataUpdateFreq = (string)datalist.Rows[i].ItemArray[4];                  //There are a few Valid options
                        string otherDataUpdateFreq = (string)datalist.Rows[i].ItemArray[5];             //Can be anything? is used it the Freq is set to other
                        string dataClassificationSoR = (string)datalist.Rows[i].ItemArray[6];           //Must be True or False
                        string dataClassificationDesc = (string)datalist.Rows[i].ItemArray[7];          


                        string error = "";

						bool senateBillBool;
						if (bool.TryParse(includeSenateBill272Report, out senateBillBool))
						{ 
                        }
						else
						{
							error += "Include Senate Bill 272 Report must be 'True' or 'False'. ";
						}
                        
                        bool dataClassificationSoRBool;
                        if (bool.TryParse(dataClassificationSoR, out dataClassificationSoRBool))
                        {

                        }
                        else if (!bool.TryParse(dataClassificationSoR, out dataClassificationSoRBool) || dataClassificationSoR.Length==0)
                        {
                            dataClassificationSoRBool = false;

                        }
                        else {
                            error += "Data Classification System of Record must be 'True' or 'False' or NULL. ";
                        }

                        //Check if Senate Bill is True. If it is, then The data Classification SoR has to be true as well.
                        if (senateBillBool == true && !dataClassificationSoRBool)
                        {
                            error += "If SB272 is True, Data Classification System of Record must be true; ";
                        }
                        //if Senate Bill is True, Data Classifcation System is True, but the Description is Empty, add error
                        else if (senateBillBool == true && String.IsNullOrWhiteSpace(dataClassificationDesc))
                        {
                            error += "Data Classification Description is required; ";
                        }

                        //Check if data classification SoR is True while senate bill is false. If so, update changes to application table but skip changes to Data Classification Table and ward user
                        bool intentionalDataClassificationSkip = false;
                        if(dataClassificationSoRBool && !senateBillBool)
                        {
                            intentionalDataClassificationSkip = true;
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
                            //Checks if we're intentionally skipping update to Data Classification and inform user
                            if (intentionalDataClassificationSkip)
                            {
                                error += "Data Classification System of Record is True, but Include in Senate Bill is False. Application Table Updated. Skipped Changes to Data Classification Table.";
                                errorList.Add("Row " + rowNumber + ": " + error);
                            }

                            Application dbEntity = null;
							// Add or Update
							if (id > 0)
							{

                                //Makes changes to existing entries?
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
                                    //Both data classification SoR and SenateBill must be true to update the Data Classification Table
                                    if (dataClassificationSoRBool&&senateBillBool)
                                    {
                                        SetDataClassificationForApplication(id, dataClassificationSoRBool, dataClassificationDesc);
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

        //Modified Method from Data Classification Service
        private static void SetDataClassificationForApplication(int applicationId, bool SystemOfRecord, string Description)
        {
            var dataClassifications = GetDataClassification(applicationId, ApplicationTableName);
            DataClassification tmpClassification;
            if (dataClassifications.Count() > 0)
            {
                tmpClassification = dataClassifications.FirstOrDefault();
                tmpClassification.DataClassificationId = tmpClassification.Id;
            }
            else
            {
                tmpClassification = new DataClassification();
                tmpClassification.TableId = applicationId;
                tmpClassification.Table = ApplicationTableName;
            }
            tmpClassification.SystemOfRecord = SystemOfRecord;
            tmpClassification.Description = Description;
            UpdateDataClassification(tmpClassification);
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