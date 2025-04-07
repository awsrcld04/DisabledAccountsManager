// Event ID's
// 1001 - started
// 1002 - stopped
// 1003 - number of accounts found
// 1004 - number of accounts added to a ToBeDeleted group
// 1005 - number of ToBeDeleted groups found
// 1006 - number of accounts to be deleted in a ToBeDeleted group

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using Microsoft.Win32;
using System.Management;
using System.Reflection;
using System.Diagnostics;

namespace DisabledAccountsManager
{
    class DAMMain
    {
        struct DisabledAccountsParams
        {
            public string strDisabledUsersLocation;
            public string strUseExclusionGroups;
            //public string strSendActionList;
            //public string strActionListSender;
            //public string strActionListRecipient;
            public int intDaysSinceLastLogon;
            public int intDisabledHoldPeriod;
            public string strDisabledAccountsPassword;
            public string strReconfigureEmailAddress;
            public string strDisabledEmailAddressPrefix;
            //public string strReconfigureDisplayName;
            //public string strDisabledDisplayNamePrefix;
            public string strHideFromGlobalAddressList;
            public List<string> lstExcludePrefix;
            public List<string> lstExcludeSuffix;
            public List<string> lstExclude;
        }

        struct CMDArguments
        {
            public bool bParseCmdArguments;
        }

        static void funcPrintParameterWarning()
        {
            Console.WriteLine("A parameter is missing or is incorrect.");
            Console.WriteLine("Run DisabledAccountsManager -? to get the parameter syntax.");
        }

        static void funcPrintParameterSyntax()
        {
            Console.WriteLine("DisabledAccountsManager v1.0");
            Console.WriteLine();
            Console.WriteLine("Description: Manage disabled user accounts");
            Console.WriteLine();
            Console.WriteLine("Parameter syntax:");
            Console.WriteLine();
            Console.WriteLine("Use the following required parameters in the following order:");
            Console.WriteLine("-run                     required parameter");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("DisabledAccountsManager -run");
        }

