param(
    [string]$Suffix = '',
    [string[]]$Names = @('AIxVR2027_VRMicroscope_Paper_EN_Formal', 'AIxVR2027_VRMicroscope_Draft_Formal')
)
$ErrorActionPreference = 'Stop'
$paperDirectory = $PSScriptRoot
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    foreach ($name in $Names) {
        $sourcePath = (Resolve-Path (Join-Path $paperDirectory ($name + $Suffix + '.docx'))).Path
        $pdfPath = [System.IO.Path]::ChangeExtension($sourcePath, '.pdf')
        Write-Output ('Opening: ' + $name + $Suffix)
        $document = $word.Documents.Open($sourcePath, $false, $true)
        try {
            $document.Repaginate()
            $pages = $document.ComputeStatistics(2)
            Write-Output ('Exporting: ' + $name + $Suffix + " ($pages pages)")
            $document.ExportAsFixedFormat($pdfPath, 17)
            Write-Output ($name + ": PDF exported ($pages pages)")
        }
        finally { $document.Close(0) }
    }
}
finally {
    $word.Quit()
}
