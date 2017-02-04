param (
    [string]$path,  #= "c:\Users\$($env:username)\desktop\AssemblyInfo.cs",
    [int]$major = 0,
    [int]$minor = 1,
    [string]$versionFile  #= "c:\Users\$($env:username)\desktop\AssemblyInfo.xml"
 )

$now = [DateTime]::Now
$avp = 'AssemblyVersion\( \"(?<v>\d.\d.\d.\d)\" \)'
$afvp = 'AssemblyFileVersion\( \"(?<v>.*)\" \)'

$file = [System.IO.File]::ReadAllText( $path )

if( [System.IO.File]::Exists( $versionFile ) )
{
    [Xml]$vf = Get-Content $versionFile

    #adjust AssemblyVersion
    $v = 'AssemblyVersion( "{0}.{1}.{2}.{3}" )' -f $vf.ai.av.Major, $vf.ai.av.Minor, $vf.ai.av.Build, $vf.ai.av.Revision
    $file = ([regex]::Replace( $file, $avp, $v ))

    #adjust AssemblyFileVersion
    if( $vf.ai.afv.Build.ToString() -eq '#' )
    {
        $vf.ai.afv.Build = '{0}{1}' -f $now.ToString( 'yy' ), $now.DayOfYear.ToString( 'D3' )
    }
    $v = 'AssemblyFileVersion( "{0}.{1}.{2}.{3}" )' -f $vf.ai.afv.Major, $vf.ai.afv.Minor, $vf.ai.afv.Build, $vf.ai.afv.Revision
    $file = ([regex]::Replace( $file, $afvp, $v ))
}
else
{
    #adjust AssemblyVersion
    $vers = ([regex]::Match( $file, $avp )).Groups
    [Version]$version = [Version]$vers.Groups[1].Value
    $v = 'AssemblyVersion( "{0}.{1}.{2}.{3}" )' -f $major, $minor, $version.Build, $version.Revision
    $file = ([regex]::Replace( $file, $avp, $v ))


    #adjust AssemblyFileVersion
    $vers = ([regex]::Match( $file, $afvp )).Groups
    [Version]$version = [Version]$vers.Groups[1].Value

    $build = '{0}{1}' -f $now.ToString( 'yy' ), $now.DayOfYear.ToString( 'D3' )
    [int]$revision = 1
    if( $version.Build.ToString() -eq $build )
    {
        $revision = [int]$version.Revision + 1
    }

    $v = 'AssemblyFileVersion( "{0}.{1}.{2}.{3}" )' -f $major, $minor, $build, $revision
    $file = ([regex]::Replace( $file, $afvp, $v ))
}

#Write-Host $file
[System.IO.File]::SetAttributes( $path, [System.IO.FileAttributes]::Normal );
[System.IO.File]::WriteAllText( $path, $file )
