using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Collections;

namespace JiraLogger
{
    class Jiraci
    {
        public Jiraci()
        {
            Start();
        }
#region Sqls
        String GetLogSQL()
        {
            return 
            @"SELECT cgroup.AUTHOR,cgroup.CREATED, citem.FIELD, citem.OLDSTRING, citem.NEWSTRING,jissue.pkey,jissue.SUMMARY 
FROM [Jira].[dbo].[changegroup] as cgroup with (nolock) 
INNER JOIN [Jira].[dbo].[changeitem] as citem with (nolock) 
on cgroup.id=citem.groupid 
INNER JOIN [Jira].[dbo].[jiraissue] as jissue with (nolock) 
on cgroup.issueid=jissue.ID 
WHERE cgroup.CREATED >= DATEADD(MINUTE, -" + GetSleepMinute() + @", GETDATE()) AND(PROJECT!='10100' AND PROJECT!='10500' AND issuetype !='12') 

AND (FIELD != 'Attachment' AND FIELD != 'WorklogId' AND FIELD != 'timespent' AND FIELD != 'Planned End' AND FIELD != 'resolutiondate' and FIELD != 'timeestimate' AND issuetype != '10')";
        }
#endregion


        public List<Row> GetLogChanges()
        {
            List<Row> rows = new List<Row>();
            using (SqlConnection connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["JIRA"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(GetLogSQL(), connection))
                {
                    connection.Open();
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        
                        while (reader.Read())
                        {
                            Row r = new Row();
                            r.ID = reader["pkey"].ToString();
                            r.Summary = GetStr(reader[6]);
                            r.Tarih = reader.GetDateTime(1);
                            r.Yapan = GetStr(reader[0]);
                            r.Degisiklik = GetStr(reader[2]) + @" : " + GetStr(reader[3]) + " -> " + GetStr(reader[4]);
                            rows.Add(r);
                        }
                    }
                }
            }
            return rows;
        }

