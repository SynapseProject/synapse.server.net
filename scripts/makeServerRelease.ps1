Add-Type -assembly "system.io.compression.filesystem"

$dir = Split-Path $MyInvocation.MyCommand.Path
Set-Location $dir

Main

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
    }
}

function CopyFolder( $source, $destination )
{
    New-Item $destination -Type directory
    $r = $dir.ToLower().Replace( '\scripts', $source )
    Copy-Item $r $destination -recurse
    CleanFolder $destination $true
}

function Unzip( $source, $destination )
{
    [io.compression.zipfile]::ExtractToDirectory( $source, $destination )
}

function DownloadRelease( $repo, $destination )
{
    $uri = ('https://api.github.com/repos/synapseproject/' + $repo + '/releases')
    $rel = Invoke-WebRequest -Uri $uri | ConvertFrom-Json
    $url = $rel[0].assets[0].browser_download_url
    $name = $dir + '\' + $rel[0].assets[0].name

    (New-Object System.Net.WebClient).DownloadFile( $url, $name )

    Unzip $name $destination
    CleanFolder $destination $true

    Remove-Item $name
}

function GetSynapseCli( $destination )
{
    $cli = ($dir + '\cli')
    DownloadRelease 'synapse.core.net' $cli
    Move-Item ($cli + '\synapse.cli.exe') $destination -Force
    RemoveFile( $cli + '\*' );
    Remove-Item $cli
}

function GetVersionInfo( $folder )
{
    return [System.Diagnostics.FileVersionInfo]::GetVersionInfo($folder + '\Synapse.Server.exe').FileVersion
}

function Main()
{
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
    Unzip ($dir + '\_setup.zip') $fr

    #delete any existing folders from Release
    Get-ChildItem $release -directory | ForEach-Object { Remove-Item -recurse -force ( $release + '\' + $_ ) }

    #these folders are created as empty
    New-Item ($release + '\Assemblies') -Type directory
    New-Item ($release + '\Logs') -Type directory
    New-Item ($release + '\Crypto') -Type directory

    #authentication folder
    CopyFolder '\Synapse.Authentication\bin\Release\*' ($release + '\Authentication')

    #dal folder
    CopyFolder '\Synapse.Controller.Dal.FileSystem\bin\Release\*' ($release + '\Dal')
    New-Item ($release + '\Dal\History') -Type directory
    New-Item ($release + '\Dal\Plans') -Type directory
    New-Item ($release + '\Dal\Security') -Type directory
    Unzip ($dir + '\_Plans.zip') ($fr + '\Dal')
    Unzip ($dir + '\_Suplex.zip') ($fr + '\Dal\Security')

    #handlers folder
    $handlers = ($fr + '\Handlers')
    New-Item  $handlers -Type directory
    DownloadRelease 'handlers.CommandLine.net' $handlers
    DownloadRelease 'handlers.Sql.net' $handlers
    DownloadRelease 'handlers.ActiveDirectory.net' $handlers

    #GetSynapseCli...
    GetSynapseCli $fr

    #zip the Release folder
    $ver = GetVersionInfo $fr
    $archive = ($dir + '\Synapse.Server.' + $ver + '-beta.zip')
    RemoveFile $archive
    [io.compression.zipfile]::CreateFromDirectory( $fr, $archive, [System.IO.Compression.CompressionLevel]::Optimal, $false );

    #clean up
    RemoveFile( $fr + '\*' );
    Remove-Item $release
}