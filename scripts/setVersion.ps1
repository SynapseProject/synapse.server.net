param (
    [string]$path = "c:\Users\$($env:username)\desktop\AssemblyInfo.cs",
    [int]$major = 1,
    [int]$minor = 0
 )

#$file = Get-Content $path | Out-String
$file = [System.IO.File]::ReadAllText( $path ) 

#adjust AssemblyVersion
$pattern = 'AssemblyVersion\( \"(?<v>\d.\d.\d.\d)\" \)'
$vers = ([regex]::Match( $file, $pattern )).Groups
[Version]$version = [Version]$vers.Groups[1].Value
$v = 'AssemblyVersion( "{0}.{1}.{2}.{3}" )' -f $major, $minor, $version.Build, $version.Revision
$file = ([regex]::Replace( $file, $pattern, $v ))


#adjust AssemblyFileVersion
$pattern = 'AssemblyFileVersion\( \"(?<v>.*)\" \)'
$vers = ([regex]::Match( $file, $pattern )).Groups
[Version]$version = [Version]$vers.Groups[1].Value

$now = [DateTime]::Now
$build = '{0}{1}' -f $now.ToString( 'yy' ), $now.DayOfYear.ToString( 'D3' )
[int]$revision = [int]$version.Revision
if( $version.Build.ToString() -eq $build )
{
    $revision = [int]$version.Revision + 1
}

$v = 'AssemblyFileVersion( "{0}.{1}.{2}.{3}" )' -f $major, $minor, $build, $revision
$file = ([regex]::Replace( $file, $pattern, $v ))

Write-Host $file
[System.IO.File]::SetAttributes( $path, [System.IO.FileAttributes]::Normal );
[System.IO.File]::WriteAllText( $path, $file )
#Set-Content -Path $path -Value $file.Trim()