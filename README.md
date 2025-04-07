
# DisabledAccountsManager

DESCRIPTION: 
- Manage disabled user accounts

> NOTES: "v1.0" was completed in 2011. DisabledAccountsManager was written to work in on-premises Active Directory environments. The purpose of DisabledAccountsManager was/is to organize disabled user accounts to support a "clean AD".

## Requirements:

Operating System Requirements:
- Windows Server 2003 or higher (32-bit)
- Windows Server 2008 or higher (32-bit)

Additional software requirements:
Microsoft .NET Framework v3.5

Active Directory requirements:
One of following domain functional levels
- Windows Server 2003 domain functional level
- Windows Server 2008 domain functional level

Additional requirements:
Domain administrative access is required to perform operations by DisabledAccountsManager


## Operation and Configuration:

Command-line parameters:
- run (Required parameter)

Configuration file: configDisabledAccountsManager.txt
- Located in the same directory as DisabledAccountsManager.exe

Configuration file parameters:

DisabledEmailAddressPrefix: Prefix to use if modifying the email address for disabled accounts

DisabledHoldPeriod: Number of days to wait before removing, from Active Directory, disabled accounts in the OU specified using the DisabledUsersLocation parameter
- If not specified, the default is 7 days

DisabledUsersLocation: Specifies an OU location in Active Directory to place disabled accounts; The OU location specified must already be present

Exclude: Exclude one or more accounts by specifying the desired account on a separate line

ExcludePrefix: Exclude one or more accounts using a prefix that will match the desired account(s)

ExcludeSuffix: Exclude one or more accounts using a suffix that will match the desired account(s)

HideFromGlobalAddressList: Determines if DisabledAccountsManager will hide the user account from the Global Address List

ReconfigureEmailAddress: Determines if DisabledAccountsManager will use DisabledEmailAddressPrefix to modify the email address for disabled accounts

Output:
- Located in the Log directory inside the installation directory; log files are in tab-delimited format
- Path example: (InstallationDirectory)\Log\

Additional detail:
- DisabledAccountsManager will act on any disabled user account except disabled user accounts excluded from being processed. This includes disabled users account that were disabled manually or by other automated processes. Excluded user accounts include those specified in the configuration file and accounts that are in the OU specified using DisabledUsersLocation parameter in the configuration file.
- DisabledAccountsManager is built to perform the following operations on a disabled account:
    - Reset the disabled user account password to a random password
    - Set the disabled user account to force a password change on the next logon (if the account is re-enabled)
    - Remove all group membership except for the disabled user account’s Primary group.
    - Modify the disabled user account’s email address
    - Hide the disabled user account from the Global Address List
    - Move the disabled user account to an OU specified in the configuration file
    - Add the disabled user account to a group created specifically to trigger the removal of the disabled user account from Active Directory based upon the DisabledHoldPeriod parameter
