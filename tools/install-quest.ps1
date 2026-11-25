<#
.SYNOPSIS
  USB でつないだすべての Quest に APK をインストールし、マイクの権限を許可する。

.DESCRIPTION
  署名が違うアプリ（別の PC でビルドしたもの）が入っているときは、アンインストールしてから入れ直す。

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\install-quest.ps1 Builds\Release\DNPDemo_20261125-1830-0861ec3.apk
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ApkPath
)

$package = 'com.dnputv.vrdemo'
$permission = 'android.permission.RECORD_AUDIO'

if (-not (Get-Command adb -ErrorAction SilentlyContinue)) {
    Write-Host 'adb が見つかりません。Android SDK の platform-tools を PATH に追加してください。' -ForegroundColor Red
    exit 1
}
if (-not (Test-Path -LiteralPath $ApkPath)) {
    Write-Host "APK が見つかりません: $ApkPath" -ForegroundColor Red
    exit 1
}
$apk = (Resolve-Path -LiteralPath $ApkPath).Path

$deviceLines = @(adb devices | Select-Object -Skip 1 | Where-Object { $_.Trim() -ne '' })
$serials = @($deviceLines | Where-Object { $_ -match '\tdevice$' } | ForEach-Object { ($_ -split '\t')[0] })
$unauthorized = @($deviceLines | Where-Object { $_ -match '\tunauthorized$' })

if ($unauthorized.Count -gt 0) {
    Write-Host "USB デバッグが許可されていない Quest が $($unauthorized.Count) 台あります。ヘッドセットをかぶり、表示されたダイアログで許可してください。" -ForegroundColor Yellow
}
if ($serials.Count -eq 0) {
    Write-Host 'インストールできる Quest がありません。USB でつなぎ、USB デバッグを許可してください。' -ForegroundColor Red
    exit 1
}

$failed = 0
foreach ($serial in $serials) {
    Write-Host "[$serial] インストールしています..."
    $output = (adb -s $serial install -r $apk 2>&1 | Out-String)

    if ($output -match 'INSTALL_FAILED_UPDATE_INCOMPATIBLE') {
        Write-Host "[$serial] 入っているアプリと署名が違うため、アンインストールしてから入れ直します。" -ForegroundColor Yellow
        adb -s $serial uninstall $package | Out-Null
        $output = (adb -s $serial install $apk 2>&1 | Out-String)
    }

    if ($output -notmatch 'Success') {
        Write-Host "[$serial] インストールに失敗しました。" -ForegroundColor Red
        Write-Host $output.Trim()
        $failed++
        continue
    }

    adb -s $serial shell pm grant $package $permission
    $dump = (adb -s $serial shell dumpsys package $package | Out-String)
    $versionName = if ($dump -match 'versionName=(\S+)') { $Matches[1] } else { '不明' }

    if ($dump -match "$([regex]::Escape($permission)): granted=true") {
        Write-Host "[$serial] 完了しました（versionName $versionName、マイクの権限を許可済み）。" -ForegroundColor Green
    }
    else {
        Write-Host "[$serial] インストールしましたが、マイクの権限を許可できませんでした（versionName $versionName）。" -ForegroundColor Red
        $failed++
    }
}

if ($failed -gt 0) {
    exit 1
}
