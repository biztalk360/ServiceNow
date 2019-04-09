using B360.Notifier.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace B360.Notifier.ServiceNowNotification
{
    public class ServiceNowChannel : IChannelNotification
    {
        public string GetGlobalPropertiesSchema()
        {
            return Helper.GetResourceFileContent("GlobalProperties.xml");
        }

        public string GetAlarmPropertiesSchema()
        {
            return Helper.GetResourceFileContent("AlarmProperties.xml");
        }

        public bool SendNotification(BizTalkEnvironment environment, Alarm alarm, string globalProperties, Dictionary<MonitorGroupTypeName, MonitorGroupData> notifications)
        {
            try
            {
                XDocument globalDocument = XDocument.Parse(globalProperties);

                string uriString = globalDocument.XPathSelectElement("/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'TextBox' and @Name = 'servicenow-url']")?.Attribute("Value")?.Value;
                string userName = globalDocument.XPathSelectElement("/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'TextBox' and @Name = 'username']")?.Attribute("Value")?.Value;
                string password = globalDocument.XPathSelectElement("/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'TextBox' and @Name = 'password']")?.Attribute("Value")?.Value;
                bool useProxy = Convert.ToBoolean(globalDocument.XPathSelectElement(
                            "/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'CheckBox' and @Name = 'use-proxy']")
                        ?.Attribute("Value")?.Value);
                bool useDefaultCredentials = Convert.ToBoolean(globalDocument.XPathSelectElement(
                            "/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'Group']/*[local-name() = 'CheckBox' and @Name = 'use-default-credentials']")
                        ?.Attribute("Value")?.Value);

                WebProxy proxy = new WebProxy();
                if (useProxy)
                {
                    string serverName = globalDocument.XPathSelectElement(
                        "/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'Group']/*[local-name() = 'TextBox' and @Name = 'server-name']")
                    ?.Attribute("Value")?.Value;
                    string domainName = globalDocument.XPathSelectElement(
                                "/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'Group']/*[local-name() = 'TextBox' and @Name = 'domain']")
                            ?.Attribute("Value")?.Value;
                    string portName = globalDocument.XPathSelectElement(
                                "/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'Group']/*[local-name() = 'TextBox' and @Name = 'port']")
                            ?.Attribute("Value")?.Value;
                    string proxyUsername = globalDocument.XPathSelectElement(
                                "/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'Group']/*[local-name() = 'TextBox' and @Name = 'user-name']")
                            ?.Attribute("Value")?.Value;

                    string proxyPassword = globalDocument.XPathSelectElement(
                                "/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'Group']/*[local-name() = 'TextBox' and @Name = 'proxy-password']")
                            ?.Attribute("Value")?.Value;

                    if (!string.IsNullOrEmpty(serverName) && !string.IsNullOrEmpty(portName))
                    {
                        proxy = new WebProxy(serverName, Convert.ToInt32(portName));
                        if (!string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword) && !string.IsNullOrEmpty(domainName))
                        {
                            proxy.Credentials = new NetworkCredential(proxyUsername, proxyPassword, domainName);
                        }
                    }

                    if (useDefaultCredentials)
                    {
                        proxy.UseDefaultCredentials = true;
                    }
                }

                XDocument alarmProperties = XDocument.Parse(alarm.AlarmProperties);

                string shortDescription = alarmProperties.XPathSelectElement("/*[local-name() = 'AlarmProperties']/*[local-name() = 'TextBox' and @Name = 'short-description']").Attribute("Value").Value;
                string impact = alarmProperties.XPathSelectElement("/*[local-name() = 'AlarmProperties']/*[local-name() = 'TextBox' and @Name = 'impact']").Attribute("Value").Value;
                string urgency = alarmProperties.XPathSelectElement("/*[local-name() = 'AlarmProperties']/*[local-name() = 'TextBox' and @Name = 'urgency']").Attribute("Value").Value;
                string assignmentgroup = alarmProperties.XPathSelectElement("/*[local-name() = 'AlarmProperties']/*[local-name() = 'TextBox' and @Name = 'assignment-group']").Attribute("Value").Value;
                string category = alarmProperties.XPathSelectElement("/*[local-name() = 'AlarmProperties']/*[local-name() = 'TextBox' and @Name = 'category']").Attribute("Value").Value;
                string subcategory = alarmProperties.XPathSelectElement("/*[local-name() = 'AlarmProperties']/*[local-name() = 'TextBox' and @Name = 'subcategory']").Attribute("Value").Value;
                string configItem = alarmProperties.XPathSelectElement("/*[local-name() = 'AlarmProperties']/*[local-name() = 'TextBox' and @Name = 'configuration-item']").Attribute("Value").Value;
                string additionalcomments = alarmProperties.XPathSelectElement("/*[local-name() = 'AlarmProperties']/*[local-name() = 'TextBox' and @Name = 'additional-comments']").Attribute("Value").Value;

                Dictionary<string, string> dictionary = new Dictionary<string, string>
                {
                    { "short_description", shortDescription },
                    { "impact", impact },
                    { "urgency", urgency },
                    { "assignment_group", assignmentgroup },
                    { "category", category },
                    { "subcategory", subcategory },
                    { "cmdb_ci", configItem },
                    { "comments", additionalcomments }
                };

                string incidentMessage = string.Empty + string.Format($"\nAlarm Name: {alarm.Name} \n\nAlarm Desc: {alarm.Description} \n" + "\n----------------------------------------------------------------------------------------------------\n" + $"\nEnvironment Name: {environment.Name} \n\nMgmt Sql Instance Name: { Regex.Escape(environment.MgmtSqlInstanceName)} \nMgmt Sql Db Name: {environment.MgmtSqlDbName}\n" + "\n----------------------------------------------------------------------------------------------------\n");
                foreach (KeyValuePair<MonitorGroupTypeName, MonitorGroupData> keyValuePair in notifications.OrderBy<KeyValuePair<MonitorGroupTypeName, MonitorGroupData>, MonitorGroupTypeName>(n => n.Key))
                {
                    string monitorGroupType = keyValuePair.Key.ToString();
                    LoggingHelper.Debug($"Populate the monitor Status{monitorGroupType}");
                    foreach (MonitorGroup monitorGroup in keyValuePair.Value.monitorGroups)
                    {
                        incidentMessage += string.Format("{0} {1}\n", monitorGroupType, monitorGroup.name);
                        if (monitorGroup.monitors != null)
                        {
                            foreach (Monitor monitor in monitorGroup.monitors)
                            {
                                incidentMessage += string.Format(" - {0} {1}\n", monitor.name, monitor.monitorStatus);
                            }
                        }
                    }
                    foreach (MonitorGroup monitorGroup in keyValuePair.Value.monitorGroups)
                    {
                        if (monitorGroup.monitors != null)
                        {
                            foreach (Monitor monitor in monitorGroup.monitors)
                            {
                                if (monitor.issues != null)
                                {
                                    incidentMessage += string.Format("\n{0} Issues for {1}\n", monitorGroupType, monitor.name);

                                    foreach (Issue issue in monitor.issues)
                                    {
                                        incidentMessage += string.Format(" - {0}\n", issue.description);

                                        if (issue.monitoringErrorDescriptions != null)
                                        {
                                            foreach (MonitorErrorDescription monitorErrorDescription in issue.monitoringErrorDescriptions)
                                            {
                                                incidentMessage += $" {monitorErrorDescription.key} ({monitorErrorDescription.count}) -> {monitorErrorDescription.value} \n";
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(issue.optionalDetails))
                                        {
                                            incidentMessage += string.Format("{0}\n", issue.optionalDetails);
                                        }



                                    }
                                }
                            }
                        }
                    }
                    incidentMessage += "\n";
                }

                dictionary.Add("work_notes", incidentMessage);
                LoggingHelper.Debug($"Successfully added to the Incident Object, Message: {incidentMessage}");

                string content = JsonConvert.SerializeObject((object)dictionary);

                HttpClientHandler handler = new HttpClientHandler()
                {
                    Proxy = proxy,
                    UseProxy = useProxy
                };

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.BaseAddress = new Uri(uriString);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage result = httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/now/table/incident")
                    {
                        Headers =
                        {
                            Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password))))
                        },
                        Content = new StringContent(content, Encoding.UTF8, "application/json")
                    }).Result;
                    if (result.IsSuccessStatusCode)
                    {
                        LoggingHelper.Info("ServiceNow Incident Creation was Successful");
                    }
                    else
                    {
                        LoggingHelper.Error(string.Format("ServiceNow Incident Creation was Unsuccessful. \n Response: {0}", result));
                    }

                    return result.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.Fatal(ex.ToString());
                LoggingHelper.Fatal(ex.StackTrace);
                LoggingHelper.Error(ex.Message);
                return false;
            }
        }      
    }
}
