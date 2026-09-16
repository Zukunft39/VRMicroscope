$ErrorActionPreference = 'Stop'
$paperDirectory = Join-Path $PSScriptRoot 'formatted'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    foreach ($name in @('AIxVR2027_VRMicroscope_Draft_Formal', 'AIxVR2027_VRMicroscope_Paper_EN_Formal')) {
        $sourcePath = Join-Path $paperDirectory ($name + '.docx')
        $pdfPath = Join-Path $paperDirectory ($name + '.pdf')
        $document = $word.Documents.Open($sourcePath, $false, $false)
        try {
            $document.Repaginate()
            $pages = $document.ComputeStatistics(2)
            $document.Save()
            $document.ExportAsFixedFormat($pdfPath, 17)
            Write-Output ($name + ': ' + $pages + ' pages; PDF exported')
        }
        finally { $document.Close(0) }
    }
}
finally { $word.Quit() }