        static CMDArguments funcParseCmdArguments(string[] cmdargs)
        {
            CMDArguments objCMDArguments = new CMDArguments();

            try
            {
                if (cmdargs[0] == "-run" & cmdargs.Length == 1)
                {
                    objCMDArguments.bParseCmdArguments = true;
                }
                else
                {
                    objCMDArguments.bParseCmdArguments = false;
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                objCMDArguments.bParseCmdArguments = false;
            }

            return objCMDArguments;
        }

        static DisabledAccountsParams funcParseConfigFile(CMDArguments objCMDArguments)
        {
            DisabledAccountsParams newParams = new DisabledAccountsParams();

            try
            {
                newParams.lstExclude = new List<string>();
                newParams.lstExcludePrefix = new List<string>();
                newParams.lstExcludeSuffix = new List<string>();

                newParams.intDaysSinceLastLogon = 0; // initialize 
                newParams.intDisabledHoldPeriod = 0; // initialize
                newParams.strDisabledAccountsPassword = "-W3nDr0w$$4P_" + DateTime.Now.ToLocalTime().ToString("MMddyyyy");

                newParams.strUseExclusionGroups = "no";

                // setup automatic exclusions
                newParams.lstExcludePrefix.Add("SYSTEMMAILBOX");

                newParams.lstExclude.Add("ADMINISTRATOR");
                newParams.lstExclude.Add("GUEST");
                newParams.lstExclude.Add("KRBTGT");
                newParams.lstExclude.Add("SUPPORT_388945A0");

                TextReader trConfigFile = new StreamReader("configDisabledAccountsManager.txt");

                using (trConfigFile)
                {
                    string strNewLine = "";

                    while ((strNewLine = trConfigFile.ReadLine()) != null)
                    {

                        if (strNewLine.StartsWith("DisabledUsersLocation=") & strNewLine != "DisabledUsersLocation=")
                        {
                            newParams.strDisabledUsersLocation = strNewLine.Substring(22);
                            //[DebugLine] Console.WriteLine(newParams.strDisabledUsersLocation);
                        }
                        if (strNewLine.StartsWith("UseExclusionGroups=") & strNewLine != "UseExclusionGroups=")
                        {
                            newParams.strUseExclusionGroups = strNewLine.Substring(19);
                            //[DebugLine] Console.WriteLine(newParams.strUseExclusionGroups);
                        }
                        //if (strNewLine.StartsWith("SendActionList="))
                        //{
                        //    newParams.strSendActionList = strNewLine.Substring(15);
                        //    //[DebugLine] Console.WriteLine(newParams.strSendActionList);
                        //}
                        //if (strNewLine.StartsWith("ActionListSender="))
                        //{
                        //    newParams.strActionListSender = strNewLine.Substring(17);
                        //    //[DebugLine] Console.WriteLine(newParams.strActionListSender);
                        //}
                        //if (strNewLine.StartsWith("ActionListRecipient="))
                        //{
                        //    newParams.strActionListRecipient = strNewLine.Substring(20);
                        //    //[DebugLine] Console.WriteLine(newParams.strActionListRecipient);
                        //}
                        if (strNewLine.StartsWith("ExcludePrefix=") & strNewLine != "ExcludePrefix=")
                        {
                            if (!newParams.lstExcludePrefix.Contains(strNewLine.Substring(14).ToUpper()))
                            {
                                newParams.lstExcludePrefix.Add(strNewLine.Substring(14).ToUpper());
                                //[DebugLine] Console.WriteLine(strNewLine.Substring(14));
                            }
                        }
                        if (strNewLine.StartsWith("ExcludeSuffix=") & strNewLine != "ExcludeSuffix=")
                        {
                            if (!newParams.lstExcludeSuffix.Contains(strNewLine.Substring(14).ToUpper()))
                            {
                                newParams.lstExcludePrefix.Add(strNewLine.Substring(14).ToUpper());
                                //[DebugLine] Console.WriteLine(strNewLine.Substring(14));
                            }
                        }
                        if (strNewLine.StartsWith("Exclude=") & strNewLine != "Exclude=")
                        {
                            if (!newParams.lstExclude.Contains(strNewLine.Substring(8).ToUpper()))
                            {
                                newParams.lstExclude.Add(strNewLine.Substring(8).ToUpper());
                                //[DebugLine] Console.WriteLine(strNewLine.Substring(8));
                            }
                        }
                        if (strNewLine.StartsWith("DaysSinceLastLogon=") & strNewLine != "DaysSinceLastLogon=")
                        {
                            newParams.intDaysSinceLastLogon = Int32.Parse(strNewLine.Substring(19));
                            //[DebugLine] Console.WriteLine(strNewLine.Substring(19) + newParams.intDaysSinceLastLogon.ToString());
                        }
                        if (strNewLine.StartsWith("DisabledHoldPeriod=") & strNewLine != "DisabledHoldPeriod=")
                        {
                            newParams.intDisabledHoldPeriod = Int32.Parse(strNewLine.Substring(19));
                            //[DebugLine] Console.WriteLine(strNewLine.Substring(19) + newParams.intDisabledHoldPeriod.ToString());
                        }
                        //if (strNewLine.StartsWith("DisabledAccountsPassword=") & strNewLine != "DisabledAccountsPassword=")
                        //{
                        //    newParams.strDisabledAccountsPassword = strNewLine.Substring(25);
                        //    //[DebugLine] Console.WriteLine(newParams.strDisabledAccountsPassword);
                        //}
                        if (strNewLine.StartsWith("ReconfigureEmailAddress=") & strNewLine != "ReconfigureEmailAddress=")
                        {
                            newParams.strReconfigureEmailAddress = strNewLine.Substring(24);
                            //[DebugLine] Console.WriteLine(newParams.strReconfigureEmailAddress);
                        }
                        if (strNewLine.StartsWith("DisabledEmailAddressPrefix=") & strNewLine != "DisabledEmailAddressPrefix=")
                        {
                            newParams.strDisabledEmailAddressPrefix = strNewLine.Substring(27);
                            //[DebugLine] Console.WriteLine(newParams.strDisabledEmailAddressPrefix);
                        }
                        //if (strNewLine.StartsWith("ReconfigureDisplayName="))
                        //{
                        //    newParams.strReconfigureDisplayName = strNewLine.Substring(23);
                        //    //[DebugLine] Console.WriteLine(newParams.strReconfigureDisplayName);
                        //}
                        //if (strNewLine.StartsWith("DisabledDisplayNamePrefix="))
                        //{
                        //    newParams.strDisabledDisplayNamePrefix = strNewLine.Substring(26);
                        //    //[DebugLine] Console.WriteLine(newParams.strDisabledDisplayNamePrefix);
                        //}
                        if (strNewLine.StartsWith("HideFromGlobalAddressList=") & strNewLine != "HideFromGlobalAddressList=")
                        {
                            newParams.strHideFromGlobalAddressList = strNewLine.Substring(26);
                            //[DebugLine] Console.WriteLine(newParams.strHideFromGlobalAddressList);
                        }

                    }
                }

                //[DebugLine] Console.WriteLine("# of Exclude= : {0}", newParams.lstExclude.Count.ToString());
                //[DebugLine] Console.WriteLine("# of ExcludePrefix= : {0}", newParams.lstExcludePrefix.Count.ToString());

                trConfigFile.Close();

                if (newParams.intDaysSinceLastLogon == 0)
                {
                    newParams.intDaysSinceLastLogon = 21;
                }
                else
                {
                    newParams.intDaysSinceLastLogon = newParams.intDaysSinceLastLogon + 14; // 14 day window for update of lastLogonTimestamp attribute
                }

                if (newParams.intDisabledHoldPeriod == 0)
                {
                    newParams.intDisabledHoldPeriod = 7; // default to 7 days if not specified
                }

            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }

            return newParams;
        }

        static void funcProgramExecution(CMDArguments objCMDArguments2)
        {
            try
            {
                Domain currentDomain = Domain.GetComputerDomain();
                if (currentDomain.DomainMode != DomainMode.Windows2000MixedDomain &
                    currentDomain.DomainMode != DomainMode.Windows2000NativeDomain &
                    currentDomain.DomainMode != DomainMode.Windows2003InterimDomain)
                {

                    // [DebugLine] Console.WriteLine("Entering funcProgramExecution");
                    if (funcCheckForFile("configDisabledAccountsManager.txt"))
                    {
                        funcToEventLog("DisabledAccountsManager", "DisabledAccountsManager started", 100);

                        funcProgramRegistryTag("DisabledAccountsManager");

                        DisabledAccountsParams newParams = funcParseConfigFile(objCMDArguments2);
                        
                        funcModifyDisabledAccounts(newParams);

                        funcRemoveUserAccounts(newParams);

                        funcToEventLog("DisabledAccountsManager", "DisabledAccountsManager stopped", 101);
                    }
                    else
                    {
                        Console.WriteLine("Config file configDisabledAccountsManager.txt could not be found.");
                    }
                }

            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }

        }

        static void funcProgramRegistryTag(string strProgramName)
        {
            try
            {
                string strRegistryProfilesPath = "SOFTWARE";
                RegistryKey objRootKey = Microsoft.Win32.Registry.LocalMachine;
                RegistryKey objSoftwareKey = objRootKey.OpenSubKey(strRegistryProfilesPath, true);
                RegistryKey objSystemsAdminProKey = objSoftwareKey.OpenSubKey("SystemsAdminPro", true);
                if (objSystemsAdminProKey == null)
                {
                    objSystemsAdminProKey = objSoftwareKey.CreateSubKey("SystemsAdminPro");
                }
                if (objSystemsAdminProKey != null)
                {
                    if (objSystemsAdminProKey.GetValue(strProgramName) == null)
                        objSystemsAdminProKey.SetValue(strProgramName, "1", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static DirectorySearcher funcCreateDSSearcher()
        {
            try
            {
                System.DirectoryServices.DirectorySearcher objDSSearcher = new DirectorySearcher();
                // [Comment] Get local domain context

                string rootDSE;

                System.DirectoryServices.DirectorySearcher objrootDSESearcher = new System.DirectoryServices.DirectorySearcher();
                rootDSE = objrootDSESearcher.SearchRoot.Path;
                //Console.WriteLine(rootDSE);

                // [Comment] Construct DirectorySearcher object using rootDSE string
                System.DirectoryServices.DirectoryEntry objrootDSEentry = new System.DirectoryServices.DirectoryEntry(rootDSE);
                objDSSearcher = new System.DirectoryServices.DirectorySearcher(objrootDSEentry);
                //Console.WriteLine(objDSSearcher.SearchRoot.Path);

                return objDSSearcher;
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return null;
            }
        }

        static PrincipalContext funcCreatePrincipalContext()
        {
            PrincipalContext newctx = new PrincipalContext(ContextType.Machine);

            try
            {
                //Console.WriteLine("Entering funcCreatePrincipalContext");
                Domain objDomain = Domain.GetComputerDomain();
                string strDomain = objDomain.Name;
                DirectorySearcher tempDS = funcCreateDSSearcher();
                string strDomainRoot = tempDS.SearchRoot.Path.Substring(7);
                // [DebugLine] Console.WriteLine(strDomainRoot);
                // [DebugLine] Console.WriteLine(strDomainRoot);

                newctx = new PrincipalContext(ContextType.Domain,
                                    strDomain,
                                    strDomainRoot);

                // [DebugLine] Console.WriteLine(newctx.ConnectedServer);
                // [DebugLine] Console.WriteLine(newctx.Container);



                //if (strContextType == "Domain")
                //{

                //    PrincipalContext newctx = new PrincipalContext(ContextType.Domain,
                //                                    strDomain,
                //                                    strDomainRoot);
                //    return newctx;
                //}
                //else
                //{
                //    PrincipalContext newctx = new PrincipalContext(ContextType.Machine);
                //    return newctx;
                //}
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }

            if (newctx.ContextType == ContextType.Machine)
            {
                Exception newex = new Exception("The Active Directory context did not initialize properly.");
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, newex);
            }

            return newctx;
        }

        static bool funcCheckNameExclusion(string strName, DisabledAccountsParams listParams)
        {
            try
            {
                bool bNameExclusionCheck = false;

                // automatic exclusions are setup in funcParseConfigFile

                if (listParams.strUseExclusionGroups == "yes")
                {
                    Domain dmCurrent = Domain.GetCurrentDomain();
                    //[DebugLine] Console.WriteLine(dmCurrent.Name);
                    PrincipalContext ctxDomain = new PrincipalContext(ContextType.Domain, dmCurrent.Name);
                    //[DebugLine] Console.WriteLine(ctxDomain.ConnectedServer + "\t" + ctxDomain.Container + "\t" + ctxDomain.Name);
                    GroupPrincipal grpOWALogonExclusions = GroupPrincipal.FindByIdentity(ctxDomain, IdentityType.SamAccountName, "OWALogonExclusions");
                    GroupPrincipal grpServiceAccountExclusions = GroupPrincipal.FindByIdentity(ctxDomain, IdentityType.SamAccountName, "ServiceAccountExclusions");
                    //[DebugLine] Console.WriteLine(strName);
                    UserPrincipal upTemp = UserPrincipal.FindByIdentity(ctxDomain, IdentityType.SamAccountName, strName);

                    if (grpOWALogonExclusions != null & upTemp != null)
                    {
                        if (upTemp.IsMemberOf(grpOWALogonExclusions))
                            bNameExclusionCheck = true;
                    }
                    if (grpServiceAccountExclusions != null & upTemp != null)
                    {
                        if (upTemp.IsMemberOf(grpServiceAccountExclusions))
                            bNameExclusionCheck = true;
                    }

                }

                strName = strName.ToUpper();
                //[DebugLine] Console.WriteLine(strName);

                if (listParams.lstExclude.Contains(strName))
                    bNameExclusionCheck = true;

                foreach (string strNameTemp in listParams.lstExcludePrefix)
                {
                    if (strName.StartsWith(strNameTemp))
                    {
                        bNameExclusionCheck = true;
                        break;
                    }
                }

                foreach (string strNameTemp in listParams.lstExcludeSuffix)
                {
                    if (strName.EndsWith(strNameTemp))
                    {
                        bNameExclusionCheck = true;
                        break;
                    }
                }

                return bNameExclusionCheck;
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return false;
            }
        }
       
        static void funcModifyDisabledAccounts(DisabledAccountsParams currentParams)
        {
            try
            {
                if (funcCheckForOU(currentParams.strDisabledUsersLocation))
                {
                    Domain thisDomain = Domain.GetComputerDomain();
                    string strDomainName = thisDomain.Name;
                    PrincipalContext ctxDisabledUsers = new PrincipalContext(ContextType.Domain, strDomainName,
                                                                             currentParams.strDisabledUsersLocation);

                    PrincipalContext currentctx = funcCreatePrincipalContext();

                    // Create a PrincipalSearcher object.
                    PrincipalSearcher ps = new PrincipalSearcher();

                    // Create an in-memory user object to use as the query example.
                    UserPrincipal up = new UserPrincipal(currentctx);

                    up.Enabled = false;

                    // Tell the PrincipalSearcher what to search for.
                    ps.QueryFilter = up;

                    PrincipalSearchResult<Principal> psr = ps.FindAll();

                    funcToEventLog("DisabledAccountsManager", "Number of accounts to process: " +
                                   psr.Count<Principal>().ToString(), 1001);

                    if (psr.Count<Principal>() > 0)
                    {
                        TextWriter twCurrent = funcOpenOutputLog();
                        string strOutputMsg = "";

                        funcWriteToOutputLog(twCurrent, "--------DisabledAccountsManager: Processing disabled accounts");

                        DirectoryEntry orgDE = new DirectoryEntry("LDAP://" + currentParams.strDisabledUsersLocation);

                        //[DebugLine] Console.WriteLine(orgDE.Path);
                        funcWriteToOutputLog(twCurrent, orgDE.Path);

                        List<string> lstDisabledUsers = new List<string>();

                        foreach (UserPrincipal u in psr)
                        {
                            if (!funcCheckNameExclusion(u.SamAccountName, currentParams))
                            {
                                if (!u.DistinguishedName.Contains(currentParams.strDisabledUsersLocation))
                                {
                                    lstDisabledUsers.Add(u.Sid.ToString());

                                    //[DebugLine] Console.WriteLine("Account to Disable: {0}", u.Name);
                                    strOutputMsg = "Disabled account: " + u.Name;
                                    funcWriteToOutputLog(twCurrent, strOutputMsg);

                                    //[DebugLine] Console.WriteLine(u.DistinguishedName);
                                    funcWriteToOutputLog(twCurrent, u.DistinguishedName);

                                    DirectoryEntry uDE = new DirectoryEntry("LDAP://" + u.DistinguishedName);
                                    if (uDE != null)
                                    {
                                        //[DebugLine] Console.WriteLine(uDE.Path);
                                        //funcWriteToOutputLog(twCurrent, uDE.Path);
                                        try
                                        {
                                            if (uDE.Properties.Contains("homeMDB"))
                                            {
                                                //[DebugLine] Console.WriteLine("homeMDB is present");
                                                //[DebugLine] funcWriteToOutputLog(twCurrent, "homeMDB is present");
                                                //if (uDE.Properties["homeMDB"].Value.ToString() == "" | uDE.Properties["homeMDB"].Value != null)
                                                //{
                                                //    Console.WriteLine(uDE.Properties["homeMDB"].Value.ToString());
                                                //}
                                                if (currentParams.strHideFromGlobalAddressList == "yes")
                                                {
                                                    uDE.Properties["msExchHideFromAddressLists"].Value = "TRUE";
                                                    uDE.Properties["ShowInAddressBook"].Value = null;
                                                    uDE.CommitChanges();
                                                    //[DebugLine] Console.WriteLine("Account will be hidden from Global Address List");
                                                    funcWriteToOutputLog(twCurrent, "Account will be hidden from Global Address List");
                                                    uDE.Close();
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MethodBase mb1 = MethodBase.GetCurrentMethod();
                                            funcGetFuncCatchCode(mb1.Name, ex);
                                        }
                                    }
                                    if (u.EmailAddress != "" & u.EmailAddress != null)
                                    {
                                        if (currentParams.strReconfigureEmailAddress == "yes")
                                        {
                                            u.EmailAddress = currentParams.strDisabledEmailAddressPrefix + u.EmailAddress;
                                            //[DebugLine] Console.WriteLine("New email address: " + currentParams.strDisabledEmailAddressPrefix + u.EmailAddress);
                                            strOutputMsg = "New email address: " + currentParams.strDisabledEmailAddressPrefix + u.EmailAddress;
                                            funcWriteToOutputLog(twCurrent, strOutputMsg);
                                        }
                                    }

                                    List<string> grplist = new List<string>();
                                    foreach (GroupPrincipal gp in u.GetGroups())
                                    {
                                        //[DebugLine] Console.WriteLine("{0} \t {1}", gp.Name, gp.Sid.ToString());
                                        grplist.Add(gp.Sid.ToString());
                                        //[DebugLine] funcWriteToOutputLog(twCurrent, gp.Name + "\t" + gp.Sid.ToString());
                                    }

                                    strOutputMsg = "Performing password change";
                                    funcWriteToOutputLog(twCurrent, strOutputMsg);

                                    //[DebugLine] Console.WriteLine("New password: " + currentParams.strDisabledAccountsPassword);
                                    //strOutputMsg = "New password: " + currentParams.strDisabledAccountsPassword;
                                    //funcWriteToOutputLog(twCurrent, strOutputMsg);

                                    u.SetPassword(currentParams.strDisabledAccountsPassword);
                                    u.ExpirePasswordNow();

                                    //if (currentParams.strReconfigureDisplayName == "yes")
                                    //{
                                    //    u.DisplayName = currentParams.strDisabledDisplayNamePrefix + u.DisplayName;
                                    //    //[DebugLine] Console.WriteLine("New displayname: " + currentParams.strDisabledDisplayNamePrefix + u.DisplayName);
                                    //    u.Name = currentParams.strDisabledDisplayNamePrefix + u.Name;
                                    //    strOutputMsg = "New displayname: " + currentParams.strDisabledDisplayNamePrefix + u.DisplayName;
                                    //    funcWriteToOutputLog(twCurrent, strOutputMsg);
                                    //}

                                    u.Save();

                                    foreach (string strGrpSID in grplist)
                                    {
                                        funcRemoveUserFromGroup(currentctx, strGrpSID, u.Sid.ToString(), twCurrent);
                                    }

                                    //[DebugLine] Console.WriteLine("Name: {0}", up.Name);
                                    //strOutputMsg = "Name: " + u.Name;
                                    //funcWriteToOutputLog(twCurrent, strOutputMsg);

                                    //DirectoryEntry newDE = new DirectoryEntry("LDAP://" + u.DistinguishedName);
                                    //[DebugLine] Console.WriteLine("Path: {0}", newDE.Path);

                                    //strOutputMsg = "Path: " + newDE.Path;
                                    //funcWriteToOutputLog(twCurrent, strOutputMsg);

                                    //if (!newDE.Path.Contains(currentParams.strDisabledUsersLocation))
                                    //{
                                    //    //Console.WriteLine("Move to DisabledObjects");
                                    //    funcWriteToOutputLog(twCurrent, "Move to DisabledObjects");
                                    //    newDE.MoveTo(orgDE);
                                    //    newDE.CommitChanges();
                                    //    //[DebugLine] Console.WriteLine("Path: {0}", newDE.Path);
                                    //    strOutputMsg = "Path: " + newDE.Path;
                                    //    funcWriteToOutputLog(twCurrent, strOutputMsg);
                                    //    newDE.Close();
                                    //}
                                    
                                    //[DebugLine] Console.WriteLine(u.DistinguishedName);
                                    u.Save(ctxDisabledUsers);
                                    UserPrincipal du = UserPrincipal.FindByIdentity(ctxDisabledUsers, IdentityType.SamAccountName, u.SamAccountName);
                                    //[DebugLine] Console.WriteLine(du.DistinguishedName);
                                    if(du.DistinguishedName.Contains(currentParams.strDisabledUsersLocation))
                                    {
                                        strOutputMsg = "Account: " + du.Name + " - successfully moved to " + currentParams.strDisabledUsersLocation;
                                        funcWriteToOutputLog(twCurrent, strOutputMsg);
                                    }
                                }
                            }
                        }

                        if (lstDisabledUsers.Count > 0)
                        {
                            // Create group - ToBeDeleted[Date]
                            // Add accounts to the group for the day that this process runs
                            string strGroupName = "AccountsToBeDeleted" + DateTime.Today.ToLocalTime().ToString("MMddyyyy");
                            GroupPrincipal newgrp = GroupPrincipal.FindByIdentity(ctxDisabledUsers, strGroupName);

                            if (newgrp == null)
                            {
                                newgrp = new GroupPrincipal(ctxDisabledUsers, strGroupName);
                                newgrp.Description = "Accounts";
                                newgrp.Save();
                            }

                            string strGroupMembersMessage = lstDisabledUsers.Count.ToString() + " accounts to be added to group " + newgrp.Name;
                            funcToEventLog("DisabledAccountsManager", strGroupMembersMessage, 1002);

                            foreach (string userSID in lstDisabledUsers)
                            {
                                UserPrincipal u2 = UserPrincipal.FindByIdentity(ctxDisabledUsers, IdentityType.Sid, userSID);
                                newgrp.Members.Add(u2);
                                newgrp.Save();
                            }
                        }

                        funcCloseOutputLog(twCurrent); 
                    }
                }
                else
                {
                    Exception newex = new Exception("The OU path for disabled accounts is invalid.");
                    throw newex;
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }
        
        static void funcRemoveUserFromGroup(PrincipalContext currentctx, string grpSID, string usrSID, TextWriter twCurrent)
        {
            try
            {
                UserPrincipal usr = UserPrincipal.FindByIdentity(currentctx, IdentityType.Sid, usrSID);
                DirectoryEntry tempDE = new DirectoryEntry("LDAP://" + usr.DistinguishedName);
                string strPrimaryGroupID = tempDE.Properties["primaryGroupID"].Value.ToString();
                if (!grpSID.EndsWith("-" + strPrimaryGroupID))
                {
                    GroupPrincipal grp = GroupPrincipal.FindByIdentity(currentctx, IdentityType.Sid, grpSID);
                    //[DebugLine] Console.WriteLine("Remove user from group {0}", grp.Name);
                    funcWriteToOutputLog(twCurrent, "Remove user from group: " + grp.Name);
                    if (usr != null & grp != null)
                    {
                        grp.Members.Remove(usr);
                    }
                    grp.Save();
                }
                else
                {
                    //[DebugLine] Console.WriteLine("Do not remove user from primary group.");
                    funcWriteToOutputLog(twCurrent, "Do not remove user from primary group.");
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static void funcRemoveUserAccounts(DisabledAccountsParams currentParams)
        {
            try
            {
                DateTime dtHoldPeriod = DateTime.Today.AddDays(-currentParams.intDisabledHoldPeriod);

                Domain thisDomain = Domain.GetComputerDomain();
                string strDomainName = thisDomain.Name;
                PrincipalContext ctxDisabledUsers = new PrincipalContext(ContextType.Domain, strDomainName,
                                                                         currentParams.strDisabledUsersLocation);

                TextWriter twCurrent = funcOpenOutputLog();

                // Create a PrincipalSearcher object.
                PrincipalSearcher ps = new PrincipalSearcher();

                // Create an in-memory user object to use as the query example.
                GroupPrincipal grp = new GroupPrincipal(ctxDisabledUsers);
                grp.Description = "Accounts";

                // Tell the PrincipalSearcher what to search for.
                ps.QueryFilter = grp;

                PrincipalSearchResult<Principal> psr = ps.FindAll();

                string strEventLogMsg = "";
                string strOutputMsg = "";

                if (psr.Count<Principal>() > 0)
                {
                    strEventLogMsg = "Number of AccountsToBeDeleted groups found: " + psr.Count<Principal>().ToString();
                    funcToEventLog("DisabledAccountsManager", strEventLogMsg, 1003);

                    funcWriteToOutputLog(twCurrent, "--------DisabledAccountsManager: Processing unused accounts");

                    foreach (GroupPrincipal g in psr)
                    {

                        string strDateString = g.Name.Substring(19,2).ToString() + "/" +
                                               g.Name.Substring(21,2).ToString() + "/" +
                                               g.Name.Substring(23).ToString();

                        //[DebugLine] Console.WriteLine(strDateString);

                        DateTime dtGroupCreated = Convert.ToDateTime(strDateString);
                        if (dtGroupCreated < dtHoldPeriod)
                        {
                            strEventLogMsg = "Number of accounts to be deleted in group " + g.Name + ": " + g.Members.Count.ToString();
                            funcToEventLog("DisabledAccountsManager", strEventLogMsg, 1005);

                            if (g.Members.Count > 0)
                            {
                                foreach (UserPrincipal p in g.GetMembers())
                                {
                                    strOutputMsg = "Deleting account: " + p.Name;
                                    funcWriteToOutputLog(twCurrent, strOutputMsg);
                                    p.Delete();
                                }
                            }

                            if (g.Members.Count == 0)
                            {
                                strOutputMsg = "Deleting group: " + g.Name;
                                funcWriteToOutputLog(twCurrent, strOutputMsg);
                                g.Delete();
                            }
                        }
                        else
                        {
                            //group is within hold period
                            strEventLogMsg = "No accounts to be deleted from group " + g.Name + " - group is within hold period ";
                            funcToEventLog("DisabledAccountsManager", strEventLogMsg, 1006);
                        }
                    }
                }
                else
                {
                    strEventLogMsg = "No AccountsToBeDeleted groups were found";
                    funcToEventLog("DisabledAccountsManager", strEventLogMsg, 1004);
                }

                funcCloseOutputLog(twCurrent);
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static void funcToEventLog(string strAppName, string strEventMsg, int intEventType)
        {
            try
            {
                string strLogName;

                strLogName = "Application";

                if (!EventLog.SourceExists(strAppName))
                    EventLog.CreateEventSource(strAppName, strLogName);

                //EventLog.WriteEntry(strAppName, strEventMsg);
                EventLog.WriteEntry(strAppName, strEventMsg, EventLogEntryType.Information, intEventType);
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static bool funcCheckForOU(string strOUPath)
        {
            try
            {
                string strDEPath = "";

                if (!strOUPath.Contains("LDAP://"))
                {
                    strDEPath = "LDAP://" + strOUPath;
                }
                else
                {
                    strDEPath = strOUPath;
                }

                if (DirectoryEntry.Exists(strDEPath))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return false;
            }
        }

        static bool funcCheckForFile(string strInputFileName)
        {
            try
            {
                if (System.IO.File.Exists(strInputFileName))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return false;
            }
        }

        static void funcGetFuncCatchCode(string strFunctionName, Exception currentex)
        {
            string strCatchCode = "";

            Dictionary<string, string> dCatchTable = new Dictionary<string, string>();
            dCatchTable.Add("funcCheckForFile", "f0");
            dCatchTable.Add("funcCheckForOU", "f1");
            dCatchTable.Add("funcCheckNameExclusion", "f2");
            dCatchTable.Add("funcCloseOutputLog", "f3");
            dCatchTable.Add("funcCreateDSSearcher", "f4");
            dCatchTable.Add("funcCreatePrincipalContext", "f5");
            dCatchTable.Add("funcErrorToEventLog", "f6");
            dCatchTable.Add("funcGetFuncCatchCode", "f7");
            dCatchTable.Add("funcModifyDisabledAccounts", "f10");
            dCatchTable.Add("funcOpenOutputLog", "f11");
            dCatchTable.Add("funcParseCmdArguments", "f12");
            dCatchTable.Add("funcParseConfigFile", "f13");
            dCatchTable.Add("funcPrintParameterSyntax", "f14");
            dCatchTable.Add("funcPrintParameterWarning", "f15");
            dCatchTable.Add("funcProgramExecution", "f16");
            dCatchTable.Add("funcProgramRegistryTag", "f17");
            dCatchTable.Add("funcRemoveUserAccounts", "f18");
            dCatchTable.Add("funcRemoveUserFromGroup", "f19");
            dCatchTable.Add("funcToEventLog", "f20");
            dCatchTable.Add("funcWriteToErrorLog", "f21");
            dCatchTable.Add("funcWriteToOutputLog", "f22");

            if (dCatchTable.ContainsKey(strFunctionName))
            {
                strCatchCode = "err" + dCatchTable[strFunctionName] + ": ";
            }

            //[DebugLine] Console.WriteLine(strCatchCode + currentex.GetType().ToString());
            //[DebugLine] Console.WriteLine(strCatchCode + currentex.Message);

            funcWriteToErrorLog(strCatchCode + currentex.GetType().ToString());
            funcWriteToErrorLog(strCatchCode + currentex.Message);
            funcErrorToEventLog("DisabledAccountsManager");

        }

        static void funcWriteToErrorLog(string strErrorMessage)
        {
            try
            {
                string strPath = Directory.GetCurrentDirectory();

                if (!Directory.Exists(strPath + "\\Log"))
                {
                    Directory.CreateDirectory(strPath + "\\Log");
                    if (Directory.Exists(strPath + "\\Log"))
                    {
                        strPath = strPath + "\\Log";
                    }
                }
                else
                {
                    strPath = strPath + "\\Log";
                }

                FileStream newFileStream = new FileStream(strPath + "\\Err-DisabledAccountsManager.log", FileMode.Append, FileAccess.Write);
                TextWriter twErrorLog = new StreamWriter(newFileStream);

                DateTime dtNow = DateTime.Now;

                string dtFormat = "MMddyyyy HH:mm:ss";

                twErrorLog.WriteLine("{0} \t {1}", dtNow.ToString(dtFormat), strErrorMessage);

                twErrorLog.Close();
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }

        }

        static TextWriter funcOpenOutputLog()
        {
            try
            {
                DateTime dtNow = DateTime.Now;

                string dtFormat2 = "MMddyyyy"; // for log file directory creation

                string strPath = Directory.GetCurrentDirectory();

                if (!Directory.Exists(strPath + "\\Log"))
                {
                    Directory.CreateDirectory(strPath + "\\Log");
                    if (Directory.Exists(strPath + "\\Log"))
                    {
                        strPath = strPath + "\\Log";
                    }
                }
                else
                {
                    strPath = strPath + "\\Log";
                }

                string strLogFileName = strPath + "\\DisabledAccountsManager" + dtNow.ToString(dtFormat2) + ".log";

                FileStream newFileStream = new FileStream(strLogFileName, FileMode.Append, FileAccess.Write);
                TextWriter twOuputLog = new StreamWriter(newFileStream);

                return twOuputLog;
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
                return null;
            }

        }

        static void funcWriteToOutputLog(TextWriter twCurrent, string strOutputMessage)
        {
            try
            {
                DateTime dtNow = DateTime.Now;

                //string dtFormat = "MM/dd/yyyy";
                string dtFormat2 = "MM/dd/yyyy HH:mm";
                //string dtFormat3 = "MM/dd/yyyy HH:mm:ss";

                twCurrent.WriteLine("{0} \t {1}", dtNow.ToString(dtFormat2), strOutputMessage);
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static void funcCloseOutputLog(TextWriter twCurrent)
        {
            try
            {
                twCurrent.Close();
            }
            catch (Exception ex)
            {
                MethodBase mb1 = MethodBase.GetCurrentMethod();
                funcGetFuncCatchCode(mb1.Name, ex);
            }
        }

        static void funcErrorToEventLog(string strAppName)
        {
            string strLogName;

            strLogName = "Application";

            if (!EventLog.SourceExists(strAppName))
                EventLog.CreateEventSource(strAppName, strLogName);

            //EventLog.WriteEntry(strAppName, strEventMsg);
            EventLog.WriteEntry(strAppName, "An error has occured. Check log file.", EventLogEntryType.Error, 0);
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    funcPrintParameterWarning();
                }
                else
                {
                    if (args[0] == "-?")
                    {
                        funcPrintParameterSyntax();
                    }
                    else
                    {
                        string[] arrArgs = args;
                        CMDArguments objArgumentsProcessed = funcParseCmdArguments(arrArgs);

                        if (objArgumentsProcessed.bParseCmdArguments)
                        {
                            funcProgramExecution(objArgumentsProcessed);
                        }
                        else
                        {
                            funcPrintParameterWarning();
                        } // check objArgumentsProcessed.bParseCmdArguments
                    } // check args[0] = "-?"
                } // check args.Length == 0
            }
            catch (Exception ex)
            {
                Console.WriteLine("errm0: {0}", ex.Message);
            }
        }
    }
}
