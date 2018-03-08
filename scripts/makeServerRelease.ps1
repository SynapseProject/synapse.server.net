param( 
    [string]$workingDir = ($MyInvocation.MyCommand.Path).Replace( $MyInvocation.MyCommand.Name, "" ),
    [string]$userName = '',
    [string]$token = ''
)

function RemoveFile( $path )
{
    if( Test-Path $path )
    {
        Remove-Item( $path ) -Recurse
    }
}

function CleanFolder
{
    param( [string]$folder, [boolean]$core )

    RemoveFile( $folder + '\*.pd*' )
    RemoveFile( $folder + '\*.vshost.*' )
    RemoveFile( $folder + '\*.xml' )
    RemoveFile( $folder + '\*.zip' )
    RemoveFile( $folder + '\Synapse.Server.Install*' )
    RemoveFile( $folder + '\Synapse.Server.config.yaml' )
    if( $core )
    {
        RemoveFile( $folder + '\Synapse.Core.dll' )
        RemoveFile( $folder + '\YamlDotNet.dll' )
        RemoveFile( $folder + '\Synapse.Controller.Common.dll' )
        RemoveFile( $folder + '\System.Net.Http.Formatting.dll' )
        RemoveFile( $folder + '\System.Web.Http.dll' )
        RemoveFile( $folder + '\Newtonsoft.Json.dll' )
        RemoveFile( $folder + '\Suplex.Core.dll' )
        RemoveFile( $folder + '\Suplex.Core.dll.config' )
    }
}

function CopyFolder( $source, $destination )
{
    New-Item $destination -Type directory | Out-Null
    $r = $dir.ToLower().Replace( '\scripts', $source )
    Copy-Item $r $destination -recurse
    CleanFolder $destination $true
}

function Unzip( $source, $destination )
{
    [io.compression.zipfile]::ExtractToDirectory( $source, $destination )
}

function DownloadRelease( $repo, $destination, $headers )
{
	#apparently github upgraded TLS versions
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Write-Host ("Downloading: " + $repo)
    $uri = ('https://api.github.com/repos/synapseproject/' + $repo + '/releases')
    $rel = Invoke-WebRequest -Headers $headers -Uri $uri | ConvertFrom-Json

    foreach ($asset in $rel[0].assets) 
    {
        $url = $asset.browser_download_url
        $name = $dir + '\' + $asset.name
    
        (New-Object System.Net.WebClient).DownloadFile( $url, $name )
    
        $repDest = $dir + '\' + $repo
        Unzip $name $repDest
        Copy-Item ($repDest + '\*') $destination -Force -Recurse
        CleanFolder $destination $true
    
        Remove-Item $repDest -Recurse
        Remove-Item $name
    }
}

function FindUrl( [string]$uri, [string]$folder, [bool]$istree, $headers )
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Write-Host ("Finding: [" + $folder + "] at: " + $uri)
    $content = Invoke-WebRequest -Headers $headers -Uri $uri | ConvertFrom-Json

    if( $istree ) {
        $content = $content.tree
    }

    $url = "";
    foreach( $item in $content )
    {
        if( $item.path -eq $folder ) {
            if( $istree ) { $url = $item.url; }
            else { $url = $item._links.git; }
            break;
        }
    }

    return $url;
}

function DownloadSamples( $headers )
{
    $uri = FindUrl 'https://api.github.com/repos/synapseproject/synapse.examples/contents' 'Plans' $false $headers
    if( $uri -ne "" ) {
        $uri = FindUrl $uri 'Release Samples' $true $headers

        if( $uri -ne "" ) {
            $uri = $uri + "?recursive=1"
        }
    }

    if( $uri -ne "" ) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Write-Host ("Getting Samples List from: " + $uri)
        $content = Invoke-WebRequest -Headers $headers -Uri $uri | ConvertFrom-Json

        $relFolder = "Release\"
        foreach( $item in $content.tree )
        {
            $fp = $relFolder + $item.path

            if( $item.type -eq "tree" ) {
                if( !(Test-Path $fp ) ) {
                    New-Item -ItemType directory -Path $fp
                }
            }
            elseif( $item.type -eq "blob" ) {
                $blob = Invoke-WebRequest -Headers $headers -Uri $item.url | ConvertFrom-Json
                $bytes = [System.Convert]::FromBase64String( $blob.Content );
                $str = [System.Text.Encoding]::ASCII.GetString( $bytes ).Replace("`n", "`r`n");
                New-Item -ItemType File -Path $fp -Value $str -Force
            }
        }
    }
    else {
        Write-Host "Could not download Samples."
    }
}

