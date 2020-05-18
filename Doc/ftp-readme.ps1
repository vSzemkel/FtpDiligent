# Ftp
# Install IIS with Ftp option
# In IIS-manager on server node choose Add Ftp-site
# Grant directory access on ftp-root to vSzemkel
# Turn on Basic Authentication
# Add Allow Rule in FTP Authorization Rules for [Read | Write] to vSzemkel
# For FTPS define data port range in Ftp Firewall Support
# Add Allow Rule in Ftp IP Address and Domain Restictions
# Sonfig stored in file C:\Windows\System32\inetsrv\Config\applicationHost.config
Restart-Service ftpsvc

# OpenSSH
# Check OpenSSH instalation status
Get-WindowsCapability -Online | ? Name -like 'OpenSSH*'
# Install the OpenSSH Client
Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0
# Install the OpenSSH Server
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
# Uninstall the OpenSSH Client
Remove-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0
# Starting OpenSSH server
Start-Service sshd
# OPTIONAL but recommended:
Set-Service -Name sshd -StartupType 'Automatic'
# Confirm the Firewall rule is configured. It should be created automatically by setup. 
Get-NetFirewallRule -Name *ssh*
# There should be a firewall rule named "OpenSSH-Server-In-TCP", which should be enabled
# If the firewall does not exist, create one
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
# User key geenration
cd ~\.ssh\
ssh-keygen
Get-Service ssh-agent | Select StartType
Get-Service -Name ssh-agent | Set-Service -StartupType Manual
Start-Service ssh-agent
ssh-add ~\.ssh\test_rsa
# Copy public key to the server file C:\Users\vSzemkel\.ssh\authorized_keys
(Get-Acl C:\Users\vSzemkel\.ssh\authorized_keys).Owner # ma być vSzemkel
icacls C:\Users\vSzemkel\.ssh\authorized_keys # ma być vSzemel(F), Administrators(F) i nic więcej
# If user is an admin then copy authorized_keys to file %programdata%\ssh\administrators_authorized_keys
# Open SSH session
ssh vszemkel@hostname-or-ip

/* Developers SQL instance credentials                  */
/* create login [ftp] with password = 'k8vSw1xo'        */
/* create user [ftp] with default_schema=[ftp]          */
/* MSSQLLocalDb: alter user dbo with default_schema=ftp */
/* exec sp_addrolemember 'db_owner', 'ftp'              */
/* create schema ftp with authorization ftp             */