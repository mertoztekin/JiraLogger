using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mail;

namespace JiraLogger
{
    class Mailer
    {
        public enum MailParamName
        {
            EmailUsername = 0,
            EmailPassword = 1,
            SMTP = 2,
            SMTPPort = 3,
            SenderName = 4,
            SenderMail = 5
        }


        public static String GetConfig(MailParamName param)
        {
            object result = null;
            switch (param)
            {
                case MailParamName.EmailUsername: result = System.Configuration.ConfigurationManager.AppSettings["EmailUserName"]; break;
                case MailParamName.EmailPassword: result = System.Configuration.ConfigurationManager.AppSettings["EmailPassword"]; break;
                case MailParamName.SMTP: result = System.Configuration.ConfigurationManager.AppSettings["EmailSMTP"]; break;
                case MailParamName.SMTPPort: result = System.Configuration.ConfigurationManager.AppSettings["EmailSMTPPort"]; break;
                case MailParamName.SenderName: result = System.Configuration.ConfigurationManager.AppSettings["EmailSenderName"]; break;
                case MailParamName.SenderMail: result = System.Configuration.ConfigurationManager.AppSettings["EmailSenderMail"]; break;
            }
            if (null != result)
                return result.ToString();
            return "";
        }

        public static bool SendEmail(string pTo, string pSubject, string pBody)
        {
            try
            {
                System.Web.Mail.MailMessage myMail = new System.Web.Mail.MailMessage();
                myMail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpserver", GetConfig(MailParamName.SMTP));
                myMail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpserverport", GetConfig(MailParamName.SMTPPort));
                myMail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/sendusing", "2");


                myMail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate", "1");
                //Use 0 for anonymous
                myMail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/sendusername", GetConfig(MailParamName.EmailUsername));
                myMail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/sendpassword", GetConfig(MailParamName.EmailPassword));
                myMail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpusessl", "true");
                myMail.From = GetConfig(MailParamName.SenderMail);
                myMail.To = pTo;
                myMail.Subject = pSubject;
                myMail.BodyFormat = MailFormat.Html;
                myMail.Body = pBody;

                System.Web.Mail.SmtpMail.SmtpServer = "smtp.gmail.com:465";
                System.Web.Mail.SmtpMail.Send(myMail);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
