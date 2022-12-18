using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using System.IO;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Security;
using System.Data.Entity.Core.Objects;
using System.Net;

namespace Console
{
    public class Program
    {
        static Log log = LogManager.GetLogger("");
        static string o365username;
        static string o365password;
        static void Main(string[] args)
        {
            System.Console.WriteLine("Start console Program");
            log.Debug("Start console program");
            //To upload to o365, provide username and password.
            o365username = args[0];
            o365password = args[1];

            //move the documents from staging to destination.
            processDocuments();
            log.Debug("End console program");
            System.Console.WriteLine("End console Program");
        }
        static void processDocuments()
        {
            try
            {
                DateTime filterStartDate = DateTime.Today.AddDays(-90);
                using (var readContext = new AppsDocuSignEEntities())
                {
                    string connection = readContext.Database.Connection.ConnectionString;
                    //Get all envelopes with Submitted status or Not Configured Status.
                    var envelopeInfos = readContext.EnvelopeMainInformation.Where(x => x.CreatedDate >= filterStartDate && (x.ProcessedStatus == ConnectConstants.CONNECT_INITIAL_STATUS || x.ProcessedStatus == ConnectConstants.CONNECT_NOTCONFIGURED_STATUS || x.ProcessedStatus == ConnectConstants.CONNECT_FAILED_STATUS)).Select(y => y.ID).ToList();
                    foreach (var envelopeInfoID in envelopeInfos)
                    {
                        var envelopeInfo = readContext.EnvelopeMainInformation.Where(x => x.ID == envelopeInfoID).SingleOrDefault();
                        readContext.Entry(envelopeInfo).State = EntityState.Detached;
                        using (var updateContext = new AppsDocuSignEEntities())
                        {
                            updateContext.Entry(envelopeInfo).State = EntityState.Modified;
                            var Status = SaveEnvelopeDetails(envelopeInfo);
                            envelopeInfo.ProcessedStatus = Status;
                            envelopeInfo.ModifiedBy = ConnectConstants.SCHEDULER_USERNAME;
                            envelopeInfo.ModifiedDate = DateTime.Now;
                            updateContext.SaveChanges();
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                log.Error("Exception in processDocuments :" + ex.Message + ex.StackTrace);
                //System.Console.WriteLine("Exception in processDocuments: " + ex.Message + ex.StackTrace);
            }
        }

        static String SaveEnvelopeDetails(EnvelopeMainInformation envelopeInfo)
        {
            String processedStatus = ConnectConstants.CONNECT_FAILED_STATUS;
            bool IsConfigured = false;

            var EmailSubject = envelopeInfo.Subject;
            using (var context = new AppsDocuSignIEntities())
            {
                var signedDate = envelopeInfo.Signed;
                var deliveredDate = envelopeInfo.Delivered;
                var SenderEmail = envelopeInfo.SenderEmail;
                IQueryable<EnvelopeDataMapping> envelopeDataMapping = null;
                var envelopeID = envelopeInfo.EnvelopeID;
                //Original
                envelopeDataMapping = context.EnvelopeDataMapping.Where(x => x.StartDate <= signedDate && signedDate <= x.EndDate && EmailSubject.Contains(x.Subject) || x.StartDate <= deliveredDate && deliveredDate <= x.EndDate && EmailSubject.Contains(x.Subject));

                foreach (var dataMapping in envelopeDataMapping)
                {
                    bool ignoreEmail = false;
                    if (!string.IsNullOrEmpty(dataMapping.SenderEmail) && dataMapping.SenderEmail.ToUpper().Equals("ANY"))
                    {
                        ignoreEmail = true;
                    }

                    if (ignoreEmail)
                    {
                        IsConfigured = true;
                    }
                    else
                    {
                        IsConfigured = dataMapping.SenderEmail.Equals(SenderEmail) ? true : false;
                    }
                    //if (EmailSubject.Contains(dataMapping.Subject))

                    if (IsConfigured)
                    {
                        string DestinationType = dataMapping.DestinationType;
                        switch (DestinationType)
                        {
                            case ConnectConstants.DESTTYPE_SHAREPOINT:
                                {
                                    var repositoryinfo = dataMapping.SharePointRepositoryInfo.FirstOrDefault();

                                    
                                        processedStatus = UploadToSharePoint(envelopeInfo, dataMapping);

                                    return processedStatus;
                                }
                            case ConnectConstants.DESTTYPE_SQLSERVER:
                                {
                                    processedStatus = UploadToSQLServer(envelopeInfo, dataMapping);
                                    return processedStatus;
                                }
                            default:
                                break;
                        }
                    }
                }
                if (!IsConfigured)
                {
                    log.Debug("Not configured to process the data:", EmailSubject);
                    processedStatus = ConnectConstants.CONNECT_NOTCONFIGURED_STATUS;
                }
            }

        return processedStatus;
        }
        static string UploadToSharePoint(EnvelopeMainInformation envelopeInfo, EnvelopeDataMapping envelopeDataMapping)
        {
            String status = ConnectConstants.CONNECT_FAILED_STATUS;
            var repositoryinfo = envelopeDataMapping.SharePointRepositoryInfo.FirstOrDefault();
            if (!string.IsNullOrEmpty(accessToken))
            {
                if (repositoryinfo.IsSharePointList.Equals("Yes"))
                {
                    status = UploadToSharePointList(envelopeInfo, envelopeDataMapping, accessToken);
                }
                else
                {
                    status = UploadToSharePointDL(envelopeInfo, envelopeDataMapping, accessToken);
                }
                return status;
            }
            else
            {
            }
        }
        static void UploadToSharePointList(EnvelopeMainInformation envelopeInfo, EnvelopeDataMapping envelopeDataMapping, string accessToken = null)

        {
          
        }
        static void UploadToSharePointDL(EnvelopeMainInformation envelopeInfo, EnvelopeDataMapping envelopeDataMapping, string accessToken = null)
        {
            
        }

        static void UploadToSQLServer(EnvelopeMainInformation envelopeInfo, EnvelopeDataMapping envelopeDataMapping)
        {


        }


        public static DayOfWeek ToDayOfWeek(string str)
        {
            return (DayOfWeek)Enum.Parse(typeof(DayOfWeek), str);
        }

    }



}