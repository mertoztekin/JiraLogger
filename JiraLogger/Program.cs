using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace JiraLogger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
            {
                new Jiraci();
            }
            catch (Exception ex)
            {
                Mailer.SendEmail("mert.oztekin@yemeksepeti.com", "Jira logger hata", ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}