function GetSynapseCli( $destination, $headers )
{
    $cli = ($dir + '\cli')
    if(!(Test-Path $cli ))
    {
        New-Item -ItemType Directory -Path $cli | Out-Null
    }
    DownloadRelease 'synapse.core.net' $cli $headers
    Move-Item ($cli + '\synapse.cli.exe') $destination -Force
    RemoveFile( $cli + '\*' );
    Remove-Item $cli
}

function GetVersionInfo( $folder )
{
    return [System.Diagnostics.FileVersionInfo]::GetVersionInfo($folder + '\Synapse.Server.exe').FileVersion
}

function MakeServerRelease( $userName, $token )
{
    $authInfo = '{0}:{1}' -f $userName, $token
    $encAuth = [System.Convert]::ToBase64String([char[]]$authInfo);
    $headers = @{
        Authorization = 'Basic {0}' -f $encAuth;
    };

    $release = 'Release';
    $fr = ($dir + '\' + $release)

    if( Test-Path( $release ) )
    {
        RemoveFile( $fr + '\*' );
        Remove-Item $release
    }

    #copy Release folder
    $r = $dir.ToLower().Replace( '\scripts', '\Synapse.Server\bin\Release')
    Copy-Item $r $dir -recurse
    CleanFolder $release $false
    Unzip ($dir + '\_Setup.zip') $fr

    #delete any existing folders from Release
    Get-ChildItem $release -directory | ForEach-Object { Remove-Item -recurse -force ( $release + '\' + $_ ) }

    #these folders are created as empty
    Write-Host "Creating folders."
    New-Item ($release + '\Assemblies') -Type directory | Out-Null
    New-Item ($release + '\Logs') -Type directory | Out-Null
    New-Item ($release + '\Crypto') -Type directory | Out-Null

    #auth folder: Authentication & Authorization
	# -copy Authentication
    Write-Host "Copying Authentication release files."
	$auth = ($release + '\Auth')
    CopyFolder '\Synapse.Authentication\bin\Release\*' $auth
	# -download Suplex Authorization
    DownloadRelease 'synapse.authorization.suplex' $auth $headers


    #dal folder
    Write-Host "Creating DAL folders, copying DAL release files."
    CopyFolder '\Synapse.Controller.Dal.FileSystem\bin\Release\*' ($release + '\Dal')
    New-Item ($release + '\Dal\History') -Type directory | Out-Null
    New-Item ($release + '\Dal\Plans') -Type directory | Out-Null
    New-Item ($release + '\Dal\Security') -Type directory | Out-Null
    Write-Host "Unzipping Suplex."
    Unzip ($dir + '\_Suplex.zip') ($fr + '\Dal\Security')


    #samples folder
    Write-Host "Creating Sample folders, copying Sample release files."
    DownloadSamples $headers


    #handlers folder
    Write-Host "Creating Handlers folders."
    $handlers = ($fr + '\Handlers')
    New-Item  $handlers -Type directory | Out-Null
    DownloadRelease 'handlers.ActiveDirectoryUtil.net' $handlers $headers
    DownloadRelease 'handlers.Aws.net' $handlers $headers
    DownloadRelease 'handlers.CommandLine.net' $handlers $headers
    DownloadRelease 'handlers.DataTransformation.net' $handlers $headers
    DownloadRelease 'handlers.Dns.net' $handlers $headers
    DownloadRelease 'handlers.FileUtil.net' $handlers $headers
    DownloadRelease 'handlers.Sql.net' $handlers $headers
    DownloadRelease 'handlers.Uri.net' $handlers $headers
    DownloadRelease 'handlers.WinUtil.net' $handlers $headers
    DownloadRelease 'handlers.Legacy.ConfigFile.net' $handlers $headers
    DownloadRelease 'handlers.Legacy.Database.net' $handlers $headers
    DownloadRelease 'handlers.Legacy.RemoteCommand.net' $handlers $headers
    DownloadRelease 'handlers.Legacy.SQLCommand.net' $handlers $headers
    DownloadRelease 'handlers.Legacy.WinUtil.net' $handlers $headers
    
    #GetSynapseCli...
    GetSynapseCli $fr $headers

    #zip the Release folder
    Write-Host "Creating Release zip."
    $ver = GetVersionInfo $fr
    $archive = ($dir + '\Synapse.Server.' + $ver + '.zip') #-beta
    RemoveFile $archive
    [io.compression.zipfile]::CreateFromDirectory( $fr, $archive, [System.IO.Compression.CompressionLevel]::Optimal, $false );

    #clean up
    Write-Host "Deleting temp files."
    RemoveFile( $fr + '\*' );
    Remove-Item $release
}


Add-Type -assembly "system.io.compression.filesystem"

$dir = $workingDir
Set-Location $dir

MakeServerRelease $userName $token