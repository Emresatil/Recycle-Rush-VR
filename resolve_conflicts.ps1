$branches = @(
    'feature/pollution-health-system',
    'feature/ar-surface-abstraction',
    'feature/advanced-waste-interactions',
    'feature/gravity-grab-and-portal-vfx',
    'feature/spatial-audio-sfx',
    'feature/precision-accuracy-engine',
    'feature/endgame-stats-and-highscore',
    'feature/combo-and-achievement-system',
    'feature/gamemanager-state-machine',
    'feature/save-manager-and-haptics'
)

foreach ($b in $branches) {
    Write-Host "Processing $b..."
    git checkout $b
    git merge origin/main
    
    # Resolve Font Asset
    git checkout --theirs "Assets/_App/UI/Fonts/ChakraPetch-Medium SDF.asset"
    
    # Resolve BinTrigger.cs
    $file = "Assets/_App/Scripts/Environment/BinTrigger.cs"
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        $content = $content -replace "(?s)<<<<<<< HEAD\r?\n(.*?)\r?\n=======\r?\n.*?CoinChange(.*?)XpChange(.*?)\r?\n.*?>>>>>>> [^\r\n]+", "$1
            CoinChange$2XpChange$3"
        $content = $content -replace "(?s)<<<<<<< HEAD\r?\n=======\r?\n(.*?)\r?\n>>>>>>> [^\r\n]+", "$1"
        
        Set-Content -Path $file -Value $content -NoNewline
    }
    
    git add .
    git commit -m "Merge main and resolve conflicts automatically"
    git push origin $b
}
