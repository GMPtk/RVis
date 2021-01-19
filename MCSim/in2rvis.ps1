<#
.SYNOPSIS
Generate .config.R and .template.in files from a .in file

.PARAMETER pathToInFile
The name, relative, or fully qualified path to the source .in file

.EXAMPLE
C:\PS> in2rvis.ps1 perc.in
#>

[CmdletBinding()]

param (

  [ValidateScript({
    if (-not ($_ | Test-Path -PathType Leaf))
    {
      throw "$_ does not exist"
    }
    return $true
  })]
  [System.IO.FileInfo]$inFile

)

function is_comment
{
  param (
    [string]$line
  )
  
  $line.Trim().StartsWith("#")
}

$lines = Get-Content $inFile
$nLines = $lines.Length
$index = 0

while ($index -lt $nLines)
{
  if (-not (is_comment $lines[$index]) -and $lines[$index] -like "*simulation*")
  {
    break;
  }

  ++$index
}

if ($index -eq $nLines)
{
  Write-Error -Message "Failed to find keyword Simulation"
  exit 1
}

while ($index -lt $nLines)
{
  if (-not (is_comment $lines[$index]) -and $lines[$index] -like "*{*")
  {
    break;
  }

  ++$index
}

if ($index -eq $nLines)
{
  Write-Error -Message "Failed to find opening brace for keyword Simulation"
  exit 1
}

$parameterAssignments = New-Object -TypeName System.Collections.Generic.List[PSCustomObject]
$outputNames = New-Object -TypeName System.Collections.Generic.List[string]

while ($index -lt $nLines)
{
  if (is_comment $lines[$index])
  {
    ++$index
    continue
  }

  $endSection = $lines[$index] -like "*}*"

  $isParameterAssignment = $lines[$index] -match "\s*(\w+)\s*=\s*([\d+-Ee]+)\s*;\s*$"

  if ($isParameterAssignment)
  {
    $name = $Matches[1]
    $value = $Matches[2]

    $parameterAssignment = [PSCustomObject]@{
      Name  = $name
      Value = $value
    }
    $parameterAssignments.Add($parameterAssignment)

    $lines[$index] = $lines[$index] -replace $value, "{{$($name)}}"
  }

  if ($lines[$index] -like "*printstep*")
  {
    $printStep = $lines[$index]

    while ($index -lt $nLines)
    {
      if (is_comment $lines[$index])
      {
        ++$index
        continue
      }

      $line = $lines[$index]

      $indexOfComment = $line.IndexOf("#")
      if ($indexOfComment -ne -1)
      {
        $line = $line.SubString(0, $indexOfComment)
      }

      $printStep = $printStep + $line

      if ($lines[$index] -like "*;*")
      {
        break;
      }

      ++$index
    }
  
    $isPrintStep = $printStep -match "PrintStep\s*\(([\w\s,]*)\s*,\s*[\deE+-.]*\s*,\s*[\deE+-.]*\s*,\s*[\deE+-.]*\s*\)\s*;"
  
    if (-not $isPrintStep)
    {
      Write-Error -Message "Unrecognised PrintStep syntax: $printStep"
      exit 1
    }
  
    $tokens = $Matches[1] -split ","
  
    foreach ($token in $tokens)
    {
      $outputNames.Add($token.Trim())  
    }
  }

  if ($endSection)
  {
    break;
  }

  ++$index
}

$pathToTemplateIn = [IO.Path]::ChangeExtension($inFile.FullName, ".template.in")

Set-Content -Path $pathToTemplateIn -Value $lines

$configName = [IO.Path]::GetFileNameWithoutExtension($inFile.Name)

$configParameterAssignments = [string]::Join(", # ? [?]`n", ($parameterAssignments | ForEach-Object { "  $($_.Name) = $($_.Value)" }))
if ($parameterAssignments.Count -gt 1)
{
  $configParameterAssignments = $configParameterAssignments + " # ? [?]"
}

$configOutputNames = [string]::Join(", # ? [?]`n", ($outputNames | ForEach-Object { "  $_ = NA" }))
if ($outputNames.Count -gt 1)
{
  $configOutputNames = $configOutputNames + " # ? [?]"
}

$configRContent = @"
import <- list(
  
  simulationName = "$configName",
  description = "$configName",
  importName = "$configName"
  
)

parameters <- list(

$configParameterAssignments

)

independentVariable <- list(

  Time = NA # ? [?]

)

outputs <- list(
  
$configOutputNames

)
"@

$pathToConfigR = [IO.Path]::ChangeExtension($inFile.FullName, ".config.R")

Set-Content -Path $pathToConfigR -Value $configRContent

exit 0
