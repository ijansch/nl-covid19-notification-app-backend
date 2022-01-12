# Not designed for Powershell ISE
# Double-check you are allowed to run custom scripts.

$VariablesSource = "Development"

if("#{Deploy.HSMScripting.OpenSslLoc}#" -like "*Deploy.HSMScripting.OpenSslLoc*")
{
	#dev
    $OpenSslLoc = "`"C:\Program Files\OpenSSL-Win64\bin\openssl.exe`""
    $HSMAdminToolsDir = "C:\Program Files\Utimaco\CryptoServer\Administration"
}
else
{
	#test, accp and prod
	$VariablesSource = "Deploy"
    $OpenSslLoc = "`"#{Deploy.HSMScripting.OpenSslLoc}#`""
    $HSMAdminToolsDir = "#{Deploy.HSMScripting.HSMAdminToolsDir}#"
}


function RunWithErrorCheck ([string]$command) 
{
	iex "& $command"

    if($lastexitcode -ne 0)
    {
		write-Warning "Script terminated due to an error. :("
		Read-Host 'Press Enter to continue'
        exit
    }
}

function Pause ($Message = "Press any key to continue...`n") {
    If ($psISE) {
        # The "ReadKey" functionality is not supported in Windows PowerShell ISE.
 
        $Shell = New-Object -ComObject "WScript.Shell"
        $Button = $Shell.Popup("Click OK to continue.", 0, "Script Paused", 0)
 
        Return
    }
 
    Write-Host -NoNewline $Message
 
    $Ignore =
        16,  # Shift (left or right)
        17,  # Ctrl (left or right)
        18,  # Alt (left or right)
        20,  # Caps lock
        91,  # Windows key (left)
        92,  # Windows key (right)
        93,  # Menu key
        144, # Num lock
        145, # Scroll lock
        166, # Back
        167, # Forward
        168, # Refresh
        169, # Stop
        170, # Search
        171, # Favorites
        172, # Start/Home
        173, # Mute
        174, # Volume Down
        175, # Volume Up
        176, # Next Track
        177, # Previous Track
        178, # Stop Media
        179, # Play
        180, # Mail
        181, # Select Media
        182, # Application 1
        183  # Application 2
 
    While ($KeyInfo.VirtualKeyCode -Eq $Null -Or $Ignore -Contains $KeyInfo.VirtualKeyCode) {
        $KeyInfo = $Host.UI.RawUI.ReadKey("NoEcho, IncludeKeyDown")
    }
 
    Write-Host
}
# Got this from https://adamstech.wordpress.com/2011/05/12/how-to-properly-pause-a-powershell-script/

function SetErrorToStop
{
	$script:ErrorActionPreference = "Stop"
	write-host "Error-behaviour of script is set to $script:ErrorActionPreference."
}

function CheckNotIse
{
	if($host.name -match "ISE")
	{
		write-host "`nYou are running this script in Powershell ISE. Please switch to the regular Powershell."
		Pause
		
		exit
	}
}


#
# start
#


write-host "`nCertificate and keypair remover"
write-host "Location and date: $env:computername. $(Get-Date -Format `"dd MMM, HH:mm:ss`")."
CheckNotIse

write-warning "`nPlease check the following:`
- Using variables from $VariablesSource. Correct?`
- Has the HSM been backed up?
If not: abort this script with Ctrl+C."
Pause

SetErrorToStop

write-host "`nPre-check for key presence"
Pause

RunWithErrorCheck "`"$HSMAdminToolsDir\cngtool`" listkeys"

$certName = read-host "`nPlease enter the filename of the certificate with extension"
$Host.UI.RawUI.FlushInputBuffer() #clears any annoying newlines that were accidentally copied in

$CertThumb = (RunWithErrorCheck "$OpenSslLoc x509 -fingerprint -sha1 -noout -in $certName") -replace "SHA1 Fingerprint=|:",""

if($CertThumb.Length -ne 40)
{
	write-warning "The extracted thumbprint was $($CertThumb.Length) instead of 40 characters long!`nThe thumb: $CertThumb"
	Pause
	
	exit
}

$storeData = RunWithErrorCheck "certutil -store `"My`" `"$CertThumb`""
$containerEntry = $storeData | Select-String -pattern "Key Container" | Select -ExpandProperty Line
$containerEntry -match "= (?<KeycontainerString>.*)" | Out-Null
$containerString = $matches["KeycontainerString"]

write-host "`nAbout to remove the following certificate and HSM-keypair:"
$storeData | Foreach-object {write-host $_}
Write-host "" #empty line for emphasis

Write-Warning "`nIs this all correct? If not: abort this script with Ctrl+C."
Pause

write-host "Windows cert store:"
$removeResult = certutil -delstore "my" $certThumb #-delstore won't return an error on a failed remove

$removeResult | Foreach-object {write-host $_}

if([string]::concat($removeResult) -notmatch "Deleting Certificate") #failed remove won't contain this string in output
{
	write-error "No certificate was removed!"
	exit
}

write-host "`nHSM:"
Pause
RunWithErrorCheck "`"$HSMAdminToolsDir\cngtool`" Name=$containerString deletekey"

write-host "`nPost-check for key presence"
Pause
RunWithErrorCheck "`"$HSMAdminToolsDir\cngtool`" listkeys"

write-host "`nRemoval has been completed."
Pause
