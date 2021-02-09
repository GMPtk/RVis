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

  [ValidateScript( {
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
$outputNames = New-Object -TypeName System.Collections.Generic.List[PSCustomObject]

while ($index -lt $nLines)
{
  if (is_comment $lines[$index])
  {
    ++$index
    continue
  }

  $endSection = $lines[$index] -like "*}*"

  $isParameterAssignment = $lines[$index] -match "\s*(\w+)\s*=\s*([\d+-Ee]+)\s*;\s*(?:#\s*([^\[]*))(?:\[\s*([^\]]*)\])?"

  if ($isParameterAssignment)
  {
    $name = $Matches[1]
    $value = $Matches[2]
    $comment = $Matches[3]
    $unit = $Matches[4]

    $parameterAssignment = [PSCustomObject]@{
      Name    = $name
      Value   = $value
      Comment = $comment
      Unit    = $unit
    }

    $parameterAssignments.Add($parameterAssignment)

    $lines[$index] = $lines[$index] -replace $value, "{{$($name)}}"
  }

  if ($lines[$index] -like "*printstep*")
  {
    ++$index

    while ($index -lt $nLines)
    {
      if (is_comment $lines[$index])
      {
        ++$index
        continue
      }

      $isOutput = $lines[$index] -match "\s*(\w+),?\s*(?:#\s*([^\[]*))(?:\[\s*([^\]]*)\])?"

      if ($isOutput)
      {
        $name = $Matches[1]
        $comment = $Matches[2]
        $unit = $Matches[3]
    
        $outputName = [PSCustomObject]@{
          Name    = $name
          Comment = $comment
          Unit    = $unit
        }

        $outputNames.Add($outputName)
      }

      if ($lines[$index] -like "*;*")
      {
        break;
      }

      ++$index
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

$parameterStatements = $parameterAssignments | ForEach-Object { $i = 1 } { 
  $statement = "  $($_.Name) = $($_.Value)"
  
  if ($i -lt $parameterAssignments.Count)
  {
    $statement = $statement + ","
  }
  
  if ($_.Comment -or $_.Unit)
  {
    $statement = $statement + " # " + $_.Comment

    if ($_.Unit)
    {
      $statement = $statement + "[" + $_.Unit + "]"
    }
  }

  ++$i

  return $statement
}

$configParameterAssignments = [string]::Join("`n", $parameterStatements)

$outputStatements = $outputNames | ForEach-Object { $i = 1 } { 
  $statement = "  $($_.Name) = NA"
  
  if ($i -lt $outputNames.Count)
  {
    $statement = $statement + ","
  }
  
  if ($_.Comment -or $_.Unit)
  {
    $statement = $statement + " # " + $_.Comment

    if ($_.Unit)
    {
      $statement = $statement + "[" + $_.Unit + "]"
    }
  }

  ++$i

  return $statement
}

$configOutputNames = [string]::Join("`n", $outputStatements)

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