        public List<Comment> GetCommentChanges()
        {
            List<Comment> comments = new List<Comment>();
            using (SqlConnection connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["JIRA"].ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(GetCommentSql(), connection))
                {
                    connection.Open();
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Comment cm = new Comment();
                            cm.ID = GetStr(reader["pkey"]);
                            cm.Summary = GetStr(reader["summary"]);
                            cm.Yapan = GetStr(reader["yorum_yapan"]);
                            cm.Yorum = GetStr(reader["yorum"]);
                            cm.Tarih = reader.GetDateTime(4);
                            comments.Add(cm);
                        }
                    }
                }
            }
            return comments;
        }

        public class Comment
        {
            public string ID;
            public String Summary;
            public String Yorum;
            public String Yapan;
            public DateTime Tarih;
            public override string ToString()
            {
                return ID;
            }
        }

        private string GetCommentSql()
        {
            return @"SELECT summary,pkey,[actionbody] as yorum,[UPDATEAUTHOR] as yorum_yapan,[Jira].[dbo].[jiraaction].UPDATED
  FROM [Jira].[dbo].[jiraaction]
  left join [Jira].[dbo].[jiraissue]
  on [Jira].[dbo].[jiraaction].issueid = [Jira].[dbo].[jiraissue].ID
 WHERE DATEDIFF(minute,[Jira].[dbo].[jiraaction].UPDATED,GETDATE())<=" + GetSleepMinute() +" order by [jiraissue].UPDATED asc";
        }

        public class Row
        {
            public string ID;
            public String Summary;
            public DateTime Tarih;
            public String Degisiklik;
            public String Yapan;
            public override string ToString()
            {
                return ID;
            }
        }


        String GetStr(Object obj)
        {
            if (null == obj || obj.Equals(DBNull.Value))
                return "";
            return obj.ToString();
        }

        void Start()
        {
                List<Row> logList = GetLogChanges();
                List<Comment> commentList = GetCommentChanges();
                if (logList.Count != 0 || commentList.Count != 0)
                {
                    SendMail(logList, commentList);
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + " Gönderildi");
                }
                else
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + " Gönderilmedi");
                System.Threading.Thread.Sleep(1000 * 5);
        }

        void SendMail(List<Row> changes, List<Comment> comments)
        {
            StringBuilder str = new StringBuilder();
            str.Append(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">
<html>
<head>
<title></title>
</head>
<body>
<table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;"">
<tbody>
<tr valign=""top"">
	<td style=""padding:5px!important"">
		<table border=""0"" cellpadding=""5px"" cellspacing=""0"" width=""100%"" style=""background-color:White;border-style:none"">
			<tbody>
            <tr valign=""top"">
					<td style=""font-size:12px;white-space:nowrap;font-family:Arial,FreeSans,Helvetica,sans-serif;padding:0px;min-width:70px;"">
						<strong>ID</strong>
					</td>
					<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;padding:0 0 10px 0"">
						 <strong>Talep</strong>
					</td>
					<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;padding:0 0 10px 0"">
						 <strong>İşlem Yapan</strong>
					</td>
					<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;padding:0 0 10px 0"">
						 <strong>Tarih</strong>
					</td>
					<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;padding:0 0 10px 0"">
						 <strong>Açıklama</strong>
					</td>
				</tr>");
            string idOnceki = "";

            int tekMiCiftmi = 0;
            foreach (Row s in changes)
            {
                if (!String.IsNullOrEmpty(idOnceki) && idOnceki != s.ID)
                {
                    // değişiklik var
                    List<Comment> comments2Remove = new List<Comment>();
                    foreach (Comment c in comments)
                    {
                        if (c.ID == idOnceki)
                        {
                            str.Append(@"<tr " + GetTekmiCiftmi(tekMiCiftmi) +@"><td colspan=""2"" style=""text-align:right;color:maroon;font-weight:bolder"">Yorum</td>");
                            str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + c.Yapan + @"</td>");
                            str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + c.Tarih.ToShortTimeString() + @"</td>");
                            str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + c.Yorum + @"</td>");
                            str.Append("</tr>");
                            comments2Remove.Add(c);
                        }
                    }

                    foreach (Comment c in comments2Remove)
                        comments.Remove(c);

                    tekMiCiftmi = 1 - tekMiCiftmi;
                }

                
                str.Append(@"<tr valign=""top"" " + GetTekmiCiftmi(tekMiCiftmi) +">");
                if (idOnceki != s.ID)
                {
                    str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"" "+GetRowSpan(s,changes)+@"><a href=""http://ysproject:8080/browse/" + s.ID + @""" >" + s.ID + @"</a></td>");
                    str.Append(@"<td style=""<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"" " + GetRowSpan(s, changes) + ">" + s.Summary + @"</td>");
                    idOnceki = s.ID;
                }
                str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + s.Yapan + @"</td>");
                str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + s.Tarih.ToShortTimeString()+ @"</td>");
                str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + s.Degisiklik + @"</td>");
                str.Append(@"</tr>");

            }

            // Kalan yorumları bas:
            str.Append(@"<tr><td colspan=""5"" style=""color:Maroon;padding:10px"">Yorumlar:</td></tr>");
            foreach (Comment c in comments)
            {
                str.Append(@"<tr " + GetTekmiCiftmi(tekMiCiftmi) +@"><td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;""><a href=""http://ysproject:8080/browse/" + c.ID + @""">" + c.ID + @"</a></td>");
                str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + c.Summary + @"</td>");
                str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + c.Yapan + @"</td>");
                str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + c.Tarih.ToShortTimeString() + @"</td>");
                str.Append(@"<td style=""font-size:12px;font-family:Arial,FreeSans,Helvetica,sans-serif;"">" + c.Yorum.Substring(0,Math.Min(c.Yorum.Length,100)) + @"</td></tr>");
                tekMiCiftmi = 1-tekMiCiftmi;
            }
            str.Append(@"
			</tbody>
		</table>
	</td>
</tr>
<tr valign=""top"">
	<td style=""color:#505050;font-family:Arial,FreeSans,Helvetica,sans-serif;font-size:10px;line-height:14px;padding:0 16px 16px 16px;text-align:center"">
		 Way arkadaş bu mesaj otomatik geliyor, cillop gibi bir yazılım atıyor.
	</td>
</tr>
</tbody>
</table>
</body>
</html>");

            foreach(String email in System.Configuration.ConfigurationManager.AppSettings["EmailReceivers"].Split(';'))
                if(!String.IsNullOrEmpty(email))
                    Mailer.SendEmail(email, "JIRA değişiklikler", str.ToString());
        }

        private string GetRowSpan(Row s, List<Row> changes)
        {
            int rowCount=0;
            foreach (Row r in changes)
                if (r.ID == s.ID)
                    rowCount++;
            if (rowCount > 1)
                return " rowspan=\"" + rowCount + "\"";
            return String.Empty;
        }

        String GetTekmiCiftmi(int i)
        {
            if (i == 0)
                return "style=\"background-color:#F1F1F1\"";
            return String.Empty;
        }

        int GetSleepMinute()
        {
            try
            {
                return int.Parse(System.Configuration.ConfigurationManager.AppSettings["sleepTime"]);
            }
            catch(Exception)
            {
                return 15;
            }
        }
    }
}
